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
using System.Windows;
using System.Windows.Media;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// Walk upstream through visual tree parents looking for PARENT_T.
		/// </summary>
		public static PARENT_T FindParent<PARENT_T>( this DependencyObject OBJECT )
		where PARENT_T : System.Windows.DependencyObject
		{
			if( OBJECT == null ) return null;

			var parent  = VisualTreeHelper.GetParent(OBJECT);
			if( parent == null ) return null;

			var cast_parent  = parent as PARENT_T;
			if( cast_parent != null ) return cast_parent;

			return FindParent<PARENT_T>(parent);
		}

		//...........................................................

		/// <summary>
		/// Depth-first recursion through children to find first CHILD_T.
		/// Note: can't actually get child until visual tree available i.e. after root Window loaded.
		/// </summary>
		public static CHILD_T GetFirstChild<CHILD_T>( this DependencyObject OBJECT )
		where CHILD_T : System.Windows.DependencyObject
		{
			if( OBJECT != null )
			for( var i = 0; i < VisualTreeHelper.GetChildrenCount(OBJECT); ++i ) {
				var child   = VisualTreeHelper.GetChild(OBJECT, i);
				var result  = (child as CHILD_T) ?? GetFirstChild<CHILD_T>(child);
				if( result != null ) return result;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Depth-first recursion through children to find all CHILD_T.
		/// Note: can't actually get child until visual tree available i.e. after root Window loaded.
		/// </summary>
		public static IEnumerable<CHILD_T> GetChildren<CHILD_T> ( this DependencyObject OBJECT )
		where CHILD_T : System.Windows.DependencyObject
		{
			if( OBJECT != null )
			for( var i = 0; i < VisualTreeHelper.GetChildrenCount(OBJECT); ++i ) {
				var child   = VisualTreeHelper.GetChild(OBJECT, i);
				var result  = (child as CHILD_T) ?? GetFirstChild<CHILD_T>(child);
				if( result != null ) yield return result;
			}
		}
	}
}

//=============================================================================
