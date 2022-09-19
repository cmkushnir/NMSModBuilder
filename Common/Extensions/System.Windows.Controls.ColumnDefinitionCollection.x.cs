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

using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace cmk
{
    public static partial class _x_
	{
		/// <summary>
		/// Add auto-width column.
		/// </summary>
		public static void AddAuto( this ColumnDefinitionCollection COLUMNS )
		{
			COLUMNS.Add(new() { Width = new(0, GridUnitType.Auto) });
		}

		//...........................................................

		/// <summary>
		/// Add fill-width column.
		/// </summary>
		public static void AddStar( this ColumnDefinitionCollection COLUMNS, double VALUE = 1 )
		{
			COLUMNS.Add(new() { Width = new(VALUE, GridUnitType.Star) });
		}

		//...........................................................

		/// <summary>
		/// Add specific pixel-width column.
		/// </summary>
		public static void AddPixel( this ColumnDefinitionCollection COLUMNS, double VALUE )
		{
			COLUMNS.Add(new() { Width = new(VALUE, GridUnitType.Pixel) });
		}
	}
}

//=============================================================================
