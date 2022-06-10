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
	public static partial class FrameworkElementFactory
	{
		public static System.Windows.FrameworkElementFactory CreateColumnAuto(
			string SHAREDSIZEGROUP = null
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(ColumnDefinition));
			factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(0, GridUnitType.Auto));
			if( !SHAREDSIZEGROUP.IsNullOrEmpty() ) {
				factory.SetValue(ColumnDefinition.SharedSizeGroupProperty, SHAREDSIZEGROUP);
			}
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateColumnStar(
			double VALUE           = 1.0,
			string SHAREDSIZEGROUP = null
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(ColumnDefinition));
			factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(VALUE, GridUnitType.Star));
			if( !SHAREDSIZEGROUP.IsNullOrEmpty() ) {
				factory.SetValue(ColumnDefinition.SharedSizeGroupProperty, SHAREDSIZEGROUP);
			}
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateColumnPixel(
			double VALUE,
			string SHAREDSIZEGROUP = null
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(ColumnDefinition));
			factory.SetValue(ColumnDefinition.WidthProperty, new GridLength(VALUE, GridUnitType.Pixel));
			if( !SHAREDSIZEGROUP.IsNullOrEmpty() ) {
				factory.SetValue(ColumnDefinition.SharedSizeGroupProperty, SHAREDSIZEGROUP);
			}
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateRowAuto()
		{
			var factory = new System.Windows.FrameworkElementFactory(typeof(RowDefinition));
			factory.SetValue(RowDefinition.HeightProperty, new GridLength(0, GridUnitType.Auto));
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateRowStar(
			double VALUE = 1.0
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(RowDefinition));
			factory.SetValue(RowDefinition.HeightProperty, new GridLength(VALUE, GridUnitType.Star));
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateRowPixel(
			double VALUE
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(RowDefinition));
			factory.SetValue(RowDefinition.HeightProperty, new GridLength(VALUE, GridUnitType.Pixel));
			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateGrid(
			int ROW, int COL
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(Grid));

			if( ROW >= 0 ) factory.SetValue(Grid.RowProperty,    ROW);
			if( COL >= 0 ) factory.SetValue(Grid.ColumnProperty, COL);

			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateCheckBox(
			int ROW, int COL,
			string BINDING = null, string TOOLTIP = null,
			BindingMode BINDING_MODE = BindingMode.TwoWay
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(CheckBox));

			if( ROW >= 0 ) factory.SetValue(Grid.RowProperty,    ROW);
			if( COL >= 0 ) factory.SetValue(Grid.ColumnProperty, COL);

			factory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			factory.SetValue(CheckBox.VerticalAlignmentProperty,   VerticalAlignment.Center);
			factory.SetValue(CheckBox.MarginProperty,              new Thickness(2));

			if( BINDING != null ) {
				factory.SetValue(CheckBox.IsCheckedProperty, new Binding(BINDING) { Mode = BINDING_MODE });
				if( BINDING_MODE == BindingMode.OneWay ) {
					factory.SetValue(CheckBox.IsEnabledProperty, false);
				}
			}
			if( TOOLTIP != null ) {
				factory.SetValue(CheckBox.ToolTipProperty, new Binding(TOOLTIP) { Mode = BindingMode.OneWay });
				factory.SetValue(ToolTipService.InitialShowDelayProperty, 0);
				factory.SetValue(ToolTipService.ShowDurationProperty, 60000);
			}

			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateImage(
			int ROW, int COL,
			string BINDING = null, string TOOLTIP = null
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(Image));

			if( ROW >= 0 ) factory.SetValue(Grid.RowProperty,    ROW);
			if( COL >= 0 ) factory.SetValue(Grid.ColumnProperty, COL);

			factory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			factory.SetValue(Image.VerticalAlignmentProperty,   VerticalAlignment.Center);
			factory.SetValue(Image.MarginProperty,              new Thickness(2));
			factory.SetValue(Image.StretchProperty,             Stretch.None);

			if( BINDING != null ) {
				factory.SetValue(Image.SourceProperty, new Binding(BINDING) { Mode = BindingMode.OneWay });
			}
			if( TOOLTIP != null ) {
				factory.SetValue(Image.ToolTipProperty, new Binding(TOOLTIP) { Mode = BindingMode.OneWay });
				factory.SetValue(ToolTipService.InitialShowDelayProperty, 0);
				factory.SetValue(ToolTipService.ShowDurationProperty, 60000);
			}

			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateTextBlock(
			int ROW, int COL,
			Brush FOREGROUND, FontWeight WEIGHT,
			string BINDING = null, string TOOLTIP = null
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(TextBlock));

			if( ROW >= 0 ) factory.SetValue(Grid.RowProperty,    ROW);
			if( COL >= 0 ) factory.SetValue(Grid.ColumnProperty, COL);

			factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
			factory.SetValue(TextBlock.VerticalAlignmentProperty,   VerticalAlignment.Center);
			factory.SetValue(TextBlock.BackgroundProperty,          Brushes.Transparent);
			factory.SetValue(TextBlock.MarginProperty,              new Thickness(2, 0, 2, 0));
			factory.SetValue(TextBlock.PaddingProperty,             new Thickness(2));

			if( FOREGROUND != null ) {
				factory.SetValue(TextBlock.ForegroundProperty, FOREGROUND);
			}
			if( WEIGHT != FontWeights.Normal ) {
				factory.SetValue(TextBlock.FontWeightProperty, WEIGHT);
			}

			if( BINDING != null ) {
				factory.SetValue(TextBlock.TextProperty, new Binding(BINDING) { Mode = BindingMode.OneWay });
			}
			if( TOOLTIP != null ) {
				factory.SetValue(TextBlock.ToolTipProperty, new Binding(TOOLTIP) { Mode = BindingMode.OneWay });
				factory.SetValue(ToolTipService.InitialShowDelayProperty, 0);
				factory.SetValue(ToolTipService.ShowDurationProperty, 60000);
			}

			return factory;
		}

		//...........................................................

		public static System.Windows.FrameworkElementFactory CreateTextBox(
			int ROW, int COL,
			Brush FOREGROUND, FontWeight WEIGHT,
			string BINDING = null, string TOOLTIP = null,
			BindingMode BINDING_MODE = BindingMode.OneWay
		){
			var factory = new System.Windows.FrameworkElementFactory(typeof(TextBox));

			if( ROW >= 0 ) factory.SetValue(Grid.RowProperty,    ROW);
			if( COL >= 0 ) factory.SetValue(Grid.ColumnProperty, COL);

			factory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Left);
			factory.SetValue(TextBox.VerticalAlignmentProperty,   VerticalAlignment.Center);
			factory.SetValue(TextBox.BackgroundProperty,          Brushes.Transparent);
			factory.SetValue(TextBox.BorderThicknessProperty,     new Thickness(0));
			factory.SetValue(TextBox.MarginProperty,              new Thickness(2, 0, 2, 0));
			factory.SetValue(TextBox.PaddingProperty,             new Thickness(2));
			factory.SetValue(TextBox.IsReadOnlyProperty,          true);

			if( FOREGROUND != null ) {
				factory.SetValue(TextBox.ForegroundProperty, FOREGROUND);
			}
			if( WEIGHT != FontWeights.Normal ) {
				factory.SetValue(TextBox.FontWeightProperty, WEIGHT);
			}

			if( BINDING != null ) {
				factory.SetValue(TextBox.TextProperty, new Binding(BINDING) { Mode = BINDING_MODE });
			}
			if( TOOLTIP != null ) {
				factory.SetValue(TextBox.ToolTipProperty, new Binding(TOOLTIP) { Mode = BindingMode.OneWay });
				factory.SetValue(ToolTipService.InitialShowDelayProperty, 0);
				factory.SetValue(ToolTipService.ShowDurationProperty, 60000);
			}

			return factory;
		}
	}
}

//=============================================================================
