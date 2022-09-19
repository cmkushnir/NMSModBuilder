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
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
    public class BitmapViewer
	: cmk.MainDockPanel
	{
		public static int BackgroundRadioIndex { get; protected set; } = 0;
		public static int ScaleRadioIndex      { get; protected set; } = 0;

		public static readonly Brush CheckerBrush = Resource.NewCheckerBrush();

		//...........................................................

		public BitmapViewer() : base()
		{
			ToolGrid.Background = Brushes.LightGray;

			BackgroundRadioPanel.Children.Add(CheckerBackgroundButton);
			BackgroundRadioPanel.Children.Add(BlackBackgroundButton);
			BackgroundRadioPanel.Children.Add(WhiteBackgroundButton);

			ScaleRadioPanel.Children.Add(StretchScaleButton);
			ScaleRadioPanel.Children.Add(TileScaleButton);
			ScaleRadioPanel.Children.Add(CenterScaleButton);

			ToolWrapPanelLeft  .Children.Add(BackgroundRadioPanel);
			ToolWrapPanelCenter.Children.Add(Label);
			ToolWrapPanelRight .Children.Add(ScaleRadioPanel);

			ClientGrid.Children.Add(Image);

			BackgroundRadioPanel.RadioButtonChanged += ( S, E ) => {
				BackgroundRadioIndex = S.SelectedIndex;
				UpdateImage();
			};

			ScaleRadioPanel.RadioButtonChanged += ( S, E ) => {
				ScaleRadioIndex = S.SelectedIndex;
				UpdateImage();
			};

			BackgroundRadioPanel.SelectedIndex = BackgroundRadioIndex;
			ScaleRadioPanel.SelectedIndex      = ScaleRadioIndex;
		}

		//...........................................................

		public readonly ImageRadioPanel BackgroundRadioPanel = new() {
			Orientation = Orientation.Horizontal,
		};
		public readonly ImageRadioButton CheckerBackgroundButton = new() {
			ToolTip = "Checker",
			Uri     = Resource.Uri("Checker.png"),
			Tag     = CheckerBrush,
		};
		public readonly ImageRadioButton BlackBackgroundButton = new() {
			ToolTip = "Solid Black",
			Uri     = Resource.Uri("Black.png"),
			Tag     = Brushes.Black,
		};
		public readonly ImageRadioButton WhiteBackgroundButton = new() {
			ToolTip = "Solid White",
			Uri     = Resource.Uri("White.png"),
			Tag     = Brushes.White,
		};

		public readonly Label Label = new() {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
		};

		public readonly ImageRadioPanel ScaleRadioPanel = new() {
			Orientation = Orientation.Horizontal
		};
		public readonly ImageRadioButton StretchScaleButton = new() {
			ToolTip = "Stretch",
			Uri     = Resource.Uri("Stretch.png"),
		};
		public readonly ImageRadioButton TileScaleButton = new() {
			ToolTip = "Tile",
			Uri     = Resource.Uri("Tile.png"),
		};
		public readonly ImageRadioButton CenterScaleButton = new() {
			ToolTip = "Center",
			Uri     = Resource.Uri("Center.png"),
		};

		public readonly Image Image = new();

		//...........................................................

		public string LabelText {
			get { return Label.Content as string; }
			set { Label.Content = value; }
		}

		//...........................................................

		protected BitmapSource m_source;
		protected Brush        m_tile_brush;

		public BitmapSource Source {
			get { return m_source; }
			set {
				if( m_source == value ) return;
				m_source = value;

				m_tile_brush = (m_source == null) ?
					Brushes.LightGray : new ImageBrush {
						ViewportUnits  = BrushMappingMode.Absolute,
						Viewport       = new(0, 0, m_source.PixelWidth, m_source.PixelHeight),
						TileMode       = TileMode.Tile,
						ImageSource    = m_source,
					}
				;

				UpdateImage();
			}
		}

		//...........................................................

		protected void UpdateImage()
		{
			var tile_button  = ScaleRadioPanel.Children[ScaleRadioIndex] as ImageRadioButton;
			if( tile_button == TileScaleButton ) {
				ClientGrid.Background = m_tile_brush;
				Image.Source = null;
				return;
			}

			var back_button = BackgroundRadioPanel.Children[BackgroundRadioIndex] as ImageRadioButton;
			ClientGrid.Background = back_button.Tag as Brush;

			Image.Source  = m_source;
			Image.Stretch = tile_button == StretchScaleButton ?
				Stretch.Uniform : Stretch.None
			;
		}
	}
}

//=============================================================================
