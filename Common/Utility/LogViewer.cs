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
using System.Windows.Media;

//=============================================================================

namespace cmk
{
	public class LogViewer
	: cmk.MainDockPanel
	{
		public LogViewer() : base()
		{
			ToolGrid.Background = Brushes.Silver;

			ToolWrapPanelLeft.Children.Add(ClearButton);
			ToolWrapPanelLeft.Children.Add(SaveButton);

			var icon_factory = FrameworkElementFactory.CreateImage(-1, 0, "Icon");
			icon_factory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Top);

			var text_factory = FrameworkElementFactory.CreateTextBox(-1, 1, null, FontWeights.Normal, "Text");
			text_factory.SetValue(TextBox.ForegroundProperty, new Binding("Foreground") { Mode = BindingMode.OneWay });
			text_factory.SetValue(TextBox.FontWeightProperty, new Binding("Weight")     { Mode = BindingMode.OneWay });

			var grid_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			grid_factory.SetValue(Grid.BackgroundProperty, new Binding("Background") { Mode = BindingMode.OneWay });

			var col = FrameworkElementFactory.CreateColumnAuto("Icon");
			grid_factory.AppendChild(col);
			    col = FrameworkElementFactory.CreateColumnStar();
			grid_factory.AppendChild(col);

			grid_factory.AppendChild(icon_factory);
			grid_factory.AppendChild(text_factory);

			ListBox.ItemTemplate = new() { VisualTree = grid_factory };

			ClientGrid.Children.Add(ListBox);

			ClearButton.Click += ( S, E ) => Log?.Clear();
			 SaveButton.Click += ( S, E ) => Log?.Save();

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

		public readonly ListBox ListBox = new() {
			FontFamily = Resource.DefaultFont,
			FontSize   = Resource.DefaultFontSize,
		};

		//...........................................................

		public Log Log {
			get { lock( ListBox ) return ListBox.ItemsSource as Log; }
			set {
				lock( ListBox ) {
					if( Log == value ) return;
					if( ListBox.ItemsSource != null ) {
						// Log calls CollectionChanged?.DispatcherInvoke
						// to ensure binding updates are on UI thread.
						// EnableCollectionSynchronization doesn't work in all cases.
						//BindingOperations.DisableCollectionSynchronization(ListBox.ItemsSource);
					}
					ListBox.ItemsSource = value;
					if( ListBox.ItemsSource != null ) {
						//BindingOperations.EnableCollectionSynchronization(ListBox.ItemsSource, ListBox.ItemsSource);
					}
					ClearButton.IsEnabled = value != null;
					 SaveButton.IsEnabled = value != null;
				}
			}
		}
	}
}

//=============================================================================
