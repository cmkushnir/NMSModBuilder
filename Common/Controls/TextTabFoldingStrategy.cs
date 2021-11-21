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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

//=============================================================================

namespace cmk
{
	public class TextTabFoldingStrategy
	: cmk.ITextViewerFoldingStrategy
	{
		public void UpdateFoldings( FoldingManager MANGER, TextDocument DOCUMENT )
		{
			int first_error_offset;
			var foldings = CreateNewFoldings(DOCUMENT, out first_error_offset);
			MANGER.UpdateFoldings(foldings, first_error_offset);
		}

		//...........................................................

		protected int LeadingTabs( TextDocument DOCUMENT, int OFFSET, int LENGTH )
		{
			var tabs = 0;
			for( int i = OFFSET, e = OFFSET + LENGTH;
				i < e && DOCUMENT.GetCharAt(i) == '\t';
				++i, ++tabs
			) ;
			return tabs;
		}

		//...........................................................

		public IEnumerable<NewFolding> CreateNewFoldings( TextDocument DOCUMENT, out int FIRST_ERROR_OFFSET )
		{
			FIRST_ERROR_OFFSET = -1;  // no error

			var foldings = new List<NewFolding>();
			if( DOCUMENT?.Lines == null ) return foldings;

			// getting offset from line expensive, getting length from line free
			int prev_offset = 0,                   // offset of start of previous line
			    line_offset = 0;                   // offset of start of current  line
			int prev_tabs   = 0;                   // track so we only have to calc once
			var started     = new Stack<int>(16);  // indexes of foldings that haven't beem closed

			foreach( var line in DOCUMENT.Lines ) {
				var line_tabs = LeadingTabs(DOCUMENT, line_offset, line.Length);
				if( line_tabs > started.Count ) {
					var name_offset = prev_offset              + prev_tabs;
					var name_length = line.PreviousLine.Length - prev_tabs;
					started.Push(foldings.Count);
					foldings.Add(new(name_offset, line_offset) {
						Name = DOCUMENT.GetText(name_offset, name_length)
					});
				}
				else while( line_tabs < started.Count ) {
					var folding = foldings[started.Pop()];
					folding.EndOffset = line_offset - 2;  // back-up to prev line end before '\n'
				}
				prev_offset  = line_offset;
				prev_tabs    = line_tabs;
				line_offset += line.TotalLength;
			}
			while( started.Count > 0 ) {
				var folding = foldings[started.Pop()];
				folding.EndOffset = line_offset;
			}

			return foldings;
		}
	}
}

//=============================================================================
