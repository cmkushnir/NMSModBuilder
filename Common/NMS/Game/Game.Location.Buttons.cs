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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk.NMS.Game.Location
{
    public class Buttons
	: System.Windows.Controls.WrapPanel
	{
		public Buttons() : base()
		{
			m_warning_icon = Resource.BitmapImage("Warning.png");

			foreach( var custom in NMS.Game.Location.Data.Custom ) {
				var release = custom.Release;
				var button  = new ImageButton{
					ToolTip = CreateTooltip(custom),
					Uri     = Resource.Uri("Smiley_Green.png"),
					Margin  = new(4, 0, 4, 0),
					Tag     = custom
				};
				if( release.MbincVersion != MBINC.Linked.Version ) button.Uri = Resource.Uri("Smiley_Red.png");
				else if( GitHub.Disabled )                         button.Uri = Resource.Uri("Smiley_Yellow.png");
				CustomButton.Add(button);
			}

			if( Game.Location.Data.HasGOG ) {
				GoGButton.Visibility = Visibility.Visible;
				GoGButton.ToolTip    = CreateTooltip(Game.Location.Data.GoG);
			}
			if( Game.Location.Data.HasSteam ) {
				SteamButton.Visibility = Visibility.Visible;
				SteamButton.ToolTip    = CreateTooltip(Game.Location.Data.Steam);
			}
			if( Game.Location.Data.HasGamePass ) {
				GamePassButton.Visibility = Visibility.Visible;
				GamePassButton.ToolTip    = CreateTooltip(Game.Location.Data.GamePass);
			}

			foreach( var button in CustomButton ) {
				Children.Add(button);
				button.Click += OnButtonClick;
			}
			Children.Add(GoGButton);
			Children.Add(SteamButton);
			Children.Add(GamePassButton);
			Children.Add(SelectButton);

			GoGButton     .Click += OnButtonClick;
			SteamButton   .Click += OnButtonClick;
			GamePassButton.Click += OnButtonClick;
			SelectButton  .Click += OnButtonClick;
		}

		//...........................................................

		public readonly List<ImageButton> CustomButton = new();

		public readonly ImageButton GoGButton = new() {
			ToolTip    = "GoG",
			Uri        = Resource.Uri("GOG.png"),
			Visibility = Visibility.Collapsed,
			Margin     = new(4, 0, 4, 0),
		};
		public readonly ImageButton SteamButton = new() {
			ToolTip    = "Steam",
			Uri        = Resource.Uri("Steam.png"),
			Visibility = Visibility.Collapsed,
			Margin     = new(4, 0, 4, 0),
		};
		public readonly ImageButton GamePassButton = new() {
			ToolTip    = "Game Pass",
			Uri        = Resource.Uri("GamePass.png"),
			Visibility = Visibility.Collapsed,
			Margin     = new(4, 0, 4, 0),
		};
		public readonly ImageButton SelectButton = new() {
			ToolTip = "Select",
			Uri     = Resource.Uri("Folder.png"),
			Margin     = new(4, 0, 0, 0),
		};

		protected readonly BitmapImage m_warning_icon;

		//...........................................................

		protected System.Windows.FrameworkElement CreateTooltip(
			NMS.Game.Location.Data DATA
		){
			var mbinc_path_string = System.IO.Path.Join(Resource.AppDirectory, "libMBIN.dll");

			var tooltip = new System.Windows.Controls.ToolTip();
			var border  = new System.Windows.FrameworkElementFactory(typeof(Border));
			var grid    = new System.Windows.FrameworkElementFactory(typeof(Grid));

			border.SetValue(Border.BorderBrushProperty, SystemColors.ActiveBorderBrush);
			border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
			border.SetValue(Border.BackgroundProperty, SystemColors.ControlBrush);
			border.SetValue(Border.PaddingProperty, new Thickness(4));

			grid.AppendChild(FrameworkElementFactory.CreateColumnAuto("Icon"));
			grid.AppendChild(FrameworkElementFactory.CreateColumnAuto("Version"));
			grid.AppendChild(FrameworkElementFactory.CreateColumnAuto("Meta"));
			grid.AppendChild(FrameworkElementFactory.CreateColumnStar());  // path

			grid.AppendChild(FrameworkElementFactory.CreateRowAuto());  // game
			grid.AppendChild(FrameworkElementFactory.CreateRowAuto());  // libmbin

			var game_version = FrameworkElementFactory.CreateTextBlock(0, 1, Brushes.Black, FontWeights.Bold);
			var game_meta    = FrameworkElementFactory.CreateTextBlock(0, 2, Brushes.Black, FontWeights.Bold);
			var game_path    = FrameworkElementFactory.CreateTextBlock(0, 3, Brushes.Black, FontWeights.Normal);

			var mbinc_icon    = FrameworkElementFactory.CreateImage    (1, 0);
			var mbinc_version = FrameworkElementFactory.CreateTextBlock(1, 1, Brushes.Black, FontWeights.Bold);
			var mbinc_meta    = FrameworkElementFactory.CreateTextBlock(1, 2, Brushes.Black, FontWeights.Bold);
			var mbinc_path    = FrameworkElementFactory.CreateTextBlock(1, 3, Brushes.Black, FontWeights.Normal);

			game_version.SetValue(TextBlock.TextProperty, DATA.Release.GameVersion.ToString(3));
			game_meta   .SetValue(TextBlock.TextProperty, DATA.Built.ToString("yyyy/MM/dd"));
			game_path   .SetValue(TextBlock.TextProperty, DATA.Path);

			mbinc_version.SetValue(TextBlock.TextProperty, DATA.Release.MbincVersion.ToString());
			mbinc_meta   .SetValue(TextBlock.TextProperty, NMS.MBINC.Linked?.Version.ToString());
			mbinc_path   .SetValue(TextBlock.TextProperty, mbinc_path_string);
			if( !System.IO.File.Exists(mbinc_path_string) ||
				NMS.MBINC.Linked.Version != DATA.Release.MbincVersion
			) {
				mbinc_icon.SetValue(Image.SourceProperty, m_warning_icon);
				mbinc_meta.SetValue(TextBlock.ForegroundProperty, Brushes.DarkRed);
				mbinc_path.SetValue(TextBlock.ForegroundProperty, Brushes.DarkRed);
			}
			else {
				mbinc_meta.SetValue(TextBlock.ForegroundProperty, Brushes.DarkGreen);
				mbinc_path.SetValue(TextBlock.ForegroundProperty, Brushes.DarkGreen);
			}

			grid.AppendChild(game_version);
			grid.AppendChild(game_meta);
			grid.AppendChild(game_path);

			grid.AppendChild(mbinc_icon);
			grid.AppendChild(mbinc_version);
			grid.AppendChild(mbinc_meta);
			grid.AppendChild(mbinc_path);

			border.AppendChild(grid);
			tooltip.Template = new() { VisualTree = border };

			return tooltip;
		}

		//...........................................................

		public async Task ButtonClick( ImageButton SENDER )
		{
			try {
				foreach( var button in CustomButton ) button.IsEnabled = false;
				GoGButton     .IsEnabled = false;
				SteamButton   .IsEnabled = false;
				GamePassButton.IsEnabled = false;
				SelectButton  .IsEnabled = false;

				NMS.Game.Data selected = null;

				     if( SENDER == GoGButton      ) selected = await Game.Data.CreateGoGAsync();
				else if( SENDER == SteamButton    ) selected = await Game.Data.CreateSteamAsync();
				else if( SENDER == GamePassButton ) selected = await Game.Data.CreateGamePassAsync();
				else if( SENDER == SelectButton   ) selected = await Game.Data.SelectAsync();
				else foreach( var button in CustomButton ) {
					if( SENDER == button ) {
						selected = await NMS.Game.Data.CreateAsync(button.Tag as NMS.Game.Location.Data);
						break;
					}
				}

				if( selected != null ) Game.Data.Selected = selected;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
			finally {
				SelectButton  .IsEnabled = true;
				SteamButton   .IsEnabled = true;
				GamePassButton.IsEnabled = true;
				GoGButton     .IsEnabled = true;
				foreach( var button in CustomButton ) button.IsEnabled = true;
			}
		}

		protected async void OnButtonClick( object SENDER )
		{
			await ButtonClick(SENDER as ImageButton);
		}
	}
}

//=============================================================================
