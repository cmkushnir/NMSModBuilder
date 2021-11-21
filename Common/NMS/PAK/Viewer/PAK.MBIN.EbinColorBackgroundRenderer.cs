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

using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk.NMS.PAK.MBIN
{
	public class EbinColorBackgroundRenderer
	: avalon.Rendering.IBackgroundRenderer
	{
		// search for RGBA color strings "(#, #, #, #)"
		public static readonly Regex ColorRegex = new(
			@"(?<=\x28)+[0-9., ]+(?=\x29)",
			RegexOptions.Singleline | RegexOptions.Compiled,
			System.TimeSpan.FromSeconds(1)
		);

		public static readonly Pen ColorSwatchPen = new(Brushes.Black, 0.7);

		//...........................................................

		static EbinColorBackgroundRenderer()
		{
			ColorSwatchPen.Freeze();
		}

		//...........................................................

		public avalon.Rendering.KnownLayer Layer {
			get { return avalon.Rendering.KnownLayer.Selection; }
		}

		//...........................................................

		public void Draw( avalon.Rendering.TextView VIEW, DrawingContext CONTEXT )
		{
			if( VIEW == null || CONTEXT == null ) return;

			VIEW.EnsureVisualLines();

			// Draw is called on each scroll message, make this as fast as possible
			var visible_lines = VIEW.VisualLines;
			var offset_start  = visible_lines.First().FirstDocumentLine.Offset;
			var offset_end    = visible_lines.Last() .LastDocumentLine .EndOffset;
			var offset_horiz  = VIEW.HorizontalOffset;
			var visible_text  = VIEW.Document.Text.Substring(offset_start, offset_end - offset_start);

			MatchCollection matches;
			try   { matches = ColorRegex.Matches(visible_text); }
			catch { return; }
			if( matches == null || matches.Count < 1 ) return;

			RenderOptions.SetEdgeMode(VIEW, EdgeMode.Unspecified);
			VIEW.SnapsToDevicePixels = false;

			foreach( Match match in matches ) {
				var parts = visible_text.Substring(match.Index, match.Length).SplitEx(',', ' ');
				if( parts.Length != 4 ) continue;  // require (R, G, B, A)

				var r = double.Parse(parts[0]);
				var g = double.Parse(parts[1]);
				var b = double.Parse(parts[2]);
				//	var a = double.Parse(parts[3]);

				var brush = new SolidColorBrush(Color.FromRgb(
					(byte)(r * byte.MaxValue),
					(byte)(g * byte.MaxValue),
					(byte)(b * byte.MaxValue)
				));

				// match is relative to visible text
				var segment = new avalon.Document.TextSegment{
					StartOffset = offset_start + match.Index,
					Length      = 1,
				};

				foreach( var rect in avalon.Rendering.BackgroundGeometryBuilder.GetRectsForSegment(VIEW, segment) ) {
					var rad_y  = (int)(rect.Height / 2) - 1;
					var center = new Point(rad_y + 2 - offset_horiz, rad_y + 1 + rect.Top);
					CONTEXT.DrawEllipse(brush, ColorSwatchPen, center, rad_y, rad_y);
				}
			}
		}
	}
}

//=============================================================================
