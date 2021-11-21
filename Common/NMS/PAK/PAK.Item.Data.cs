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
	public partial class Data
	: System.IComparable<Data>  // compare Path's, not Raw
	{
		public Data()
		{
			// hack: for derived static constructor registration.
			// all derived must also have a default constructor.
		}

		//...........................................................

		/// <summary>
		/// Construct from raw data extracted from a pak file.
		/// If !RAW then uses INFO.Raw(), if !INFO.Raw() uses new MemoryStream().
		/// </summary>
		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null )
		{
			File = INFO?.File;
			Game = INFO?.Game;
			Path = INFO?.Path ?? new();
			Raw  = RAW ?? INFO.Raw() ?? new MemoryStream();
		}

		//...........................................................

		/// <summary>
		/// Construct new data to be added to a mod pak.
		/// If !RAW then uses new MemoryStream().
		/// </summary>
		public Data( Game.Data GAME, string PATH, Stream RAW = null )
		{
			Game = GAME;
			Path = new NMS.PAK.Item.Path(PATH);
			Raw  = RAW ?? new MemoryStream();
		}

		//...........................................................

		protected struct DataTypes
		{
			public DataTypes( Type DATA = null, Type VIEWER = null, Type DIFFER = null )
			{
				Data          = DATA;
				DefaultViewer = VIEWER;
				DefaultDiffer = DIFFER;
			}
			public Type Data;
			public Type DefaultViewer;
			public Type DefaultDiffer;
		}

		protected readonly static Dictionary<string,DataTypes> s_extensions = new();

		/// <summary>
		/// To be called by an application after all dll's are loaded.
		/// Will go through each dll and create instances of each
		/// class derived from cmk.NMS.PAK.Item.Data, which will trigger
		/// the class static constructor, which will register it with s_extensions.
		/// </summary>
		public static void RegisterAllClasses()
		{
			foreach( var assembly in AppDomain.CurrentDomain.GetAssemblies() ) {
				var    location = "";
				try  { location = assembly.Location; }
				catch{}
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
			}
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
				var types     = s_extensions.GetValueOrDefault(extension);
				return types.Data == null ?
					new(INFO, RAW) :
					Activator.CreateInstance(types.Data, INFO, RAW) as NMS.PAK.Item.Data
				;
			}
			catch( Exception EX ) { LOG.AddFailure(EX, $"{INFO.Path}:\n"); }
			return null;
		}

		//...........................................................

		/// <summary>
		/// Factory to create correct Data object based on PATH.
		/// Use for new pak items e.g. to be added to a mod pak.
		/// </summary>
		public static NMS.PAK.Item.Data Create( Game.Data GAME, string PATH, Stream RAW, Log LOG = null )
		{
			if( !PATH.IsNullOrEmpty() ) try {
				var extension = System.IO.Path.GetExtension(PATH).ToUpper();
				var types     = s_extensions.GetValueOrDefault(extension);
				return types.Data == null ?
					new(GAME, PATH, RAW) :
					Activator.CreateInstance(types.Data, GAME, PATH, RAW) as NMS.PAK.Item.Data
				;
			}
			catch( Exception EX ) { LOG.AddFailure(EX, $"{PATH}:\n"); }
			return null;
		}

		//...........................................................

		public readonly Game.Data Game = null;  // never null if Data is valid
	
		public readonly NMS.PAK.File.Loader File = null;  // null if we create Data from scratch
		public IO.Path                 FilePath      => File?.Path;
		public List<NMS.PAK.Item.Info> FileInfoList  => File?.InfoList;
		public NMS.PAK.Item.Info.Node  FileInfoTree  => File?.InfoTree;
		public bool                    FileInPCBANKS => File?.InPCBANKS ?? false;
		public bool                    FileInMODS    => File?.InMODS    ?? false;

		public readonly Stream Raw = null;  // never null, but may be empty

		//...........................................................
		
		public NMS.PAK.Item.Path Path { get; }  // item path

		/// <summary>
		/// Has any data for this instance been modified since extract.
		/// e.g. only IsEdited Data gets added to mod .pak on PAK.Factory.Load.
		/// May not be known until after Save is called e.g. mbin's.
		/// </summary>
		public bool IsEdited { get; protected set; } = false;

		/// <summary>
		/// Build uses as a flag to indicate if this data should result
		/// in the source game pak being rebuilt using this data.
		/// This is to get around cases where game may force load data
		/// from PCBANKS and not look at MODS pak files.
		/// </summary>
		public bool IsGameReplacement { get; set; } = false;  // todo, not used

		//...........................................................

		/// <summary>
		/// Write any changes back to Raw.
		/// IsEdited = ???, generally only set true never false here.
		/// </summary>
		public virtual bool Save()
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
		public virtual UIElement GetViewer( NMS.PAK.Item.Data LHS = null )
		{
			var lhs    = LHS as Data;
			var types  = s_extensions.GetValueOrDefault(Path.Extension.ToUpper());
			var viewer = types.DefaultViewer;  // todo: ?? HexViewer
			var differ = types.DefaultDiffer;  // todo: ?? HexDiffer
			return lhs == null ?
				viewer == null ? null : Activator.CreateInstance(viewer,      this) as UIElement :
				differ == null ? null : Activator.CreateInstance(differ, lhs, this) as UIElement
			;
		}

		//...........................................................

		/// <summary>
		/// Add any other supported extension types to DIALOG.Filter,
		/// and possibly change extension of DIALOG.FileName.
		/// </summary>
		protected virtual void SaveFilePrepare( SaveFileDialog DIALOG )
		{
		}

		//...........................................................

		/// <summary>
		/// Prompt user to select a save file path.
		/// If valid path selected then save current Raw to path,
		/// or convert and save if non-Raw alternate format (extension) selected.
		/// Caller responsible for ensuring Saved first.
		/// </summary>
		public void SaveFileDialog()
		{
			var dialog = new SaveFileDialog {
				InitialDirectory = Resource.SaveDirectory,
				FileName = Path?.Name,
				Filter   = string.Format("{0}|*{1}",
					Path?.Extension.TrimStart('.'),
					Path?.Extension
				),
			};
			SaveFilePrepare(dialog);
			if( dialog.ShowDialog() == true ) {
				try {
					Resource.SaveDirectory = dialog.FileName;
					SaveFileTo(dialog.FileName);
				}
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
			}
		}

		//...........................................................

		/// <summary>
		/// Save current Raw to PATH.
		/// Derived may add convert and save for alternate formats (extensions).
		/// Caller responsible for ensuring Saved first.
		/// </summary>
		protected virtual void SaveFileTo( string PATH )
		{
			if( Raw == null || PATH.IsNullOrEmpty() ) return;
			try {
				Raw.Position = 0;
				using( var file = System.IO.File.Create(PATH) ) {
					Raw.CopyTo(file);
				}
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
		}

		//...........................................................

		public override string ToString() => Path?.ToString();

		int IComparable<Data>.CompareTo( Data RHS )
		{
			if( Path == null && RHS == null ) return 0;
			if( Path == null ) return -1;
			if( RHS  == null ) return  1;
			return Path.CompareTo(RHS.Path);
		}

		//=========================================================================

		/// <summary>
		/// Tree node wrapping extracted pak item data.
		/// </summary>
		public class Node : cmk.PathNode<NMS.PAK.Item.Data, Node>
		{
			public Node() {}
			public Node(
				Node   PARENT = null,
				string PATH   = "",
				Data   ENTRY  = null
			)
			: base(PARENT, PATH, ENTRY)
			{}
		}
	}
}

//=============================================================================
