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
using System.Collections;
using System.Collections.Generic;

//=============================================================================
namespace cmk
{
	// list interfaces have some conflicts.
	// may get ambiguous errors for some of these (see IReadOnlyList extensions).
	public static partial class _x_
	{
		public static bool IsNullOrEmpty(
			this IList LIST
		){
			return (LIST == null) || (LIST.Count < 1);
		}

		//.................................................

		/// <summary>
		/// Reverse scan to find last AS_T.
		/// </summary>
		public static int IndexOfLast<AS_T>(
			this IList LIST
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( LIST[i] is AS_T ) return i;
			}
			return -1;
		}

		/// <summary>
		/// Reverse scan to find last MATCH AS_T.
		/// </summary>
		public static int IndexOfLast<AS_T>(
			this IList LIST,
			Predicate<AS_T> MATCH
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( LIST[i] is AS_T as_t && MATCH(as_t) ) return i;
			}
			return -1;
		}

		//.................................................

		/// <summary>
		/// Reverse scan to find last AS_T.
		/// </summary>
		public static AS_T FindLast<AS_T>(
			this IList LIST
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( LIST[i] is AS_T as_t ) return as_t;
			}
			return default;
		}

		/// <summary>
		/// Reverse scan to find last MATCH AS_T.
		/// </summary>
		public static AS_T FindLast<AS_T>(
			this IList LIST,
			Predicate<AS_T> MATCH
		){
			if( LIST != null )
			for( var i = LIST.Count; i-- > 0; ) {
				if( LIST[i] is AS_T as_t && MATCH(as_t) ) return as_t;
			}
			return default;
		}

		//.................................................

		/// <summary>
		/// Add OBJECT to LIST iff not already in LIST.
		/// Use COMPARE.Equals to compare each element with OBJECT.
		/// Use EqualityComparer<OBJECT_T>.Default if COMPARE == null.
		/// </summary>
		/// <returns>True if Added, false if didn't Add e.g. already exists.</returns>
		public static bool AddUnique<OBJECT_T>(
			this IList<OBJECT_T>        LIST,
			OBJECT_T                    OBJECT,
			IEqualityComparer<OBJECT_T> COMPARE = null
		){
			if( LIST == null || OBJECT == null ) return false;

			if( COMPARE == null ) COMPARE = EqualityComparer<OBJECT_T>.Default;

			foreach( var item in LIST ) {
				if( COMPARE.Equals(OBJECT, item) ) return false;
			}

			LIST.Add(OBJECT);
			return true;
		}
	}
}

//=============================================================================
