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
using System.Text;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		public static void Clear( this StringBuilder BUILDER )
		{
			if( BUILDER != null ) BUILDER.Length = 0;
		}

		//...........................................................

		/// <summary>
		/// Append list of objects to string builder.
		/// Use prefix, separator, suffix to bracket and separate objects.
		/// </summary>
		public static void Append<T>(
			this StringBuilder BUILDER,
			IEnumerator<T>     ITEMS,
			string             PREFIX     = "",
			string             SEPARATOR  = ", ",
			string             SUFFIX     = "",
			string             NULLSTRING = "",
			Func<T, string>    STRINGFUNC = null
		){
			if( BUILDER == null ) return;
			if( ITEMS   == null ) {
				BUILDER.Append(PREFIX + SUFFIX);
				return;
			}

			if( NULLSTRING.IsNullOrEmpty() ) NULLSTRING = "";
			if( !PREFIX.IsNullOrEmpty() )    BUILDER.Append(PREFIX);

			if( STRINGFUNC == null ) STRINGFUNC = OBJ_T => OBJ_T.ToString();

			ITEMS.Reset();
			if( ITEMS.MoveNext() && ITEMS.Current != null ) {
				var s0 = STRINGFUNC(ITEMS.Current);
				BUILDER.Append(s0.IsNullOrEmpty() ? NULLSTRING : s0);

				var sep = !SEPARATOR.IsNullOrEmpty();

				while( ITEMS.MoveNext() && ITEMS.Current != null ) {
					var sn = STRINGFUNC(ITEMS.Current);
					if( sep ) BUILDER.Append(SEPARATOR);
					BUILDER.Append(sn.IsNullOrEmpty() ? NULLSTRING : sn);
				}
			}

			if( !SUFFIX.IsNullOrEmpty() ) BUILDER.Append(SUFFIX);
		}

		//...........................................................

		/// <summary>
		/// Append list of objects to string builder.
		/// Use prefix, separator, suffix to bracket and separate objects.
		/// </summary>
		public static void Append(
			this StringBuilder  BUILDER,
			IEnumerator         ITEMS,
			string              PREFIX     = "",
			string              SEPARATOR  = ", ",
			string              SUFFIX     = "",
			string              NULLSTRING = "",
			Func<object,string> STRINGFUNC = null
		){
			if( BUILDER == null ) return;
			if( ITEMS   == null ) {
				BUILDER.Append(PREFIX + SUFFIX);
				return;
			}

			if( NULLSTRING.IsNullOrEmpty() ) NULLSTRING = "";
			if( !PREFIX.IsNullOrEmpty() )    BUILDER.Append(PREFIX);

			if( STRINGFUNC == null ) STRINGFUNC = OBJ => OBJ.ToString();

			ITEMS.Reset();
			if( ITEMS.MoveNext() && ITEMS.Current != null ) {
				var s0 = STRINGFUNC(ITEMS.Current);
				BUILDER.Append(s0.IsNullOrEmpty() ? NULLSTRING : s0);

				var sep = !SEPARATOR.IsNullOrEmpty();

				while( ITEMS.MoveNext() && ITEMS.Current != null ) {
					var sn = STRINGFUNC(ITEMS.Current);
					if( sep ) BUILDER.Append(SEPARATOR);
					BUILDER.Append(sn.IsNullOrEmpty() ? NULLSTRING : sn);
				}
			}

			if( !SUFFIX.IsNullOrEmpty() ) BUILDER.Append(SUFFIX);
		}
	}
}

//=============================================================================
