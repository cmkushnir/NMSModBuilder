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
	public static partial class _x_
	{
		/// <summary>
		/// Forward scan to find first AS_T.
		/// Return -1 if LIST is null or not found.
		/// </summary>
		public static int IndexOfFirst<AS_T>(
			this IEnumerable LIST
		){
			if( LIST != null ) {
				int index = -1;
				foreach( var item in LIST ) {
					++index;
					if( item is AS_T ) return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Forward scan to find first MATCH AS_T.
		/// Return -1 if LIST is null or MATCH is null or not found.
		/// </summary>
		public static int IndexOfFirst<AS_T>(
			this IEnumerable     LIST,
			     Predicate<AS_T> MATCH
		){
			if( LIST != null && MATCH != null ) {
				int index = -1;
				foreach( var item in LIST ) {
					++index;
					if( item is AS_T as_t && MATCH(as_t) ) return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Forward scan to find first MATCH.
		/// Return -1 if LIST is null or MATCH is null or not found.
		/// </summary>
		public static int IndexOfFirst<AS_T>(
			this IEnumerable<AS_T> LIST,
			     Predicate<AS_T>   MATCH
		){
			if( LIST != null && MATCH != null ) {
				int index = -1;
				foreach( var item in LIST ) {
					++index;
					if( MATCH(item) ) return index;
				}
			}
			return -1;
		}

		//.................................................

		/// <summary>
		/// Forward scan to find first AS_T.
		/// Return default if LIST is null or not found.
		/// </summary>
		public static AS_T FindFirst<AS_T>(
			this IEnumerable LIST
		){
			if( LIST != null )
			foreach( var item in LIST ) {
				if( item is AS_T as_t ) return as_t;
			}
			return default;
		}

		/// <summary>
		/// Forward scan to find first MATCH AS_T.
		/// Return default if LIST is null or MATCH is null or not found.
		/// </summary>
		public static AS_T FindFirst<AS_T>(
			this IEnumerable     LIST,
			     Predicate<AS_T> MATCH
		){
			if( LIST != null && MATCH != null )
			foreach( var item in LIST ) {
				if( item is AS_T as_t && MATCH(as_t) ) return as_t;
			}
			return default;
		}

		/// <summary>
		/// Forward scan to find first MATCH.
		/// Return default if LIST is null or MATCH is null or not found.
		/// </summary>
		public static AS_T FindFirst<AS_T>(
			this IEnumerable<AS_T> LIST,
			     Predicate<AS_T>   MATCH
		){
			if( LIST != null && MATCH != null )
			foreach( var item in LIST ) {
				if( MATCH(item) ) return item;
			}
			return default;
		}

		//.................................................

		/// <summary>
		/// Forward scan to find all AS_T.
		/// </summary>
		public static IEnumerable<AS_T> FindAll<AS_T>(
			this IEnumerable LIST
		){
			if( LIST != null )
			foreach( var item in LIST ) {
				if( item is AS_T as_t ) yield return as_t;
			}
		}

		/// <summary>
		/// Forward scan to find all MATCH's AS_T.
		/// </summary>
		public static IEnumerable<AS_T> FindAll<AS_T>(
			this IEnumerable     LIST,
			     Predicate<AS_T> MATCH
		){
			if( LIST != null && MATCH != null )
			foreach( var item in LIST ) {
				if( item is AS_T as_t && MATCH(as_t) ) yield return as_t;
			}
		}

		/// <summary>
		/// Forward scan to find all MATCH's.
		/// </summary>
		public static IEnumerable<AS_T> FindAll<AS_T>(
			this IEnumerable<AS_T> LIST,
			     Predicate<AS_T>   MATCH
		){
			if( LIST != null && MATCH != null )
			foreach( var item in LIST ) {
				if( MATCH(item) ) yield return item;
			}
		}
	}
}

//=============================================================================
