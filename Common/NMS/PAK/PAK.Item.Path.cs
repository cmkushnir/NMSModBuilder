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

using System.ComponentModel;
using System.Runtime.CompilerServices;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
    /// <summary>
    /// Path wrapper: full, directory, name, extension.
    /// </summary>
    public class Path
	: System.IComparable<Path>
	, System.IComparable<string>
	, System.ComponentModel.INotifyPropertyChanged
	{
		protected void PropertyChangedInvoke( [CallerMemberName] string NAME = "" )
		{
			PropertyChanged?.Invoke(this, new(NAME));
		}
		public event PropertyChangedEventHandler PropertyChanged;

		//...........................................................

		public Path( string FULL = null )
		{
			Full = FULL;
		}

		//...........................................................

		public Path( string DIRECTORY, string NAME, string EXTENSION = null )
		{
			var full = System.IO.Path.Join(DIRECTORY ?? "", NAME ?? "");
			if( !EXTENSION.IsNullOrEmpty() ) {
				full = System.IO.Path.ChangeExtension(full, EXTENSION);
			}
			Full = full;
		}

		//...........................................................

		/// <summary>
		/// Convert all "\\" to "\", convert all "\" to "/"
		/// Remove leading slash, add trailing slash if no extension.
		/// </summary>
		public static string Normalize( string PATH )
		{
			PATH = PATH?
				.Trim(' ', '\t', '\'', '\"')
				.Replace('\\', '/')
				.Replace("/./", "/")
				.Replace("//", "/")
				.TrimStart('/')
				?? ""
			;
			var index_slash = PATH.LastIndexOf('/');
			var index_dot   = PATH.LastIndexOf('.');
			if( index_dot  <= index_slash ) {
				if( !PATH.EndsWith('/')  ) PATH += '/';
			}
			if( !PATH.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase) ) {
				PATH = PATH.ToUpperInvariant();
			}
			return PATH;
		}

		//...........................................................

		/// <summary>
		/// Optionally call Normalize.
		/// Replace invalid extensions e.g. .PNG => .DDS, .MXML => .MBIN, ...
		/// </summary>
		public static string NormalizeExtension( string PATH, bool NORMALIZE_PATH = true )
		{
			if( NORMALIZE_PATH ) PATH = Normalize(PATH);
			if( PATH.IsNullOrEmpty() ) return "";

			var index_slash = PATH.LastIndexOf('/');
			var index_dot   = PATH.LastIndexOf('.');
			if( index_dot  <= index_slash ) return PATH;  // no extension

			var dir_name  = PATH.Substring(0, index_dot);
			var extension = PATH.Substring(index_dot).ToUpper();

			     if( extension == ".MXML" ) PATH = dir_name + ".MBIN";
			else if( extension == ".PNG"  ) PATH = dir_name + ".DDS";
			else if( extension == ".DAE"  ) PATH = dir_name + ".SCENE.MBIN";

			return PATH;
		}

		//...........................................................

		protected string m_full = "";

		/// <summary>
		/// Full "dir/.../name.ext"
		/// </summary>
		public string Full {
			get { return m_full; }
			set {
				value = Normalize(value);
				if( string.Equals(m_full, value) ) return;

				var path = value;
				if( path.IsNullOrEmpty() ) {
					m_full = "";
					m_dir  = "";
					m_name = "";
					m_ext  = "";
					return;
				}

				// some pak items have multiple '.' in name e.g. 'GCDEBUGOPTIONS.GLOBAL.MBIN'
				// parse as: m_name = 'GCDEBUGOPTIONS.GLOBAL', m_extension = '.MBIN'
				var index_ext  = path.LastIndexOf('.');
				var index_name = path.LastIndexOf('/');
				if( index_name > index_ext ) index_ext = -1;  // last '.' before last '/', no ext

				if( index_ext  < 0 ) index_ext  = path.Length;  // no ext
				if( index_name < 0 ) index_name = 0;            // no dir
				else ++index_name;                // name starts after last '/'

				var ext  = (index_ext  >= path.Length) ? "" : path.Substring(index_ext);  // include '.'
				var name = (index_name >= path.Length) ? "" : path.Substring(index_name, index_ext - index_name);
				var dir  = path.Substring(0, index_name);
				if( dir.Length > 0 && !dir.EndsWith('/') ) dir += '/';
				var full = dir + name + ext;

				var is_ext_changed  = !string.Equals(ext,  m_ext);
				var is_name_changed = !string.Equals(name, m_name);
				var is_dir_changed  = !string.Equals(dir,  m_dir);
				var is_full_changed = !string.Equals(full, m_full);

				m_ext  = ext;
				m_name = name;
				m_dir  = dir;
				m_full = full;

				if( is_ext_changed  ) PropertyChangedInvoke("Extension");
				if( is_name_changed ) PropertyChangedInvoke("Name");
				if( is_dir_changed  ) PropertyChangedInvoke("Directory");
				if( is_full_changed ) PropertyChangedInvoke("Full");
			}
		}

		//...........................................................

		protected string m_dir = "";

		/// <summary>
		/// No name no ext, ends with '\\'.
		/// </summary>
		public string Directory {
			get { return m_dir; }
			set {
				if( m_dir == value ) return;
				var full = System.IO.Path.Join(value, Name);
				Full = System.IO.Path.ChangeExtension(full, Extension);
			}
		}

		//...........................................................

		/// <summary>
		/// Name + Extension.
		/// </summary>
		public string NameExt {
			get { return m_name + m_ext; }
			set {
				if( NameExt == value ) return;
				Full = System.IO.Path.Join(Directory, value);
			}
		}

		//...........................................................

		protected string m_name = "";

		/// <summary>
		/// No dir no ext.
		/// </summary>
		public string Name {
			get { return m_name; }
			set {
				if( m_name == value ) return;
				var full = System.IO.Path.Join(Directory, value);
				Full = System.IO.Path.ChangeExtension(full, Extension);
			}
		}

		//...........................................................

		protected string m_ext = "";

		/// <summary>
		/// No dir no name.
		/// If !Extension then assume is directory.
		/// If setting Extension must include leading '.'.
		/// </summary>
		public string Extension {
			get { return m_ext; }
			set {
				if( m_ext == value ) return;
				Full = System.IO.Path.ChangeExtension(Full, value);
			}
		}

		//...........................................................

		public static int Compare( Path LHS, Path RHS )
		{
			// don't compare Full, Blah.cs would sort after Blah_.cs
			// as it would compare '.' vs '_'
			var c  = string.Compare(LHS?.Directory, RHS?.Directory);
			if( c != 0 ) return c;

			c = string.Compare(LHS?.Name, RHS?.Name);
			if( c != 0 ) return c;

			return string.Compare(LHS?.Extension, RHS?.Extension);
		}
		public static int Compare( Path   LHS, string RHS ) => Compare(LHS, new Path(RHS));
		public static int Compare( string LHS, Path   RHS ) => Compare(new Path(LHS), RHS);

		public int CompareTo( Path   RHS ) => Compare(this, RHS);
		public int CompareTo( string RHS ) => Compare(this, RHS);

		//...........................................................

		public static bool Equals( Path   LHS, Path   RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( Path   LHS, string RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( string LHS, Path   RHS ) => Compare(LHS, RHS) == 0;

		public override bool Equals( object RHS )
		{
			if( RHS is Path   rhs_path ) return Equals(this, rhs_path);
			if( RHS is string rhs_str  ) return Equals(this, rhs_str);
			return false;
		}

		//...........................................................

		// https://stackoverflow.com/questions/16789360/wpf-listbox-items-with-changing-hashcode
		// - Changing the hash code of an item in a ListBox seems to work ok.
		// - Changing the hash code of the selected item in a ListBox breaks it's functionality.
		// When a selection made (single or multiple selection mode) the IList ListBox.SelectedItems is updated.
		// Items that are added to the selection are added to SelectedItems and items that are no longer included in the selection are removed.
		// If the hash code of an item is changed while it is selected, there is no way to remove it from SelectedItems.
		//
		// So, if we use GetHashCode() => Path.GetHashCode() it will break ListBox's
		// if the Item is currently Selected.  Since we can't rely on how it will
		// be handled we will just use the default object.GetHashCode().
		public override int GetHashCode() => base.GetHashCode();
		public override string ToString() => Full;

		//...........................................................

		public static implicit operator Path( string FULL ) => new Path(FULL);
		public static implicit operator string( Path PATH ) => PATH?.Full;
	}
}

//=============================================================================

namespace cmk
{
    public static partial class _x_
	{
		public static bool IsNullOrEmpty( this cmk.NMS.PAK.Item.Path PATH )
		{
			return PATH?.Full.IsNullOrEmpty() ?? true;
		}
	}
}

//=============================================================================
