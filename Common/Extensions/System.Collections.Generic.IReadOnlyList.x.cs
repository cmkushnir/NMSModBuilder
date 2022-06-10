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

//=============================================================================
namespace cmk
{
	// list interfaces have some conflicts.
	// may get ambiguous errors for some of these (see IList extensions).
	public static partial class _x_
	{
		//public static bool IsNullOrEmpty<OBJECT_T>(
		//	this IReadOnlyList<OBJECT_T> LIST
		//){
		//	return (LIST == null) || (LIST.Count < 1);
		//}

		//.................................................

		/// <summary>
		/// Reverse scan to find last MATCH AS_T.
		/// </summary>
		public static int IndexOfLast<AS_T>(
			this IReadOnlyList<AS_T> LIST,
			     Predicate<AS_T>     MATCH
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( MATCH(LIST[i]) ) return i;
			}
			return -1;
		}

		//.................................................

		/// <summary>
		/// Find insert index for KEY in sorted LIST using bsearch.
		/// </summary>
		public static int BsearchIndexOfInsert<OBJECT_T, KEY_T>(
			this IReadOnlyList<OBJECT_T>    LIST,
			     KEY_T                      KEY,  // RHS in compare
			     Func<OBJECT_T, KEY_T, int> COMPARE
		){
			if( LIST == null || COMPARE == null ) return -1;

			int min = 0;
			int max = LIST.Count - 1;

			while( min <= max ) {
				int mid = (min + max) / 2;
				int c  = COMPARE(LIST[mid], KEY);
				if( c == 0 ) return mid;  // todo: find last matching index
				if( c <  0 ) min = mid + 1;
				else         max = mid - 1;
			}

			return min >= 0 ? min : max;
		}

		//.................................................

		/// <summary>
		/// Find index of KEY in unique sorted LIST using bsearch.
		/// </summary>
		public static int BsearchIndexOf<OBJECT_T, KEY_T>(
			this IReadOnlyList<OBJECT_T>    LIST,
			     KEY_T                      KEY,  // RHS in compare
			     Func<OBJECT_T, KEY_T, int> COMPARE
		){
			if( LIST == null || COMPARE == null ) return -1;

			int min = 0;
			int max = LIST.Count - 1;

			while( min <= max ) {
				int mid = (min + max) / 2;
				int c  = COMPARE(LIST[mid], KEY);
				if( c == 0 ) return mid;
				if( c <  0 ) min = mid + 1;
				else         max = mid - 1;
			}

			return -1;
		}

		//.................................................

		public static AS_T Find<AS_T>(
			this IReadOnlyList<AS_T> LIST,
			     Predicate<AS_T>     MATCH,
			     AS_T                ON_NULL = default
		){
			if( LIST != null )
			for( var i = 0; i < LIST.Count; ++i ) {
				if( MATCH(LIST[i]) ) return LIST[i];
			}
			return ON_NULL;
		}

		/// <summary>
		/// Find KEY in unique sorted LIST using bsearch.
		/// </summary>
		public static OBJECT_T Bsearch<OBJECT_T, KEY_T>(
			this IReadOnlyList<OBJECT_T>    LIST,
			     KEY_T                      KEY,  // RHS in compare
			     Func<OBJECT_T, KEY_T, int> COMPARE,
			     OBJECT_T                   ON_NULL = default
		){
			var    index = BsearchIndexOf(LIST, KEY, COMPARE);
			return index < 0 ? ON_NULL : LIST[index];
		}

		//.................................................

		/// <summary>
		/// Reverse scan to find last MATCH AS_T.
		/// </summary>
		public static AS_T FindLast<AS_T>(
			this IReadOnlyList<AS_T> LIST,
			     Predicate<AS_T>     MATCH,
			     AS_T                ON_NULL = default
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( MATCH(LIST[i]) ) return LIST[i];
			}
			return ON_NULL;
		}
	}
}

//=============================================================================
