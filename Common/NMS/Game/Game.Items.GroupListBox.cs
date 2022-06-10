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

namespace cmk.NMS.Game.Items
{
	/// <summary>
	/// Used to display substance, product, or technology lists.
	/// </summary>
	public class GroupListBox
	: cmk.ListBox
	{
		public event MouseButtonEventHandler IconMouseDoubleClick;

		//...........................................................

		public GroupListBox( bool IS_VIRTUALIZING = true ) : base(IS_VIRTUALIZING)
		{
			Padding = new(0);

			var item_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			item_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());      // icon
			item_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Id"));  // id
			item_factory.AppendChild(FrameworkElementFactory.CreateColumnStar());      // name, desc, egg icon

			item_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // icon, id, name, egg
			item_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // desc

			// this is for when the data is displayed grouped by category.
			// Name is from the category group object, not our data source object.
			var category_factory = FrameworkElementFactory.CreateTextBox(-1, -1, Brushes.White, FontWeights.Bold, "Name");

			// these are for the per-item data to display.
			var icon_factory = FrameworkElementFactory.CreateImage  (0, 0, "Icon64");
			var id_factory   = FrameworkElementFactory.CreateTextBox(0, 1, null, FontWeights.Normal, "Id");
			var name_factory = FrameworkElementFactory.CreateTextBox(0, 2, null, FontWeights.Bold,   "Name");
			var desc_factory = FrameworkElementFactory.CreateTextBox(1, 2, null, FontWeights.Normal, "Description");

			category_factory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
			category_factory.SetValue(TextBox.PaddingProperty,             new Thickness(8, 2, 8, 4));
			category_factory.SetValue(TextBox.BackgroundProperty,          Brushes.DarkGray);
			category_factory.SetValue(TextBox.FontSizeProperty,            16.0);

			icon_factory.SetValue(Grid.RowSpanProperty,            2);
			icon_factory.SetValue(Image.MarginProperty,            new Thickness(0, 2, 8, 2));
			icon_factory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Top);
			icon_factory.AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(OnIconMouseDown));

			desc_factory.SetValue(Grid.ColumnSpanProperty, 3);  // name and icon columns
			desc_factory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Top);
			desc_factory.SetValue(TextBox.MarginProperty,            new Thickness(0, 0, 0, 4));

			item_factory.AppendChild(icon_factory);
			item_factory.AppendChild(id_factory);
			item_factory.AppendChild(name_factory);
			item_factory.AppendChild(desc_factory);

			ItemTemplate  = new() { VisualTree = item_factory };
			m_group_style = new() { HeaderTemplate = new() { VisualTree = category_factory } };
		}

		//...........................................................

		protected GroupStyle m_group_style;

		/// <summary>
		/// Add/remove grouping style.  Grouping makes it slow to load.
		/// </summary>
		public bool IsGrouped {
			get { return GroupStyle.Count > 0; }
			set {
				if( IsGrouped == value ) return;
				var view = ListCollectionView;
				if( !value ) {
					if( view != null ) {
						view.CustomSort = null;
						view.GroupDescriptions.Clear();
					}
					GroupStyle.Clear();
				}
				else {
					GroupStyle.Add(m_group_style);
					if( view != null ) {
						view.GroupDescriptions.Add(new PropertyGroupDescription("CategoryName"));
						view.CustomSort = new Game.Items.Data.CategoryComparer();
					}
				}
			}
		}

		//...........................................................

		/// <summary>
		/// On double-click open icon dds in PAK Item viewer.
		/// </summary>
		protected void OnIconMouseDown( object SENDER, MouseButtonEventArgs ARGS )
		{
			if( ARGS.ClickCount == 2 ) IconMouseDoubleClick?.Invoke(SENDER, ARGS);
		}
	}
}

//=============================================================================
