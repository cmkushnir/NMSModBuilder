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
    public class TextBoxDialog
	: System.Windows.Window
	{
		public TextBoxDialog() : base()
		{
			Title = "Edit Text";

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SizeToContent = SizeToContent.Width;
			Topmost   = true;
			MinHeight = 105;
			MaxHeight = 105;
			MinWidth  = 256;

			Grid.Children.Add(TextBox);
			Grid.Children.Add(OkButton);
			Grid.Children.Add(CancelButton);

			Content = Grid;

			OkButton.Click     += OnOkClick;
			CancelButton.Click += OnCancelClick;

			Loaded += ( S, E ) => TextBox.Text = Text;
		}

		//...........................................................

		public readonly Grid Grid = new();

		public readonly TextBox TextBox = new() {
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment   = VerticalAlignment.Top,
			TextWrapping        = TextWrapping.NoWrap,
			Margin              = new(10, 10, 10, 0),
		};

		public readonly Button OkButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Margin  = new(0, 0, 90, 10),
			Width   = 75,
			Content = "OK",
		};
		public readonly Button CancelButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Margin  = new(0, 0, 10, 10),
			Width   = 75,
			Content = "Cancel",
		};

		//...........................................................

		public string Text { get; set; }

		//...........................................................

		protected void OnOkClick( object SENDER, RoutedEventArgs ARGS )
		{
			Text = TextBox.Text;
			DialogResult = true;
			Close();
		}

		//...........................................................

		protected void OnCancelClick( object SENDER, RoutedEventArgs ARGS )
		{
			DialogResult = false;
			Close();
		}
	}
}

//=============================================================================
