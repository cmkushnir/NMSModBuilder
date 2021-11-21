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
using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace cmk.NMS.Game.Location
{
	public class Buttons
	: System.Windows.Controls.WrapPanel
	{
		public Buttons() : base()
		{
			foreach( var custom in NMS.Game.Location.Data.Custom ) {
				var release = custom.Release;
				var button  = new ImageButton{
					ToolTip = $"{release.GameVersion.ToString(3)} {release.MbincVersion} {custom.Path}",
					Uri     = Resource.Uri("Smiley_Green.png"),
					Tag     = custom
				};
				     if( release.MbincVersion != MBINC.Linked.Version ) button.Uri = Resource.Uri("Smiley_Red.png");
				else if( GitHub.Disabled )                              button.Uri = Resource.Uri("Smiley_Yellow.png");
				CustomButton.Add(button);
				ToolTipService.SetShowDuration(button, 60000);
			}

			if( Game.Location.Data.HasGOG ) {
				var location = Game.Location.Data.GoG;
				var release  = location.Release;
				GoGButton.Visibility = Visibility.Visible;
				GoGButton.ToolTip    = $"{release.GameVersion.ToString(3)} {release.MbincVersion} {location.Path}";
				ToolTipService.SetShowDuration(GoGButton, 60000);
			}
			if( Game.Location.Data.HasSteam ) {
				var location = Game.Location.Data.Steam;
				var release  = location.Release;
				SteamButton.Visibility = Visibility.Visible;
				SteamButton.ToolTip    = $"{release.GameVersion.ToString(3)} {release.MbincVersion} {location.Path}";
				ToolTipService.SetShowDuration(SteamButton, 60000);
			}

			foreach( var button in CustomButton ) {
				Children.Add(button);
				ToolTipService.SetShowDuration(button, 60000);
				button.Click += OnButtonClick;
			}
			Children.Add(GoGButton);
			Children.Add(SteamButton);
			Children.Add(SelectButton);

			ToolTipService.SetShowDuration(GoGButton,    60000);
			ToolTipService.SetShowDuration(SteamButton,  60000);
			ToolTipService.SetShowDuration(SelectButton, 60000);

			GoGButton   .Click += OnButtonClick;
			SteamButton .Click += OnButtonClick;
			SelectButton.Click += OnButtonClick;
		}

		//...........................................................

		public readonly List<ImageButton> CustomButton = new();

		public readonly ImageButton GoGButton = new() {
			ToolTip    = "GoG",
			Uri        = Resource.Uri("GOG.png"),
			Visibility = Visibility.Collapsed,
			Margin     = new(8, 0, 0, 0),
		};
		public readonly ImageButton SteamButton = new() {
			ToolTip    = "Steam",
			Uri        = Resource.Uri("Steam.png"),
			Visibility = Visibility.Collapsed,
			Margin     = new(8, 0, 8, 0),
		};
		public readonly ImageButton SelectButton = new() {
			ToolTip = "Select",
			Uri     = Resource.Uri("Folder.png"),
		};

		//...........................................................

		protected async void OnButtonClick( object SENDER, System.Windows.Input.MouseButtonEventArgs ARGS )
		{
			try {
				foreach( var button in CustomButton ) button.IsEnabled = false;
				GoGButton   .IsEnabled = false;
				SteamButton .IsEnabled = false;
				SelectButton.IsEnabled = false;

				NMS.Game.Data selected = null;

				     if( SENDER == GoGButton )    selected = await Game.Data.CreateGoGAsync();
				else if( SENDER == SteamButton )  selected = await Game.Data.CreateSteamAsync();
				else if( SENDER == SelectButton ) selected = await Game.Data.SelectAsync();
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
				SelectButton.IsEnabled = true;
				SteamButton .IsEnabled = true;
				GoGButton   .IsEnabled = true;
				foreach( var button in CustomButton ) button.IsEnabled = true;
			}
		}
	}
}

//=============================================================================
