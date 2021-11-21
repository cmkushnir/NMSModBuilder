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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using avalon = ICSharpCode.AvalonEdit;
using diffplex = DiffPlex.DiffBuilder;

//=============================================================================

namespace cmk
{
	public class TextDifferBackgroundRenderer
	: avalon.Rendering.IBackgroundRenderer
	{
		public static readonly Color ModifiedColor = Color.FromRgb(0x88, 0xff, 0xff);
		public static readonly Color  DeletedColor = Color.FromRgb(0xff, 0xcc, 0xcc);
		public static readonly Color InsertedColor = Color.FromRgb(0xcc, 0xff, 0xcc);
		public static readonly Color  PaddingColor = Color.FromRgb(0xd4, 0xd4, 0xd4);

		//...........................................................

		public TextDifferBackgroundRenderer( avalon.TextEditor EDITOR ) : base()
		{
			Editor = EDITOR;

			var background = Editor?.Background as SolidColorBrush;
			var color      = background?.Color ?? SystemColors.WindowColor;

			ModifiedBrush = new LinearGradientBrush(
				ModifiedColor, color, 0.0
			);
			DeletedBrush = new LinearGradientBrush(
				DeletedColor, color, 0.0
			);
			InsertedBrush = new LinearGradientBrush(
				InsertedColor, color, 0.0
			);
			PaddingBrush = new LinearGradientBrush(
				PaddingColor, color, 0.0
			);

			ModifiedBrush.Freeze();
			 DeletedBrush.Freeze();
			InsertedBrush.Freeze();
			 PaddingBrush.Freeze();
		}

		//...........................................................

		public Brush ModifiedBrush { get; set; }
		public Brush  DeletedBrush { get; set; }
		public Brush InsertedBrush { get; set; }
		public Brush  PaddingBrush { get; set; }

		//...........................................................

		public avalon.TextEditor Editor { get; }

		public List<diffplex.Model.DiffPiece> DiffLines { get; set; }

		//...........................................................

		public avalon.Rendering.KnownLayer Layer {
			get { return avalon.Rendering.KnownLayer.Selection; }
		}

		//...........................................................

		public void Draw( avalon.Rendering.TextView VIEW, DrawingContext CONTEXT )
		{
			var diff_line = DiffLines;
			if( VIEW == null || CONTEXT == null || diff_line.IsNullOrEmpty() ) return;

			VIEW.EnsureVisualLines();

			// draw full-width, not just line width
			var width = System.Math.Max(Editor?.ExtentWidth ?? 0, VIEW.ActualWidth);
			var size  = new Size(width, VIEW.DefaultLineHeight);

			// Avalon Edit line numbers are 1-based, DiffLines are 0-based.
			var visible_lines = VIEW.VisualLines;
			var line          = visible_lines.First().FirstDocumentLine;
			var line_end      = visible_lines.Last() .LastDocumentLine.LineNumber;

			for( ; line != null && line.LineNumber <= line_end; line = line.NextLine ) {
				var diff_line_number  = line.LineNumber - 1;
				if( diff_line_number >= diff_line.Count ) break;

				var   diff  = diff_line[diff_line_number];
				Brush brush = null;

				if( diff?.Text == null ) brush = PaddingBrush;
				else switch( diff.Type ) {
						case diffplex.Model.ChangeType.Deleted:  brush =  DeletedBrush; break;
						case diffplex.Model.ChangeType.Inserted: brush = InsertedBrush; break;
						case diffplex.Model.ChangeType.Modified: brush = ModifiedBrush; break;
					}
				if( brush == null ) continue;

				foreach( var rect in avalon.Rendering.BackgroundGeometryBuilder.GetRectsForSegment(VIEW, line) ) {
					var extents = new Rect(rect.Location, size);
					CONTEXT.DrawRectangle(brush, null, extents);
				}
			}
		}
	}
}

//=============================================================================
