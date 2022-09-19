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
		public delegate void InfoDoubleClickEventHandler( NMS.PAK.Item.Info INFO, TextSearchData SEARCH );
		public event         InfoDoubleClickEventHandler InfoDoubleClick;

		//...........................................................

		public GroupListBox( bool IS_VIRTUALIZING = true ) : base(IS_VIRTUALIZING)
		{
			Background = Brushes.DarkGray;

			var border_factory = new System.Windows.FrameworkElementFactory(typeof(Border));
			border_factory.SetValue(Border.BackgroundProperty,      Brushes.LightGray);
			border_factory.SetValue(Border.BorderBrushProperty,     Brushes.Black);
			border_factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
			border_factory.SetValue(Border.PaddingProperty,         new Thickness(10, 8, 12, 8));
			border_factory.SetValue(Border.MarginProperty,          new Thickness(1));
			border_factory.SetValue(Border.CornerRadiusProperty,    new CornerRadius(32));

			var grid_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());      // icon
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Id"));  // id
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnStar());      // name, desc, egg icon

			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // icon, id, name, egg
			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // desc
			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // requirements

			// this is for when the data is displayed grouped by category.
			// Name is from the category group object, not our data source object.
			var category_factory = FrameworkElementFactory.CreateTextBox(-1, -1, Brushes.White, FontWeights.Bold, "Name");
			category_factory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
			category_factory.SetValue(TextBox.PaddingProperty,             new Thickness(8, 2, 8, 4));
			category_factory.SetValue(TextBox.BackgroundProperty,          Brushes.DarkGray);
			category_factory.SetValue(TextBox.FontSizeProperty,            16.0);

			// these are for the per-item data to display.
			var icon_factory = FrameworkElementFactory.CreateImage  (0, 0, "Icon64");
			var id_factory   = FrameworkElementFactory.CreateTextBox(0, 1, null, FontWeights.Normal, "Id");
			var name_factory = FrameworkElementFactory.CreateTextBox(0, 2, null, FontWeights.Bold,   "Name");
			var desc_factory = FrameworkElementFactory.CreateTextBox(1, 2, null, FontWeights.Normal, "Description");

			icon_factory.SetValue(Grid.RowSpanProperty,            2);
			icon_factory.SetValue(Image.MarginProperty,            new Thickness(0, 2, 8, 0));
			icon_factory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Top);
			//icon_factory.AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(OnIconMouseDown));

			// can't add handler like for icon as text gets mouse events
			//id_factory.AddHandler(TextBox.MouseDownEvent, new MouseButtonEventHandler(OnIdMouseDown));

			desc_factory.SetValue(Grid.ColumnSpanProperty, 3);  // name and icon columns
			desc_factory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Top);
			desc_factory.SetValue(TextBox.MarginProperty,            new Thickness(0));

			grid_factory.AppendChild(icon_factory);
			grid_factory.AppendChild(id_factory);
			grid_factory.AppendChild(name_factory);
			grid_factory.AppendChild(desc_factory);
			grid_factory.AppendChild(CreateRequirementsListBox());

			border_factory.AppendChild(grid_factory);

			ItemTemplate  = new() { VisualTree = border_factory };
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

		protected System.Windows.FrameworkElementFactory CreateRequirementsListBox()
		{
			var list_box_factory = new System.Windows.FrameworkElementFactory(typeof(cmk.ListBox));
			list_box_factory.SetValue(Grid.RowProperty,        2);
			list_box_factory.SetValue(Grid.ColumnProperty,     2);
			list_box_factory.SetValue(ListBox.BorderThicknessProperty, new Thickness(0));
			list_box_factory.SetValue(ListBox.ItemsSourceProperty,     new Binding("Requirements"));

			var grid_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // requirement icon
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Amount"));  // requirement amount
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Name"));    // requirement name
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // requirement id

			var icon_factory   = FrameworkElementFactory.CreateImage  (-1, 0, "Data.Icon32");
			var amount_factory = FrameworkElementFactory.CreateTextBox(-1, 1, null,         FontWeights.Bold,   "Amount");
			var name_factory   = FrameworkElementFactory.CreateTextBox(-1, 2, null,         FontWeights.Bold,   "Data.Name", "Data.Description");
			var id_factory     = FrameworkElementFactory.CreateTextBox(-1, 3, Brushes.Gray, FontWeights.Normal, "Id");

			//icon_factory.AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(OnIngredientIconMouseDoubleClick));
			amount_factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
			amount_factory.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			name_factory  .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			id_factory    .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));

			grid_factory.AppendChild(icon_factory);
			grid_factory.AppendChild(amount_factory);
			grid_factory.AppendChild(name_factory);
			grid_factory.AppendChild(id_factory);

			var list_box_item_template = new DataTemplate {
				VisualTree = grid_factory,
			};
			list_box_factory.SetValue(ListBox.ItemTemplateProperty, list_box_item_template);

			return list_box_factory;
		}

		//...........................................................

		protected override void OnMouseDoubleClick( MouseButtonEventArgs ARGS )
		{
			base.OnMouseDoubleClick(ARGS);

			var source = ARGS.OriginalSource as FrameworkElement;
			var data   = source?.DataContext as NMS.Game.Items.Data;

			var id  = data?.Id;
			if( id == null ) return;

			NMS.PAK.Item.Info info   = null;
			TextSearchData    search = null;

			if( ARGS.OriginalSource is Image ) {
				info = data?.IconInfo;
			}
			else {
				info   = data?.Collection?.ItemInfo;
				search = new() {
					Pattern = id,
					Scroll  = TextSearchData.ScrollEnum.First,
				};
			}

			if( info != null ) InfoDoubleClick?.Invoke(info, search);
		}
	}
}

//=============================================================================
