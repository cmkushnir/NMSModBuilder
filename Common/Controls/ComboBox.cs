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

//=============================================================================

namespace cmk
{
	public class ComboBox
	: System.Windows.Controls.ComboBox
	{
		public delegate void VisualChildrenChangedEventHandler( DependencyObject ADDED, DependencyObject REMOVED );
		public event         VisualChildrenChangedEventHandler VisualChildrenChanged;

		//...........................................................

		public ComboBox() : base()
		{
			Construct(true);
		}

		//...........................................................

		public ComboBox( bool IS_VIRTUALIZING ) : base()
		{
			Construct(IS_VIRTUALIZING);
		}

		//...........................................................

		protected void Construct( bool IS_VIRTUALIZING )
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch;

			Grid.SetIsSharedSizeScope(this, true);

			if( IS_VIRTUALIZING ) ItemsPanel = new() {
				VisualTree = new System.Windows.FrameworkElementFactory(typeof(VirtualizingStackPanel))
			};
			SetValue(VirtualizingStackPanel.IsVirtualizingProperty,             IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.IsVirtualizingWhenGroupingProperty, IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.VirtualizationModeProperty,         VirtualizationMode.Recycling);
		}

		//...........................................................

		protected override void OnVisualChildrenChanged( DependencyObject ADDED, DependencyObject REMOVED )
		{
			VisualChildrenChanged?.Invoke(ADDED, REMOVED);
		}

		//...........................................................

		public ListCollectionView ListCollectionView {
			get {
				return CollectionViewSource.GetDefaultView(ItemsSource) as ListCollectionView;
			}
		}
	}
}

//=============================================================================
