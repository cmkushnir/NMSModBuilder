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

using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
    public class Resource
	{
		public static string AppDirectory { get; } =
			System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + '\\'
		;

		public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(16);

		protected static string s_save_directory = System.IO.Path.GetTempPath();

		/// <summary>
		/// Get returns path ending with a backslash.
		/// Set will extract directory portion if includes file.
		/// If set doesn't include file then must end in backslash
		/// or will remove last part (treats as file name w/o extension).
		/// Setting null or empty will reset to temp path.
		/// </summary>
		public static string SaveDirectory {
			get { return s_save_directory; }
			set {
				if( s_save_directory == value ) return;
				s_save_directory = System.IO.Path.GetDirectoryName(value);
				if( s_save_directory.IsNullOrEmpty() ) s_save_directory  = System.IO.Path.GetTempPath();
				if( !s_save_directory.EndsWith('\\') ) s_save_directory += '\\';
			}
		}

		// font used for text viewer|editor and other ui elements

		public static readonly FontFamily DefaultFont     = new("Consolas");
		public static readonly double     DefaultFontSize = 14;

		// general rgb colors

		public static readonly Color LightRedColor = Color.FromRgb(255, 160, 160);
		public static readonly Brush LightRedBrush = new SolidColorBrush(LightRedColor);

		public static readonly Color LightYellowColor = Color.FromRgb(255, 255, 65);
		public static readonly Brush LightYellowBrush = new SolidColorBrush(LightYellowColor);

		public static readonly Color LightGreenColor = Color.FromRgb(160, 255, 160);
		public static readonly Brush LightGreenBrush = new SolidColorBrush(LightGreenColor);

		public static readonly Color LightBlueColor = Color.FromRgb(160, 160, 255);
		public static readonly Brush LightBlueBrush = new SolidColorBrush(LightBlueColor);

		// background for ImageButton's

		public static readonly Color     OverBackgroundColor = Color.FromArgb(128, 0, 255, 255);
		public static readonly Color SelectedBackgroundColor = Color.FromArgb(255, 0, 255, 255);

		public static readonly Brush  NormalBackgroundBrush = Brushes.Transparent;
		public static readonly Brush    OverBackgroundBrush;
		public static readonly Brush PressedBackgroundBrush;

		public static readonly Brush EnabledBackgroundBrush;
		public static readonly Brush DisabledBackgroundBrush;

		// fade ImageButton's if they are disabled
		public static readonly float DisabledOpacity = 0.3f;

		public static readonly Brush SelectedBackgroundBrush = Brushes.Yellow;

		// search for strings: "..."
		public static readonly Regex StringRegex = new(
			@"(?<=\x22)[^\x22]*(?=\x22)",
			RegexOptions.Singleline | RegexOptions.Compiled,
			System.TimeSpan.FromSeconds(1)
		);

		public static readonly Regex FilePathRegex = new(
			@"\b[a-zA-Z]*:?[\-\\_0-9a-zA-Z]*\\[.\\_0-9a-zA-Z]*\b",
			RegexOptions.Singleline | RegexOptions.Compiled,
			System.TimeSpan.FromSeconds(1)
		);

		public static readonly Regex ItemPathRegex = new(
			@"\b[0-9a-zA-Z][/\-\\_0-9a-zA-Z]*\.[0-9a-zA-Z][.0-9a-zA-Z]*\b",
			RegexOptions.Singleline | RegexOptions.Compiled,
			System.TimeSpan.FromSeconds(1)
		);

		//...........................................................

		static Resource()
		{
			LightRedBrush  .Freeze();
			LightGreenBrush.Freeze();
			LightBlueBrush .Freeze();

			OverBackgroundBrush = new RadialGradientBrush(new(3) {
				new(OverBackgroundColor, 0.0),
				new(OverBackgroundColor, 0.99),
				new(Colors.Transparent,  1.0),
			});
			PressedBackgroundBrush = new RadialGradientBrush(new(3) {
				new(SelectedBackgroundColor, 0.0),
				new(SelectedBackgroundColor, 0.99),
				new(Colors.Transparent,      1.0),
			});

			EnabledBackgroundBrush = new RadialGradientBrush(new(3) {
				new(LightGreenColor,    0.0),
				new(LightGreenColor,    0.99),
				new(Colors.Transparent, 1.0),
			});
			DisabledBackgroundBrush = new RadialGradientBrush(new(3) {
				new(LightRedColor,      0.0),
				new(LightRedColor,      0.99),
				new(Colors.Transparent, 1.0),
			});

			OverBackgroundBrush   .Freeze();
			PressedBackgroundBrush.Freeze();
		}

		//...........................................................

		/// <summary>
		/// Load a Uri from a resource PATH and optional ASSEMBLY.
		/// Use: e.g. Icon = new BitmapImage(Resource.Get("NMS.ico"));
		/// </summary>
		/// <param name="PATH">e.g. "Logo.ico"</param>
		/// <param name="ASSEMBLY">Name of assembly e.g. "Common", or null|empty for application.</param>
		public static Uri Uri( string PATH, string ASSEMBLY = null )
		{
			if( PATH.IsNullOrEmpty() ) return null;
			if( ASSEMBLY.IsNullOrEmpty() ) {
				ASSEMBLY = Assembly.GetExecutingAssembly().GetName().Name;
			}
			return new(
				"pack://application:,,,/" + ASSEMBLY + ";component/Resources/" + PATH
			);
		}

		//...........................................................

		/// <summary>
		/// Return a new BitmapImage from the resource PATH and optional ASSEMBLY.
		/// </summary>
		/// <param name="PATH">e.g. "Logo.ico"</param>
		/// <param name="ASSEMBLY">Name of assembly e.g. "Common", or null|empty for application.</param>
		public static BitmapImage BitmapImage( string PATH, string ASSEMBLY = null )
		{
			return new(Uri(PATH, ASSEMBLY));
		}

		//...........................................................

		/// <summary>
		/// Return a new Image control from the resource PATH and optional ASSEMBLY.
		/// </summary>
		/// <param name="PATH">e.g. "Logo.ico"</param>
		/// <param name="ASSEMBLY">Name of assembly e.g. "Common", or null|empty for application.</param>
		public static Image Image( string PATH, string TOOLTIP = null, string ASSEMBLY = null )
		{
			var image = new Image() {
				Source  = new BitmapImage(Uri(PATH, ASSEMBLY)),
				ToolTip = TOOLTIP,
				Stretch = Stretch.None,
			};
			ToolTipService.SetInitialShowDelay(image, 0);
			ToolTipService.SetShowDuration(image, 60000);
			return image;
		}

		//...........................................................

		public static Brush NewCheckerBrush(
			double SIZE  = 32,    // square size in ui units (pixels, unless display scaled)
			Brush  LIGHT = null,  // 0xb0, 0xb0, 0xb0
			Brush  DARK  = null   // 0xa0, 0xa0, 0xa0
		){
			var brush  = new DrawingBrush();

			if( SIZE  <  1 ) SIZE  = 1;
			if( LIGHT == null ) LIGHT = new SolidColorBrush(Color.FromRgb(0xb0, 0xb0, 0xb0));
			if( DARK  == null ) DARK  = new SolidColorBrush(Color.FromRgb(0xa0, 0xa0, 0xa0));

			var group_dark = new GeometryGroup();
			group_dark.Children.Add(new RectangleGeometry(new(0, 0, 50, 50)));
			group_dark.Children.Add(new RectangleGeometry(new(50, 50, 50, 50)));

			var draw_back = new GeometryDrawing(LIGHT, null, new RectangleGeometry(new(0, 0, 100, 100)));
			var draw_dark = new GeometryDrawing(DARK,  null, group_dark);

			var checkers = new DrawingGroup();
			checkers.Children.Add(draw_back);
			checkers.Children.Add(draw_dark);

			brush.Drawing       = checkers;
			brush.TileMode      = TileMode.Tile;
			brush.ViewportUnits = BrushMappingMode.Absolute;
			brush.Viewport      = new(0, 0, SIZE, SIZE);

			brush.Freeze();
			return brush;
		}
	}
}

//=============================================================================
