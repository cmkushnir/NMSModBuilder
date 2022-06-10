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

using System.IO;
using System.Windows.Media;
using System.Windows.Shapes;

//=============================================================================

namespace cmk
{
	public class HexDiffer
	: System.Windows.Controls.Grid
	{
		public HexDiffer( Stream LHS, Stream RHS ) : base()
		{
			ColumnDefinitions.AddStar();
			ColumnDefinitions.AddPixel(4);
			ColumnDefinitions.AddStar();

			SetColumn(ViewerLeft,  0);
			SetColumn(Splitter,    1);
			SetColumn(ViewerRight, 2);

			Children.Add(ViewerLeft);
			Children.Add(Splitter);
			Children.Add(ViewerRight);

			StreamLeft  = LHS;
			StreamRight = RHS;
		}

		//...........................................................

		public readonly Rectangle Splitter    = new() { Fill = Brushes.Black };
		public readonly HexViewer ViewerLeft  = new(null);
		public readonly HexViewer ViewerRight = new(null);

		//...........................................................

		public Stream StreamLeft {
			get { return ViewerLeft.Raw; }
			set { ViewerLeft.Raw = value; }
		}

		public Stream StreamRight {
			get { return ViewerRight.Raw; }
			set { ViewerRight.Raw = value; }
		}
	}
}

//=============================================================================
