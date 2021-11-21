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
	public static partial class Array_x
	{
		/// <summary>
		/// Use msvcrt memcmp to compare min length of LHS, RHS.
		/// If common length elements are equal then compare lengths.
		/// </summary>
		public static int MemCmp( byte[] LHS, byte[] RHS )
		{
			if( LHS == RHS  ) return  0;
			if( LHS == null ) return -1;
			if( RHS == null ) return  1;

			var length   = Math.Min(LHS.Length, RHS.Length);
			var compare  = PInvoke.memcmp(LHS, RHS, length);
			if( compare != 0 ) return compare;

			if( LHS.Length < length ) return -1;
			if( RHS.Length < length ) return  1;

			return 0;
		}
	}
}

//=============================================================================
