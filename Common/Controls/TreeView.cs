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
	public class TreeView
	: System.Windows.Controls.TreeView
	{
		public delegate void VisualChildrenChangedEventHandler( DependencyObject ADDED, DependencyObject REMOVED );
		public event         VisualChildrenChangedEventHandler VisualChildrenChanged;

		//...........................................................

		public TreeView() : base()
		{
			Construct(true);
		}

		//...........................................................

		public TreeView( bool IS_VIRTUALIZING ) : base()
		{
			Construct(IS_VIRTUALIZING);
		}

		//...........................................................

		protected void Construct( bool IS_VIRTUALIZING )
		{
			Resources[SystemColors.HighlightBrushKey]     = Brushes.Yellow;
			Resources[SystemColors.HighlightTextBrushKey] = Brushes.Black;
			Resources[SystemColors.InactiveSelectionHighlightBrushKey]     = Brushes.Yellow;
			Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = Brushes.Black;

			Background = Brushes.LightGray;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;

			Grid.SetIsSharedSizeScope(this, true);

			if( IS_VIRTUALIZING ) ItemsPanel = new() {
				VisualTree = new System.Windows.FrameworkElementFactory(typeof(VirtualizingStackPanel))
			};
			SetValue(VirtualizingStackPanel.IsVirtualizingProperty,             IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.IsVirtualizingWhenGroupingProperty, IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
		}

		//...........................................................

		protected override void OnVisualChildrenChanged( DependencyObject ADDED, DependencyObject REMOVED )
		{
			VisualChildrenChanged?.Invoke(ADDED, REMOVED);
		}
	}

	//=============================================================================

	public static partial class _x_
	{
		public static void Select( this System.Windows.Controls.TreeView TREEVIEW, int INDEX )
		{
			var item  = TREEVIEW?.ItemContainerGenerator.ContainerFromIndex(INDEX) as TreeViewItem;
			if( item != null ) item.IsSelected = true;
		}
	}
}

//=============================================================================
