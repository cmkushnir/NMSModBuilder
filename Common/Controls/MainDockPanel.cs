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

//=============================================================================

namespace cmk
{
	/// <summary>
	/// DockPanel with toolbar, client area, and statusbar.
	/// Toolbar   has left, center, and right sub-panels.
	/// Statusbar has left, center, and right sub-panels.
	/// Note: center panels are set in column 0 and span all 3 columns in order to be truly centered.
	///       this means that small horiz size grid can cause sub-panels to overlap.
	/// </summary>
	public class MainDockPanel
	: System.Windows.Controls.DockPanel
	{
		public MainDockPanel() : base()
		{
			Resources.Add(typeof(ComboBox), (Style)FindResource(ToolBar.ComboBoxStyleKey));
			Background = Brushes.LightGray;

			SetDock(ToolGrid, Dock.Top);
			SetDock(StatusGrid, Dock.Bottom);

			ToolGrid.ColumnDefinitions.AddAuto();
			ToolGrid.ColumnDefinitions.AddStar();
			ToolGrid.ColumnDefinitions.AddAuto();

			StatusGrid.ColumnDefinitions.AddAuto();
			StatusGrid.ColumnDefinitions.AddStar();
			StatusGrid.ColumnDefinitions.AddAuto();

			Grid.SetColumn(ToolWrapPanelLeft,   0);
			Grid.SetColumn(ToolWrapPanelCenter, 0); Grid.SetColumnSpan(ToolWrapPanelCenter, 3);
			Grid.SetColumn(ToolWrapPanelRight,  2);

			Grid.SetColumn(StatusWrapPanelLeft,   0);
			Grid.SetColumn(StatusWrapPanelCenter, 0); Grid.SetColumnSpan(StatusWrapPanelCenter, 3);
			Grid.SetColumn(StatusWrapPanelRight,  2);

			ToolGrid.Children.Add(ToolWrapPanelLeft);
			ToolGrid.Children.Add(ToolWrapPanelRight);
			ToolGrid.Children.Add(ToolWrapPanelCenter);

			StatusGrid.Children.Add(StatusWrapPanelLeft);
			StatusGrid.Children.Add(StatusWrapPanelCenter);
			StatusGrid.Children.Add(StatusWrapPanelRight);

			Children.Add(ToolGrid);
			Children.Add(StatusGrid);
			Children.Add(ClientGrid);
		}

		//...........................................................

		public readonly Grid ToolGrid = new() {
			VerticalAlignment = VerticalAlignment.Center,
			Background        = Brushes.White,
		};
		public readonly WrapPanel ToolWrapPanelLeft = new() {
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment   = VerticalAlignment.Center,
		};
		public readonly WrapPanel ToolWrapPanelCenter = new() {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
		};
		public readonly WrapPanel ToolWrapPanelRight = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Center,
		};

		public readonly Grid ClientGrid = new();

		public readonly Grid StatusGrid = new() {
			VerticalAlignment = VerticalAlignment.Center,
			Background        = Brushes.White,
		};
		public readonly WrapPanel StatusWrapPanelLeft = new() {
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment   = VerticalAlignment.Center,
		};
		public readonly WrapPanel StatusWrapPanelCenter = new() {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
		};
		public readonly WrapPanel StatusWrapPanelRight = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Center,
		};
	}
}

//=============================================================================
