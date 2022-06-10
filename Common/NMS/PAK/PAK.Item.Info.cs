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

using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
	/// <summary>
	/// Meta-data for a (compressed) item in a NMS .pak file.
	/// </summary>
	public class Info
	: System.IComparable
	{
		public Info( NMS.PAK.File.Loader FILE, int ID, uint INDEX, long OFFSET, long LENGTH )
		{
			File   = FILE;
			Id     = ID;
			Index  = INDEX;
			Offset = OFFSET;
			Length = LENGTH;
		}

		//...........................................................

		public readonly NMS.PAK.File.Loader File = null;
		public IO.Path                 FilePath      => File?.Path      ?? new();
		public List<NMS.PAK.Item.Info> FileInfoList  => File?.InfoList  ?? new();
		public NMS.PAK.Item.Info.Node  FileInfoTree  => File?.InfoTree;
		public bool                    FileInPCBANKS => File?.InPCBANKS ?? false;
		public bool                    FileInMODS    => File?.InMODS    ?? false;
		public string                  FileSubPath   => File?.SubPath   ?? "";

		public readonly   int Id     = 0;  // ordinal of EntryInfo in IO.PAK.m_entries
		public readonly  uint Index  = 0;  // index of first block for Item in PAK.Blocks
		public readonly  long Offset = 0;  // offset in PAK file where Item block data starts
		public readonly  long Length = 0;  // length of uncompressed Item

		//...........................................................

		protected NMS.PAK.Item.Path m_path = new NMS.PAK.Item.Path();

		/// <summary>
		/// Item path, never null.
		/// </summary>
		public NMS.PAK.Item.Path Path {
			get { return m_path; }
			set { m_path = value ?? new NMS.PAK.Item.Path(); }
		}

		//...........................................................

		// parent NMS.PAK.File.Loader will default to its InfoTree.Find(Path).
		// PCBANKS collection will change to its merged InfoTree.Find(Path).
		public NMS.PAK.Item.Info.Node TreeNode { get; set; }

		//...........................................................

		// hack: doesn't belong here, but convenient.
		// from mbin items only, set for each mbin item when PAK.File.Loader loaded.
		public NMS.PAK.MBIN.Header MbinHeader { get; set; }

		//...........................................................

		public Stream Raw( Log LOG = null )
		{
			return File?.Extract(this, LOG);
		}

		//...........................................................

		/// <summary>
		/// Get this Item Data from File.
		/// </summary>
		public NMS.PAK.Item.Data ExtractData( Log LOG = null )
		{
			return File?.ExtractData(this, LOG);
		}

		public AS_T ExtractData<AS_T>( Log LOG = null )
		where AS_T : NMS.PAK.Item.Data
		{
			return File?.ExtractData<AS_T>(this, LOG);
		}

		//...........................................................

		/// <summary>
		/// Get this DDS Item Data from File and convert to a BitmapSource.
		/// </summary>
		public BitmapSource ExtractDdsBitmapSource( int HEIGHT = 32, Log LOG = null )
		{
			return File?.ExtractDdsBitmapSource(this, HEIGHT, LOG);
		}

		//...........................................................

		/// <summary>
		/// Get this MBIN or MBIN.PC Item Data from File and convert to NMSTemplate based DOM.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( Log LOG = null )
		where AS_T : class  // libMBIN.NMSTemplate
		{
			return File?.ExtractMbin<AS_T>(this, LOG);
		}

		//...........................................................

		public int CompareTo( object RHS )
		{
			if( RHS is Info rhs ) return Path.Compare(Path, rhs?.Path);
			if( RHS.GetType().IsCastableTo(typeof(string)) ) {
				return Path.Compare(Path, (string)RHS);
			}
			return 1;  // RHS is likely something like UnsetValue
		}

		//...........................................................

		public override string ToString() => Path;

		//=========================================================================

		/// <summary>
		/// Tree node wrapping meta-data for a .pak entry.
		/// </summary>
		public class Node: cmk.PathNode<NMS.PAK.Item.Info, Node>
		{
			public Node() {}
			public Node(
				Node   PARENT = null,
				string PATH   = "",
				Info   ENTRY  = null
			)
			: base(PARENT, PATH, ENTRY)
			{}
			protected override PathNode<NMS.PAK.Item.Info, Node> CreateDerived(
				PathNode<NMS.PAK.Item.Info, Node> PARENT,
				string                            PATH,
				NMS.PAK.Item.Info                 TAG
			)	=> new Node(PARENT as Node, PATH, TAG);
		}
	}
}

//=============================================================================
