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
using System.Text.RegularExpressions;
using System.Windows.Media;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk
{
	public class TextSearchBackgroundRenderer
	: avalon.Rendering.IBackgroundRenderer
	{
		public List<Match> Results     { get; set; }
		public Brush       MarkerBrush { get; set; }

		//...........................................................

		public avalon.Rendering.KnownLayer Layer {
			get { return avalon.Rendering.KnownLayer.Selection; }
		}

		//...........................................................

		public void Draw( avalon.Rendering.TextView VIEW, DrawingContext CONTEXT )
		{
			var results = Results;
			if( VIEW == null || CONTEXT == null || results.IsNullOrEmpty() ) return;

			VIEW.EnsureVisualLines();

			var visible_lines = VIEW.VisualLines;
			var offset_start  = visible_lines.First().FirstDocumentLine.Offset;
			var offset_end    = visible_lines.Last() .LastDocumentLine .EndOffset;

			var brush = MarkerBrush ?? Brushes.Yellow;

			foreach( var match in results ) {
				var match_end    = match.Index + match.Length;
				if( match_end   <= offset_start ) continue;
				if( match.Index >  offset_end   ) break;

				var segment = new avalon.Document.TextSegment{
					StartOffset = match.Index,
					Length      = match.Length,
				};

				foreach( var rect in avalon.Rendering.BackgroundGeometryBuilder.GetRectsForSegment(VIEW, segment) ) {
					CONTEXT.DrawRectangle(brush, null, rect);
				}
			}
		}
	}
}

//=============================================================================
