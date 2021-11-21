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
	public static partial class _x_
	{
		/// <summary>
		/// All parts <= 0.
		/// </summary>
		public static bool IsNull( this Version VERSION )
		{
			return
				VERSION          == null || (
				VERSION.Major    <= 0 &&
				VERSION.Minor    <= 0 &&
				VERSION.Build    <= 0 &&
				VERSION.Revision <= 0
			);
		}

		//...........................................................

		/// <summary>
		/// Convert any parts that are < 0 to 0.
		/// By default NET uses -1 to indicate part not used.
		/// </summary>
		public static Version Normalize( this Version VERSION )
		{
			if( VERSION == null ) return new();

			var major    = VERSION.Major;
			var minor    = VERSION.Minor;
			var build    = VERSION.Build;
			var revision = VERSION.Revision;

			if( major    < 1 ) major    = 0;
			if( minor    < 1 ) minor    = 0;
			if( build    < 1 ) build    = 0;
			if( revision < 1 ) revision = 0;

			return new(major, minor, build, revision);
		}

		//...........................................................

		public static Version ClampLower( this Version VERSION, Version CLAMP )
		{
			return (VERSION < CLAMP) ? CLAMP : VERSION;
		}

		//...........................................................

		public static Version ClampUpper( this Version VERSION, Version CLAMP )
		{
			return (VERSION > CLAMP) ? CLAMP : VERSION;
		}

		//...........................................................

		public static Version Clamp( this Version VERSION, Version MIN, Version MAX )
		{
			return ClampUpper(ClampLower(VERSION, MIN), MAX);
		}
	}
}

//=============================================================================
