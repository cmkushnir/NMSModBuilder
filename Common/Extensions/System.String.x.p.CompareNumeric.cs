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

//=============================================================================

namespace cmk
{
	public static partial class String
	{
		/// <summary>
		/// All embedded #'s are treated as positive and "01" > "1",
		/// i.e. in "blah-3blah" the '-' is a char, not part of the number.
		/// Does not handle decimals i.e. '.' is considered a char, not part of the number.
		/// </summary>
		public static int CompareNumeric( ReadOnlySpan<char> LHS, ReadOnlySpan<char> RHS, bool IGNORE_CASE = false )
		{
			if( LHS == null ) return -1;
			if( RHS == null ) return  1;

			var ll = LHS.Length;
			var lr = RHS.Length;

			for( int il = 0, ir = 0; il < ll && ir < lr; ++il, ++ir ) {
				var is_dl = char.IsDigit(LHS[il]);
				var is_dr = char.IsDigit(RHS[ir]);
				if( is_dl && is_dr ) {
					// skip leading 0's
					while( il < ll && LHS[il] == '0' ) ++il;
					while( ir < lr && RHS[ir] == '0' ) ++ir;

					// find end of digit string
					var idl = il; while( idl < ll && char.IsDigit(LHS[idl]) ) ++idl;
					var idr = ir; while( idr < lr && char.IsDigit(RHS[idr]) ) ++idr;

					// if diff # of digits then compare lengths
					var ldl = idl - il;
					var ldr = idr - ir;
					if( ldl < ldr ) return -1;
					if( ldl > ldr ) return  1;

					// parse digit strings, they have same # of char
					if( ldl > 0 ) {
						var sl = LHS.Slice(il, ldl);
						var sr = RHS.Slice(ir, ldr);
						var c  = sl.CompareTo(sr, StringComparison.Ordinal);
						if( c < 0 ) return -1;
						if( c > 0 ) return  1;
					}

					il = idl - 1;
					ir = idr - 1;
				}
				else { // not both digits
					var cl = is_dl ? LHS[il] : char.ToUpper(LHS[il]);
					var cr = is_dr ? RHS[ir] : char.ToUpper(RHS[ir]);
					if( cl < cr ) return -1;
					if( cl > cr ) return  1;
					if( !IGNORE_CASE ) {
						cl = LHS[il];
						cr = RHS[ir];
						if( cl < cr ) return -1;
						if( cl > cr ) return  1;
					}
				}
			}

			if( ll < lr ) return -1;
			if( ll > lr ) return  1;

			return 0;
		}
	}
}

//=============================================================================
