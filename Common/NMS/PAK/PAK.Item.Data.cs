//=============================================================================
/*
cmk NMS Common
Copyright (C) 2021  Chris Kushnir

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
//=============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
    public interface IViewer
	{
		ImageButton ViewerButton { get; }
	}

	public interface IDiffer
	{
		ImageButton DifferButton { get; }
	}

	//=========================================================================

	public class Extension
	{
		public Type Data;  // e.g. typeof(NMS.PAK.MBIN.Data)

		/// <summary>
		/// Registered viewers and differs.
		/// First entry is always used as default, last should be hex,
		/// so registering viewers and differs should use Insert instead of Add.
		/// A toolbar button is created for each derived from Viewer. 
		/// </summary>
		public List<Type> Viewers = new() { typeof(HexViewer) };
		public List<Type> Differs = new() { typeof(HexDiffer) };
	}

	//=========================================================================

	public class Data
	: System.IComparable<Data>  // compare Path's, not Raw
	{
		// will compile of protected, but RegisterAllClasses Activator will fail
		// hack: for derived static constructor registration.
		// all derived must also have a default constructor.
		public Data()
		{
			Path = new();
		}

		//...........................................................

		/// <summary>
		/// Construct from raw data extracted from a pak file.
		/// If !RAW then uses INFO.Extract().
		/// </summary>
		public Data( NMS.PAK.Item.Info INFO, Stream RAW, Log LOG )
		{
			Info      = INFO;
			Path      = InfoPath;
			Raw       = RAW ?? Info?.Extract();
			Extension = s_extensions.GetValueOrDefault(Path.Extension.ToUpper());
		}

		//...........................................................

		/// <summary>
		/// Construct new data to be added to a mod pak.
		/// If !RAW then uses new MemoryStream().
		/// Sets IsEdited = true.
		/// </summary>
		public Data( string PATH, Stream RAW, Log LOG )
		{
			Path      = new NMS.PAK.Item.Path(PATH);
			Raw       = RAW ?? new MemoryStream();
			Extension = s_extensions.GetValueOrDefault(Path.Extension.ToUpper());
			IsEdited  = true;
		}

		//...........................................................

		protected readonly static Dictionary<string,Extension> s_extensions = new();

		/// <summary>
		/// To be called by an application after all dll's are loaded.
		/// Will go through each dll and create instances of each
		/// class derived from cmk.NMS.PAK.Item.Data, which will trigger
		/// the class static constructor, which will register it with s_extensions.
		/// </summary>
		public static void RegisterAllClasses()
		{
			foreach( var assembly in AppDomain.CurrentDomain.GetAssemblies() ) {
				var   location = "";
				try { location = assembly.Location; }
				catch {}  // may get System.NotSupportedException
				if( location.StartsWith(Resource.AppDirectory) ) {
					RegisterAllClasses(assembly);
				}
			}
		}

		protected static void RegisterAllClasses( Assembly ASSEMBLY )
		{
			foreach( var type in ASSEMBLY.GetTypes() ) {
				if( !type.IsAssignableTo(typeof(Data)) ) continue;
				var instance = Activator.CreateInstance(type) as Data;  // trigger static constructor
			}															// default Item.Data constructor used, instance.Extension not set
		}

		//...........................................................

		/// <summary>
		/// Factory to create correct Data object based on INFO.Path.
		/// Use for existing pak items e.g. in pcbanks or mods folder.
		/// </summary>
		public static NMS.PAK.Item.Data Create( NMS.PAK.Item.Info INFO, Stream RAW, Log LOG = null )
		{
			if( INFO != null && RAW != null ) try {
				var extension = INFO.Path.Extension.ToUpper();
				var type      = s_extensions.GetValueOrDefault(extension);
				var data      = type?.Data == null ?
					new(INFO, RAW, LOG) :  // unsupported extension
					Activator.CreateInstance(type.Data, INFO, RAW, LOG) as NMS.PAK.Item.Data
				;
				return data;
			}
			catch( Exception EX ) { LOG.AddFailure(EX, $"{INFO.Path}:\n"); }
			return null;
		}

		//...........................................................

		/// <summary>
		/// Factory to create correct Data object based on PATH.
		/// Use for new pak items e.g. to be added to a mod pak.
		/// </summary>
		public static NMS.PAK.Item.Data Create( string PATH, Stream RAW, Log LOG = null )
		{
			if( !PATH.IsNullOrEmpty() ) try {  // RAW may be null, Data constructor will handle
				var extension = System.IO.Path.GetExtension(PATH).ToUpper();
				var type      = s_extensions.GetValueOrDefault(extension);
				var data      = type?.Data == null ?
					new(PATH, RAW, LOG) :
					Activator.CreateInstance(type.Data, PATH, RAW, LOG) as NMS.PAK.Item.Data
				;
				return data;
			}
			catch( Exception EX ) { LOG.AddFailure(EX, $"{PATH}:\n"); }
			return null;
		}

		//...........................................................

		public NMS.PAK.Item.Data Clone( Log LOG = null )
		{
			var data = Create(Path, Raw.Clone(), LOG);
			if( data == null ) return null;

			data.IsEdited = IsEdited;

			return data;
		}

		//...........................................................

		public readonly NMS.PAK.Item.Info Info           =  null;  // null if we create Data from scratch
		public NMS.PAK.Item.Path          InfoPath       => Info?.Path ?? new();
		public NMS.PAK.Item.Info.Node     InfoTreeNode   => Info?.TreeNode;
		public NMS.PAK.MBIN.Header        InfoMbinHeader => Info?.MbinHeader;
		public string                     InfoEbinCache  => Info?.EbinCache;

		public NMS.PAK.File.Loader     File          => Info?.File;
		public IO.Path                 FilePath      => File?.Path      ?? new();
		public List<NMS.PAK.Item.Info> FileInfoList  => File?.InfoList  ?? new();
		public NMS.PAK.Item.Info.Node  FileInfoTree  => File?.InfoTree;
		public bool                    FileInPCBANKS => File?.InPCBANKS ?? false;
		public bool                    FileInMODS    => File?.InMODS    ?? false;
		public string                  FileSubPath   => File?.SubPath   ?? "";

		public readonly NMS.PAK.Item.Extension Extension;  // from s_extensions
		public readonly NMS.PAK.Item.Path      Path;       // item path, not all Data have Info
		public readonly Stream                 Raw;        // never null, but may be empty

		//...........................................................

		/// <summary>
		/// Has any data for this instance been modified since extract.
		/// e.g. only IsEdited Data gets added to mod .pak on PAK.Factory.Load.
		/// May not be known until after Save is called e.g. mbin's.
		/// </summary>
		public bool IsEdited { get; set; } = false;

		/// <summary>
		/// Build uses as a flag to indicate if this data should result
		/// in the source game pak being rebuilt using this data.
		/// This is to get around cases where game may force load data
		/// from PCBANKS and not look at MODS pak files.
		/// </summary>
		public bool IsGameReplacement { get; set; } = false;  // todo, unused

		//...........................................................

		/// <summary>
		/// Write any changes back to Raw.
		/// IsEdited = ???, generally only set true never false here.
		/// </summary>
		public virtual bool Save( Log LOG = null )
		{
			return true;
		}

		//...........................................................

		/// <summary>
		/// Get type specific viewer.
		/// If LHS == null return a single item viewer.
		/// If LHS != null return a two item differ,
		/// LHS is game version of Data, this is Mod version of Data.
		/// </summary>
		public UIElement GetViewer( NMS.PAK.Item.Data LHS, Log LOG = null )
		{
			if( Extension == null ) return null;
			var viewer = Extension.Viewers.Count < 1 ? typeof(HexViewer) : Extension.Viewers[0];
			var differ = Extension.Differs.Count < 1 ? typeof(HexDiffer) : Extension.Differs[0];
			var lhs    = LHS as Data;
			return lhs == null ?
				Activator.CreateInstance(viewer,      this, LOG) as UIElement :
				Activator.CreateInstance(differ, lhs, this, LOG) as UIElement
			;
		}

		//...........................................................

		/// <summary>
		/// Add any other supported extension types to DIALOG.Filter,
		/// and possibly change extension of DIALOG.FileName.
		/// </summary>
		protected virtual void SaveFilePrepare( SaveFileDialog DIALOG, Log LOG = null )
		{
		}

		//...........................................................

		/// <summary>
		/// Prompt user to select a save file path.
		/// If valid path selected then save current Raw to path,
		/// or convert and save if non-Raw alternate format (extension) selected.
		/// Caller responsible for ensuring Saved first.
		/// </summary>
		public void SaveFileDialog( Log LOG = null )
		{
			var dialog = new SaveFileDialog {
				InitialDirectory = Resource.SaveDirectory,
				FileName = Path?.Name,
				Filter   = string.Format("{0}|*{1}",
					Path?.Extension.TrimStart('.'),
					Path?.Extension
				),
			};
			SaveFilePrepare(dialog, LOG);
			if( dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true ) {
				try {
					Resource.SaveDirectory = dialog.FileName;
					SaveFileTo(dialog.FileName, LOG);
				}
				catch( Exception EX ) { LOG.AddFailure(EX); }
			}
		}

		//...........................................................

		/// <summary>
		/// Save current Raw to PATH.
		/// Derived may add convert and save for alternate formats (extensions).
		/// Caller responsible for ensuring Saved first.
		/// </summary>
		protected virtual void SaveFileTo( string PATH, Log LOG = null )
		{
			if( Raw == null || PATH.IsNullOrEmpty() ) return;
			try {
				Raw.Position = 0;
				using( var file = System.IO.File.Create(PATH) ) {
					Raw.CopyTo(file);
				}
			}
			catch( Exception EX ) { LOG.AddFailure(EX); }
		}

		//...........................................................

		public override string ToString() => Path?.ToString();

		int IComparable<Data>.CompareTo( Data RHS )
		{
			return Path.Compare(Path, RHS?.Path);
		}

		//=========================================================================

		/// <summary>
		/// Tree node wrapping extracted pak item data.
		/// </summary>
		public class Node: cmk.PathNode<NMS.PAK.Item.Data, Node>
		{
			public Node() {}

			public Node(
				Node   PARENT = null,
				string PATH   = "",
				Data   ENTRY  = null
			)
			: base(PARENT, PATH, ENTRY)
			{}

			protected override PathNode<NMS.PAK.Item.Data, Node> CreateDerived(
				PathNode<NMS.PAK.Item.Data, Node> PARENT,
				string                            PATH,
				NMS.PAK.Item.Data                 TAG
			)	=> new Node(PARENT as Node, PATH, TAG);
		}
	}
}

//=============================================================================
