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
using System.Runtime.CompilerServices;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// The object passed to IS_ALIVE will be non-null.
		/// </summary>
		[Obsolete]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool WaitForPendingFinalizers(
			this WeakReference WEAK,
			Predicate<object>  IS_ALIVE = null
		){
			if( WEAK     == null ) return true;
			if( IS_ALIVE == null ) IS_ALIVE = OBJECT => true;

			for( var i = 0; WEAK.IsAlive && (i < 10); ++i ) {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				var obj  = WEAK.Target;
				if( obj == null ||
					!IS_ALIVE(obj)
				)	return true;
				System.Threading.Thread.Sleep(8);
			}

			return !WEAK.IsAlive;
		}

		//...........................................................

		/// <summary>
		/// The object passed to IS_ALIVE will be non-null.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool WaitForPendingFinalizers<TYPE_T>(
			this WeakReference<TYPE_T> WEAK,
			Predicate<TYPE_T>          IS_ALIVE = null
		)
		where TYPE_T : class
		{
			if( WEAK     == null ) return true;
			if( IS_ALIVE == null ) IS_ALIVE = OBJECT => true;

			for( var i = 0; i < 10; ++i ) {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				TYPE_T obj = null;
				if( !WEAK.TryGetTarget(out obj) ||
					!IS_ALIVE(obj)
				)	return true;
				System.Threading.Thread.Sleep(8);
			}

			return false;
		}
	}
}

//=============================================================================
