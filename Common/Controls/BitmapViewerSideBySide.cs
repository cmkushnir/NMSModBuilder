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

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

//=============================================================================

namespace cmk
{
    public class BitmapViewerSideBySide
	: System.Windows.Controls.Grid
	{
		public BitmapViewerSideBySide() : base()
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
		}

		//...........................................................

		public readonly Rectangle    Splitter    = new() { Fill = Brushes.Black };
		public readonly BitmapViewer ViewerLeft  = new();
		public readonly BitmapViewer ViewerRight = new();

		//...........................................................

		public string LabelLeft {
			get { return ViewerLeft.LabelText; }
			set { ViewerLeft.LabelText = value; }
		}

		public string LabelRight {
			get { return ViewerRight.LabelText; }
			set { ViewerRight.LabelText = value; }
		}

		//...........................................................

		public BitmapSource SourceLeft {
			get { return ViewerLeft.Source; }
			set { ViewerLeft.Source = value; }
		}

		public BitmapSource SourceRight {
			get { return ViewerRight.Source; }
			set { ViewerRight.Source = value; }
		}
	}
}

//=============================================================================
