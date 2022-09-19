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
using System.Windows.Media;

//=============================================================================

namespace cmk
{
    public enum LogItemType
	{
		Heading,
		Failure,
		Warning,
		Information,
		Success,
	}

	//=========================================================================

	public class LogItem
	{
		public LogItemType Type { get; set; } = LogItemType.Information;
		public string      Text { get; set; }
		public object      Tag0 { get; set; }
		public object      Tag1 { get; set; }

		//...........................................................
		// following controls how LogItem displayed in LogViewer
		//...........................................................

		public static readonly ImageSource HeadingIcon;
		public static readonly ImageSource FailureIcon = Resource.BitmapImage("Error.png");
		public static readonly ImageSource WarningIcon = Resource.BitmapImage("Warning.png");
		public static readonly ImageSource InformationIcon;
		public static readonly ImageSource SuccessIcon;

		public static readonly Brush HeadingBackground     = Brushes.DarkGray;
		public static readonly Brush FailureBackground     = Brushes.Transparent;
		public static readonly Brush WarningBackground     = Brushes.Transparent;
		public static readonly Brush InformationBackground = Brushes.Transparent;
		public static readonly Brush SuccessBackground     = Brushes.Transparent;

		public static readonly Brush HeadingForeground     = Brushes.White;
		public static readonly Brush FailureForeground     = Brushes.DarkRed;
		public static readonly Brush WarningForeground     = new SolidColorBrush(Color.FromRgb(150, 70, 0));
		public static readonly Brush InformationForeground = Brushes.DarkBlue;
		public static readonly Brush SuccessForeground     = Brushes.DarkGreen;

		//...........................................................

		static LogItem()
		{
			HeadingIcon?.Freeze();
			FailureIcon?.Freeze();
			WarningIcon?.Freeze();
			InformationIcon?.Freeze();
			SuccessIcon?.Freeze();

			WarningForeground?.Freeze();
		}

		//...........................................................

		public ImageSource Icon {
			get {
				switch( Type ) {
					case LogItemType.Heading: return HeadingIcon;
					case LogItemType.Failure: return FailureIcon;
					case LogItemType.Warning: return WarningIcon;
					case LogItemType.Success: return SuccessIcon;
				}
				return InformationIcon;
			}
		}

		//...........................................................

		public Brush Background {
			get {
				switch( Type ) {
					case LogItemType.Heading: return HeadingBackground;
					case LogItemType.Failure: return FailureBackground;
					case LogItemType.Warning: return WarningBackground;
					case LogItemType.Success: return SuccessBackground;
				}
				return InformationBackground;
			}
		}

		//...........................................................

		public Brush Foreground {
			get {
				switch( Type ) {
					case LogItemType.Heading: return HeadingForeground;
					case LogItemType.Failure: return FailureForeground;
					case LogItemType.Warning: return WarningForeground;
					case LogItemType.Success: return SuccessForeground;
				}
				return InformationForeground;
			}
		}

		//...........................................................

		public FontWeight Weight {
			get {
				switch( Type ) {
					case LogItemType.Heading: return FontWeights.Bold;
				}
				return FontWeights.Normal;
			}
		}
	}
}

//=============================================================================
