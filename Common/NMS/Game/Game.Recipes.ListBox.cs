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

namespace cmk.NMS.Game.Recipes
{
    public class ListBox
	: cmk.ListBox
	{
		public delegate void InfoDoubleClickEventHandler( NMS.PAK.Item.Info INFO, TextSearchData SEARCH );
		public event         InfoDoubleClickEventHandler InfoDoubleClick;

		//...........................................................

		public ListBox( bool IS_VIRTUALIZING = true ) : base(IS_VIRTUALIZING)
		{
			Background = Brushes.DarkGray;

			var border_factory = new System.Windows.FrameworkElementFactory(typeof(Border));
			border_factory.SetValue(Border.BackgroundProperty,      Brushes.LightGray);
			border_factory.SetValue(Border.BorderBrushProperty,     Brushes.Black);
			border_factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
			border_factory.SetValue(Border.PaddingProperty,         new Thickness(4));
			border_factory.SetValue(Border.MarginProperty,          new Thickness(1));
			border_factory.SetValue(Border.CornerRadiusProperty,    new CornerRadius(24));

			var grid_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // result icon
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Amount"));  // result amount
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Name"));    // result name, recipe name
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Id"));      // result nameid, recipe nameid
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // time, recipe id
			grid_factory.AppendChild(FrameworkElementFactory.CreateColumnStar());          //

			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // result
			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // recipe
			grid_factory.AppendChild(FrameworkElementFactory.CreateRowAuto());  // ingredients

			var result_icon_factory   = FrameworkElementFactory.CreateImage  (0, 0, "ResultData.Icon48");
			var result_amount_factory = FrameworkElementFactory.CreateTextBox(0, 1, null,         FontWeights.Bold,   "ResultAmount");
			var result_name_factory   = FrameworkElementFactory.CreateTextBox(0, 2, null,         FontWeights.Bold,   "ResultData.Name",  "ResultData.Description");
			var result_id_factory     = FrameworkElementFactory.CreateTextBox(0, 3, Brushes.Gray, FontWeights.Normal, "ResultId");
			var result_time_factory   = FrameworkElementFactory.CreateTextBox(0, 4, null,         FontWeights.Bold,   "TimeToMake");
			var recipe_type_factory   = FrameworkElementFactory.CreateTextBox(1, 2, Brushes.DarkBlue, FontWeights.Normal, "RecipeType");
			var recipe_typeid_factory = FrameworkElementFactory.CreateTextBox(1, 3, Brushes.Gray,     FontWeights.Normal, "RecipeTypeId");
			var recipe_id_factory     = FrameworkElementFactory.CreateTextBox(1, 4, Brushes.Gray,     FontWeights.Normal, "Id");
			var recipe_name_factory   = FrameworkElementFactory.CreateTextBox(1, 5, Brushes.Gray,     FontWeights.Normal, "RecipeName");

			//result_icon_factory.AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(OnResultIconMouseDoubleClick));
			result_amount_factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
			result_amount_factory.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			result_name_factory  .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			result_id_factory    .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));

			grid_factory.AppendChild(result_icon_factory);
			grid_factory.AppendChild(result_amount_factory);
			grid_factory.AppendChild(result_name_factory);
			grid_factory.AppendChild(result_id_factory);
			grid_factory.AppendChild(result_time_factory);
			grid_factory.AppendChild(recipe_type_factory);
			grid_factory.AppendChild(recipe_typeid_factory);
			grid_factory.AppendChild(recipe_id_factory);
			grid_factory.AppendChild(recipe_name_factory);
			grid_factory.AppendChild(CreateIngredientListBox());

			border_factory.AppendChild(grid_factory);

			ItemTemplate = new() { VisualTree = border_factory };
		}

		//...........................................................

		protected System.Windows.FrameworkElementFactory CreateIngredientListBox()
		{
			var list_box_factory = new System.Windows.FrameworkElementFactory(typeof(cmk.ListBox));
			list_box_factory.SetValue(Grid.RowProperty,        2);
			list_box_factory.SetValue(Grid.ColumnProperty,     2);
			list_box_factory.SetValue(Grid.ColumnSpanProperty, 3);
			list_box_factory.SetValue(ListBox.BorderThicknessProperty, new Thickness(0));
			list_box_factory.SetValue(ListBox.ItemsSourceProperty,     new Binding("Ingredients"));

			var ingredient_grid_factory = new System.Windows.FrameworkElementFactory(typeof(Grid));
			ingredient_grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // ingredient icon
			ingredient_grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Amount"));  // ingredient amount
			ingredient_grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto("Name"));    // ingredient name
			ingredient_grid_factory.AppendChild(FrameworkElementFactory.CreateColumnAuto());          // ingredient id

			var ingredient_icon_factory   = FrameworkElementFactory.CreateImage  (-1, 0, "Data.Icon32");
			var ingredient_amount_factory = FrameworkElementFactory.CreateTextBox(-1, 1, null,         FontWeights.Bold,   "Amount");
			var ingredient_name_factory   = FrameworkElementFactory.CreateTextBox(-1, 2, null,         FontWeights.Bold,   "Data.Name", "Data.Description");
			var ingredient_id_factory     = FrameworkElementFactory.CreateTextBox(-1, 3, Brushes.Gray, FontWeights.Normal, "Id");

			//ingredient_icon_factory.AddHandler(Image.MouseDownEvent, new MouseButtonEventHandler(OnIngredientIconMouseDoubleClick));
			ingredient_amount_factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
			ingredient_amount_factory.SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			ingredient_name_factory  .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));
			ingredient_id_factory    .SetValue(TextBlock.MarginProperty, new Thickness(8, 0, 0, 0));

			ingredient_grid_factory.AppendChild(ingredient_icon_factory);
			ingredient_grid_factory.AppendChild(ingredient_amount_factory);
			ingredient_grid_factory.AppendChild(ingredient_name_factory);
			ingredient_grid_factory.AppendChild(ingredient_id_factory);

			var list_box_item_template = new DataTemplate {
				VisualTree = ingredient_grid_factory,
			};
			list_box_factory.SetValue(ListBox.ItemTemplateProperty, list_box_item_template);

			return list_box_factory;
		}

		//...........................................................

		protected override void OnMouseDoubleClick( MouseButtonEventArgs ARGS )
		{
			base.OnMouseDoubleClick(ARGS);

			var source = ARGS.OriginalSource as FrameworkElement;
			var data   = source?.DataContext as NMS.Game.Recipes.Data;

			var id  = data?.Id;
			if( id == null ) return;

			var info   = data?.Collection?.RecipeInfo;
			var search = new TextSearchData(){
				Pattern = id,
				Scroll  = TextSearchData.ScrollEnum.First,
			};

			if( info != null ) InfoDoubleClick?.Invoke(info, search);
		}
	}
}

//=============================================================================
