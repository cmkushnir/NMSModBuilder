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
using System.Linq;
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
	where TAG_T     : class
	where DERIVED_T : PathNode<TAG_T, DERIVED_T>
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
			DERIVED_T PARENT,
			string    PATH,
			TAG_T     TAG
		){
			m_parent = PARENT;
			if( PATH.IsNullOrEmpty() ) return;

			var index = PATH.IndexOf('/');
			Name = index < 1 ? PATH : PATH.Substring(0, index + 1);
			Path = m_parent?.Path + Name;

			var remaining = PATH.Substring(Name.Length);
			if( remaining.Length < 1 ) m_tag = TAG;
			else                       Insert(remaining, TAG);
		}

		//...........................................................

		public string Path { get; protected set; } = "";  // path up to (including) this node
		public string Name { get; protected set; } = "";  // directory or file name

		//...........................................................

		protected TAG_T m_tag;

		object IPathNode.Tag {
			get { return Tag; }
			set { Tag = value as TAG_T; }
		}

		public TAG_T Tag {
			get { return m_tag; }
			set {
				if( m_tag != value ) {
					m_tag  = value;
				}
			}
		}

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

		protected DERIVED_T m_parent;

		IPathNode IPathNode.Parent {
			get { return m_parent; }
		}

		public DERIVED_T Parent {
			get { return m_parent; }
		}

		//...........................................................

		protected List<DERIVED_T> m_items;

		IEnumerable<IPathNode> IPathNode.Items {
			get { return Items; }
		}

		public List<DERIVED_T> Items {
			get { return m_items; }
		}

		//...........................................................

		public List<IPathNode> PathNodes {
			get {
				var list = m_parent?.PathNodes ?? new List<IPathNode>();
				list.Add(this);
				return list;
			}
		}

		//...........................................................

		public void ItemsClear()
		{
			if( m_items != null ) m_items.Clear();
		}

		//...........................................................

		protected DERIVED_T CreateDerived( DERIVED_T PARENT, string PATH, TAG_T TAG )
		{
			return (DERIVED_T)Activator.CreateInstance(typeof(DERIVED_T), PARENT, PATH, TAG);
		}

		/// <summary>
		/// Insert PATH + TAG into this list of Items.
		/// Creates sub-nodes as needed.
		/// </summary>
		/// <param name="PATH">
		/// Path under this node e.g. if this node is 'a/' then PATH 
		/// would be 'b/c.x', as opposed to 'a/b/c.x'.
		/// </param>
		/// <param name="TAG">Payload to be attached to leaf node.</param>
		public DERIVED_T Insert( string PATH, TAG_T TAG )
		{
			if( PATH.IsNullOrEmpty() ) return null;

			var index     = PATH.IndexOf('/');
			var name      = index < 1 ? PATH : PATH.Substring(0, index + 1);
			var remaining = PATH.Substring(name.Length);

			if( m_items == null ) m_items = new();

			// try add branch to existing node.
			// item.Insert will recursively add to existing branch.
			if( remaining.Length > 0 ) {
				var item  = m_items.Find(name, Compare);
				if( item != null ) return item.Insert(remaining, TAG);
				remaining = PATH;  // need to create new branch for PATH
			}
			else {
				remaining = name;  // leaf
			}

			// CreateDerived will either create leaf or recursively create full branch
			var at   = m_items.IndexOfInsert(name, Compare);
			var node = CreateDerived(this as DERIVED_T, remaining, TAG);
			m_items.Insert(at, node);
			return node;
		}

		//...........................................................

		public void ForEachTag(
			Action<TAG_T, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		){
			if( m_tag != null ) HANDLER(m_tag, CANCEL, LOG);

			if( !m_items.IsNullOrEmpty() )
			foreach( var item in m_items ) {
				if( CANCEL.IsCancellationRequested ) break;
				item.ForEachTag(HANDLER, CANCEL, LOG);
			}
		}

		//...........................................................

		public DERIVED_T Find( Predicate<DERIVED_T> MATCH )
		{
			if( MATCH(this as DERIVED_T) ) return this as DERIVED_T;

			if( !m_items.IsNullOrEmpty() )
			foreach( var item in m_items ) {
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
			if( PATH.IsNullOrEmpty() || m_items.IsNullOrEmpty() ) return null;

			var index = PATH.IndexOf('/');
			var name  = index < 1 ? PATH : PATH.Substring(0, index + 1);

			var found  = m_items.Find(name, Compare);
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

			bool lhs_is_dir = Name.Last() == '/',
				 rhs_is_dir = RHS .Last() == '/';

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
			if( LHS == null && RHS == null ) return  0;
			if( LHS == null )                return -1;
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
			if( LHS == null && RHS == null ) return  0;
			if( LHS == null )                return -1;
			return LHS.CompareTo(RHS);
		}
	}
}

//=============================================================================
