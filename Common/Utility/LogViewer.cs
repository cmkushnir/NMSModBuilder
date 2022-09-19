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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

//=============================================================================

namespace cmk
{
    public class LogViewer
	: cmk.MainDockPanel
	{
		// broadcast if double-click a path .../... in the code.
		public delegate void InfoDoubleClickEventHandler( NMS.PAK.Item.Info INFO, TextSearchData SEARCH );
		public event         InfoDoubleClickEventHandler InfoDoubleClick;

		//...........................................................

		public LogViewer( bool IS_VIRTUALIZING = true ) : base()
		{
			ToolGrid.Background = Brushes.Silver;

			ListBox = new(IS_VIRTUALIZING) {
				FontFamily = Resource.DefaultFont,
				FontSize   = Resource.DefaultFontSize,
			};

			ToolWrapPanelLeft.Children.Add(ClearButton);
			ToolWrapPanelLeft.Children.Add(SaveButton);

			ListBox.ItemTemplate = new() { VisualTree = CreateListBox() };

			ClientGrid.Children.Add(ListBox);

			ListBox.MouseDoubleClick += OnListBoxMouseDoubleClick;

			ClearButton.Click += ( S ) => Log?.Clear();
			SaveButton .Click += ( S ) => Log?.Save();

			Loaded += ( S, E ) => ListBox.AutoScroll = true;
		}

		//...........................................................

		public readonly ImageButton ClearButton = new() {
			Uri       = Resource.Uri("Clear.png"),
			ToolTip   = "Clear",
			IsEnabled = false,
			Margin    = new(0, 0, 8, 0),
		};
		public readonly ImageButton SaveButton = new() {
			Uri       = Resource.Uri("Save.png"),
			ToolTip   = "Save",
			IsEnabled = false,
		};

		public readonly ListBox ListBox;  // construct w/ IS_VIRTUALIZING

		//...........................................................

		public Log Log {
			get => ListBox.ItemsSource as Log;
			set {
				if( Log == value ) return;
				if( ListBox.ItemsSource != null ) {
					BindingOperations.DisableCollectionSynchronization(ListBox.ItemsSource);
				}
				ListBox.ItemsSource = value;
				if( ListBox.ItemsSource != null ) {
					BindingOperations.EnableCollectionSynchronization(ListBox.ItemsSource, ListBox.ItemsSource);
				}
				ClearButton.IsEnabled = value != null;
				SaveButton .IsEnabled = value != null;
			}
		}

		//...........................................................

		protected System.Windows.FrameworkElementFactory CreateListBox()
		{
			var grid_factory = FrameworkElementFactory.CreateGrid(-1, -1);
			grid_factory.SetValue(Grid.BackgroundProperty, new Binding("Background") { Mode = BindingMode.OneWay });
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Icon"));
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnStar());

			var icon_factory = FrameworkElementFactory.CreateImage(-1, 0, "Icon");
			icon_factory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Top);

			var text_factory = FrameworkElementFactory.CreateTextBox(-1, 1, null, FontWeights.Normal, "Text");
			text_factory.SetValue(TextBox.ForegroundProperty, new Binding("Foreground") { Mode = BindingMode.OneWay });
			text_factory.SetValue(TextBox.FontWeightProperty, new Binding("Weight")     { Mode = BindingMode.OneWay });

			grid_factory.AppendChild(icon_factory);
			grid_factory.AppendChild(text_factory);

			return grid_factory;
		}

		//...........................................................

		protected void OnListBoxMouseDoubleClick( object SENDER, MouseButtonEventArgs ARGS )
		{
			var sender  = SENDER as ListBox;
			if( sender == null ) return;

			var handler  = InfoDoubleClick;
			if( handler == null ) return;

			var selected  = sender.SelectedItem as LogItem;
			if( selected == null || selected.Text.IsNullOrEmpty() ) return;

			var info  = selected.Tag0 as NMS.PAK.Item.Info;
			if( info == null ) {
				var data  = selected.Tag0 as NMS.PAK.Item.Data;
				if( data != null ) info = data.Info;
			}
			if( info == null ) {
				var matches = Resource.ItemPathRegex.Matches(selected.Text);
				foreach( var match in matches ) {
					info = NMS.Game.Data.Selected.PCBANKS.FindInfo(match.ToString());
					if( info != null ) break;
				}
			}

			var search = selected.Tag1 as TextSearchData;

			if( info != null ) handler.Invoke(info, search);
		}
	}
}

//=============================================================================
