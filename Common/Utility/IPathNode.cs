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
using System.Threading;

//=============================================================================

namespace cmk
{
	public interface IPathNode
	: System.IComparable<IPathNode>
	, System.IComparable<string>
	{
		string Name { get; }       // last part of Path
		string Path { get; }       // full path to this node e.g. "dir1/dir2/name.ext"
		object Tag  { get; set; }  // payload

		IPathNode              Root   { get; }
		IPathNode              Parent { get; }  // parent node, null if root
		IEnumerable<IPathNode> Items  { get; }  // children

		List<IPathNode> PathNodes { get; }  // get nodes from root to this

		IPathNode Find( string PATH );
	}

	//=========================================================================

	public class PathNode<TAG_T, DERIVED_T>
	: cmk.IPathNode
	where TAG_T : class
	where DERIVED_T : PathNode<TAG_T, DERIVED_T>, new()
	{
		/// <summary>
		/// The PATH passed in starts with this node.
		/// The first segment will be parsed from PATH and used as this Text.
		/// The remainder of PATH will be passed to Insert(remainder, TAG).
		/// Only the last segment will be assigned TAG.
		/// Note: The Path property returns the path from the root node to (including) this node.
		/// </summary>
		/// <param name="PARENT">Parent node.</param>
		/// <param name="PATH">Path from this node down. Segments are delimited by '/'.</param>
		/// <param name="TAG">Payload to be attached to leaf node.</param>
		public PathNode() {}
		public PathNode(
			PathNode<TAG_T, DERIVED_T> PARENT,
			string NAME,
			TAG_T  TAG
		){
			Parent = PARENT as DERIVED_T;
			Name   = NAME ?? "";
			Tag    = TAG; 
			Path   = Parent?.Path + Name;
		}

		//...........................................................

		public string Path { get; protected set; }  // path up to (including) this node
		public string Name { get; protected set; }  // directory or file name

		//...........................................................

		object IPathNode.Tag {
			get => Tag;
			set => Tag = value as TAG_T;
		}
		public TAG_T Tag { get; set; }

		//...........................................................

		IPathNode IPathNode.Root {
			get {
				for( var node = this; node != null; node = node.Parent ) {
					if( node.Parent == null ) return node;
				}
				return null;
			}
		}

		public DERIVED_T Root {
			get {
				for( var node = this as DERIVED_T; node != null; node = node.Parent ) {
					if( node.Parent == null ) return node;
				}
				return null;
			}
		}

		//...........................................................

		IPathNode IPathNode.Parent => Parent;
		public DERIVED_T    Parent { get; protected set; }

		//...........................................................

		IEnumerable<IPathNode> IPathNode.Items => Items;
		public List<DERIVED_T>           Items { get; protected set; }

		//...........................................................

		public List<IPathNode> PathNodes {
			get {
				var list = Parent?.PathNodes ?? new List<IPathNode>();
				list.Add(this);
				return list;
			}
		}

		//...........................................................

		public void ItemsClear()
		{
			Items?.Clear();
		}

		//...........................................................

		protected virtual PathNode<TAG_T, DERIVED_T> CreateDerived(
			PathNode<TAG_T, DERIVED_T> PARENT, string PATH, TAG_T TAG
		){
			return new PathNode<TAG_T, DERIVED_T>(PARENT, PATH, TAG);
		}

		/// <summary>
		/// Insert PATH + TAG into this list of Items.
		/// Creates sub-nodes as needed.
		/// </summary>
		/// <param name="PATH">
		/// Path under this node e.g. if want to find absolute path 'a/b/c.x'
		/// and this node is 'a/' then PATH would be 'b/c.x', as opposed to 'a/b/c.x'.
		/// </param>
		/// <param name="TAG">Payload to be attached to leaf node.</param>
		#if true  // recursive Insert
		public DERIVED_T Insert( string PATH, TAG_T TAG )
		{
			if( PATH.IsNullOrEmpty() ) return null;

			var index     = PATH.IndexOf('/');
			var name      = index < 1 ? PATH : PATH.Substring(0, index + 1);
			var remaining = PATH.Substring(name.Length);
			var is_leaf   = remaining.IsNullOrEmpty();

			if( Items == null ) Items = new();

			var item  = Items.Bsearch(name, Compare);
			if( item == null ) {
				var at = Items.BsearchIndexOfInsert(name, Compare);
				item   = CreateDerived(this, name, is_leaf ? TAG : null) as DERIVED_T;
				Items.Insert(at, item);
			}

			return is_leaf ? item : item.Insert(remaining, TAG);
		}
		#else  // iterative Insert (no slashes at end of folder name)
		public DERIVED_T Insert( string PATH, TAG_T TAG )
		{
			if( PATH.IsNullOrEmpty() ) return null;

			var current = this;
			var parts   = PATH.Split('/');
			var end     = parts.Length - 1;

			DERIVED_T top = null;

			for( var i = 0; i <= end; ++i ) {
				var part  = parts[i];
				var item  = current.Items?.Bsearch(part, Compare);
				if( item == null ) {
					var at = 0;
					if( current.Items == null ) current.Items = new();
					else at = current.Items.BsearchIndexOfInsert(part, Compare);
					item = CreateDerived(this, part, i == end ? TAG : null) as DERIVED_T;
					current.Items.Insert(at, item);
				}
				current = item;
				if( top == null ) top = item;
			}

			return top;
		}
		#endif  // speed is similar enough not to pref one over other

		//...........................................................

		public DERIVED_T Remove( string PATH, bool REMOVE_EMPTY_FOLDER = true )
		{
			if( PATH.IsNullOrEmpty() ) return null;

			var index     = PATH.IndexOf('/');
			var name      = index < 1 ? PATH : PATH.Substring(0, index + 1);
			var remaining = PATH.Substring(name.Length);
			var is_leaf   = remaining.IsNullOrEmpty();

			var item  = Items?.Bsearch(name, Compare);
			if( item == null ) return null;

			if( is_leaf ) Items?.Remove(item);
			else          item = Remove(remaining, REMOVE_EMPTY_FOLDER);  // recurse

			if( REMOVE_EMPTY_FOLDER && item.Items.IsNullOrEmpty() ) {
				Items?.Remove(item);
			}

			return item;
		}

		//...........................................................

		public void ForEachTag(
			Action<TAG_T, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		){
			if( Tag != null ) HANDLER(Tag, CANCEL, LOG);

			if( !Items.IsNullOrEmpty() )
			foreach( var item in Items ) {
				if( CANCEL.IsCancellationRequested ) break;
				item.ForEachTag(HANDLER, CANCEL, LOG);
			}
		}

		//...........................................................

		public DERIVED_T Find( Predicate<DERIVED_T> MATCH )
		{
			if( MATCH(this as DERIVED_T) ) return this as DERIVED_T;

			if( !Items.IsNullOrEmpty() )
			foreach( var item in Items ) {
				var found  = item.Find(MATCH);
				if( found != null ) return found;
			}

			return null;
		}

		//...........................................................

		IPathNode IPathNode.Find( string PATH )
		{
			return Find(PATH);
		}

		/// <summary>
		/// Search for exact (not partial) PATH as a child of this node
		/// e.g. if this node is 'a/' then PATH would be 'b/c.x', as opposed to 'a/b/c.x'.
		/// </summary>
		/// <param name="PATH">
		/// Segments delimited by '/'.
		/// If looking for a directory then PATH must end with '/'.
		/// </param>
		/// <returns>Null if PATH not found.</returns>
		public DERIVED_T Find( string PATH )
		{
			if( PATH.IsNullOrEmpty() || Items.IsNullOrEmpty() ) return null;

			var index = PATH.IndexOf('/');
			var name  = index < 1 ? PATH : PATH.Substring(0, index + 1);

			var found  = Items.Bsearch(name, Compare);
			if( found == null ) return null;

			var    remaining = PATH.Substring(name.Length);
			return remaining.IsNullOrEmpty() ?
				found : found.Find(remaining)  // recurse
			;
		}

		//...........................................................

		/// <summary>
		/// </summary>
		/// <returns>Text, not Path.</returns>
		public override string ToString()
		{
			return Name;
		}

		//...........................................................

		/// <summary>
		/// Compare Name against RHS.Name.
		/// Directories (Path|RHS.Path ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public int CompareTo( IPathNode RHS )
		{
			return CompareTo(RHS?.Name);
		}

		/// <summary>
		/// Compare Name against RHS string.
		/// Directories (Path|RHS ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="RHS">Directory or file name.</param>
		/// <returns></returns>
		public int CompareTo( string RHS )
		{
			if( RHS == null ) return 1;

			bool lhs_is_dir = Name[Name.Length - 1] == '/',
				 rhs_is_dir = RHS [RHS .Length - 1] == '/';

			if( lhs_is_dir == rhs_is_dir ) {
				// this and RHS are either both directories or both files
				return String.CompareNumeric(Name, RHS, true);
			}

			if( lhs_is_dir ) return -1;  // this is directory, RHS is file
			                 return  1;  // this is file, RHS is directory
		}

		//...........................................................

		/// <summary>
		/// Compare LHS node Name against RHS node Name.
		/// Directories (LHS.Name|RHS.Name ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="LHS"></param>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public static int Compare( DERIVED_T LHS, DERIVED_T RHS )
		{
			if( LHS == null && RHS == null ) return 0;
			if( LHS == null ) return -1;
			return LHS.CompareTo(RHS);
		}

		/// <summary>
		/// Compare LHS node Name against RHS string.
		/// Directories (LHS.Name|RHS ends with '/') compare as less-than files.
		/// </summary>
		/// <param name="LHS"></param>
		/// <param name="RHS"></param>
		/// <returns></returns>
		public static int Compare( DERIVED_T LHS, string RHS )
		{
			if( LHS == null && RHS == null ) return 0;
			if( LHS == null ) return -1;
			return LHS.CompareTo(RHS);
		}
	}
}

//=============================================================================
