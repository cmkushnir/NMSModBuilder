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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static cmk.TextSearchData;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk
{
    public class TextSearchData
	{
		public enum ScrollEnum
		{
			None  = 0,
			First = -1,
			Last  = int.MaxValue
		}
		public string     Pattern          = null;
		public string     Text             = null;
		public int        ScrollLineOffset = int.MaxValue;
		public ScrollEnum Scroll           = ScrollEnum.First;
		public bool       WholeWord        = false;
		public bool       CaseSensitive    = false;
		public bool       Regex            = false;
	}

	//=========================================================================

    public partial class TextSearchPanel
	: System.Windows.Controls.WrapPanel
	{
		public delegate void SearchChangedEventHandler( TextSearchPanel SENDER );
		public event         SearchChangedEventHandler SearchChanged;

		//...........................................................

		protected static string [] s_offsets = new string [] {
			"", "0",
			"1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
			"11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
		};
		protected static int s_selected_offset_index = 3;

		// # lines to show above a search match when moving through matches
		public int SearchScrollLineOffset { get; protected set; } = s_selected_offset_index - 1;

		//...........................................................

		public TextSearchPanel() : base()
		{
			Children.Add(SearchWholeWordButton);
			Children.Add(SearchCaseSensitiveButton);
			Children.Add(SearchRegexButton);

			Children.Add(SearchTextBox);
			Children.Add(SearchScrollLineOffsetComboBox);

			Children.Add(SearchFirstButton);
			Children.Add(SearchPrevButton);
			Children.Add(SearchCountTextBlock);
			Children.Add(SearchNextButton);
			Children.Add(SearchLastButton);

			SearchScrollLineOffsetComboBox.ItemsSource   = s_offsets;
			SearchScrollLineOffsetComboBox.SelectedIndex = s_selected_offset_index;

			SearchWholeWordButton    .CheckedChanged += ( S, E ) => DoSearch();
			SearchCaseSensitiveButton.CheckedChanged += ( S, E ) => DoSearch();
			SearchRegexButton        .CheckedChanged += ( S, E ) => DoSearch();

			SearchTextBox.TextChanged += ( S, E ) => DoSearch();
			SearchScrollLineOffsetComboBox.SelectionChanged += OnSearchScrollLineOffsetComboBoxSelectionChanged;

			SearchFirstButton.Click += OnFirstClick;
			SearchPrevButton .Click += OnPrevClick;
			SearchNextButton .Click += OnNextClick;
			SearchLastButton .Click += OnLastClick;
		}

		//...........................................................

		public readonly ImageCheckButton SearchWholeWordButton = new() {
			ToolTip = "Whole Word",
			Uri     = Resource.Uri("WholeWord.png"),
		};
		public readonly ImageCheckButton SearchCaseSensitiveButton = new() {
			ToolTip = "Case-Sensitive",
			Uri     = Resource.Uri("CaseSensitive.png"),
		};
		public readonly ImageCheckButton SearchRegexButton = new() {
			ToolTip = "Regex",
			Uri     = Resource.Uri("Regex.png"),
		};

		public readonly TextBox SearchTextBox = new() {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			Padding  = new(4, 2, 4, 2),
			Margin   = new(2),
			MinWidth = 60,
		};
		public readonly cmk.ComboBox SearchScrollLineOffsetComboBox = new() {
			ToolTip  = "Search Line Offset",
			MinWidth = 10,
		};

		public readonly ImageButton SearchFirstButton = new() {
			Uri = Resource.Uri("First.png"),
		};
		public readonly ImageButton SearchPrevButton = new() {
			Uri = Resource.Uri("Prev.png"),
		};
		public readonly TextBlock SearchCountTextBlock = new() {
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new(2),
			Text   = "0"
		};
		public readonly ImageButton SearchNextButton = new() {
			Uri = Resource.Uri("Next.png"),
		};
		public readonly ImageButton SearchLastButton = new() {
			Uri = Resource.Uri("Last.png"),
		};

		//...........................................................

		public readonly TextSearchBackgroundRenderer SearchBackgroundRenderer = new();

		public List<Match> Results {
			get { return SearchBackgroundRenderer.Results; }
			set { SearchBackgroundRenderer.Results = value; }
		}

		public Brush MarkerBrush {
			get { return SearchBackgroundRenderer.MarkerBrush; }
			set { SearchBackgroundRenderer.MarkerBrush = value; }
		}

		//...........................................................

		protected avalon.TextEditor m_editor;

		public avalon.TextEditor Editor {
			get { return m_editor; }
			set {
				if( m_editor == value ) return;
				if( m_editor != null ) {
					m_editor.TextChanged -= OnEditorTextChanged;
					EditorBackgroundRenderers.Remove(SearchBackgroundRenderer);
				}
				m_editor = value;
				if( m_editor != null ) {
					// Add, always want it to be last one (draw last on top)
					EditorBackgroundRenderers.Add(SearchBackgroundRenderer);
					m_editor.TextChanged += OnEditorTextChanged;
				}
			}
		}

		//...........................................................

		public avalon.Document .TextDocument EditorDocument => Editor?.Document;
		public avalon.Editing  .TextArea     EditorArea     => Editor?.TextArea;
		public avalon.Editing  .Caret        EditorCaret    => EditorArea?.Caret;
		public avalon.Rendering.TextView     EditorView     => EditorArea?.TextView;

		public IList<avalon.Rendering.IBackgroundRenderer> EditorBackgroundRenderers
		=> EditorView?.BackgroundRenderers;

		//...........................................................

		public string EditorText => EditorDocument?.Text ?? "";

		public string SearchText {
			get { return SearchTextBox.Text ?? ""; }
			set {
				SearchTextBox.Text = value;
				DoSearch();
			}
		}

		//...........................................................

		public TextSearchData Data {
			get => new TextSearchData() {
				Pattern          = SearchText,
				Text             = EditorText,
				ScrollLineOffset = SearchScrollLineOffset,
				Scroll           = ScrollEnum.None,
				WholeWord        = SearchWholeWordButton.IsChecked,
				CaseSensitive    = SearchCaseSensitiveButton.IsChecked,
				Regex            = SearchRegexButton.IsChecked,
			};
			set {
				if( value == null ) return;
				Dispatcher.Invoke(() => {
					SearchText = "";  // so don't re-search when updating following

					SearchWholeWordButton    .IsChecked = value.WholeWord;
					SearchCaseSensitiveButton.IsChecked = value.CaseSensitive;
					SearchRegexButton        .IsChecked = value.Regex;

					if( value.ScrollLineOffset >= 0 &&
						value.ScrollLineOffset <= s_offsets.Length
					)	SearchScrollLineOffsetComboBox.SelectedIndex = value.ScrollLineOffset + 1;

					var document  = EditorDocument;
					if( document != null && value.Text != null ) {
						EditorDocument.Text = value.Text;
					}

					SearchText = value.Pattern;

					switch( value.Scroll ) {
						case ScrollEnum.First: SearchFirstButton.PerformClick(); break;
						case ScrollEnum.Last:  SearchLastButton .PerformClick(); break;
					}
				});
			}
		}

		//...........................................................

		protected void OnEditorTextChanged( object SENDER, EventArgs ARGS )
		{
			// todo: start timer, DoSearch after timer exprires
			// to avoid researching after each keystroke.
			// low-priority as only applies to scripts at the moment,
			// and they are short enough that re-search is fast.
			DoSearch();
		}

		//...........................................................

		protected void OnSearchScrollLineOffsetComboBoxSelectionChanged( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var sender  = SENDER as ComboBox;
			if( sender != null ) {
				s_selected_offset_index = sender.SelectedIndex;
				SearchScrollLineOffset  = s_selected_offset_index - 1;
			}
		}

		//...........................................................

		protected void OnFirstClick( object SENDER )
		{
			var  results = Results;
			if( !results.IsNullOrEmpty() ) SelectResult(results[0]);
		}

		//...........................................................

		protected void OnPrevClick( object SENDER )
		{
			var results = Results;
			if( results.IsNullOrEmpty() ) return;

			var i = 0;
			for( var offset = EditorCaret.Offset; i < results.Count; ++i ) {
				if( results[i].Index >= offset ) break;
			}
			if( --i < 0 ) i = results.Count - 1;

			SelectResult(results[i]);
		}

		//...........................................................

		protected void OnNextClick( object SENDER )
		{
			var results = Results;
			if( results.IsNullOrEmpty() ) return;

			var i = 0;
			for( var offset = EditorCaret.Offset + 1; i < results.Count; ++i ) {
				if( results[i].Index >= offset ) break;
			}
			if( i >= results.Count ) i = 0;

			SelectResult(results[i]);
		}

		//...........................................................

		protected void OnLastClick( object SENDER )
		{
			var  results = Results;
			if( !results.IsNullOrEmpty() ) SelectResult(results[results.Count - 1]);
		}

		//...........................................................

		public bool IsWordBorder( int OFFSET )
		{
			return OFFSET == avalon.Document.TextUtilities.GetNextCaretPosition(
				EditorDocument, OFFSET - 1, LogicalDirection.Forward,
				avalon.Document.CaretPositioningMode.WordBorder
			);
		}

		//...........................................................

		public void DoSearch()
		{
			EditorArea.ClearSelection();
			EditorView.InvalidateLayer(avalon.Rendering.KnownLayer.Selection);

			SearchCountTextBlock.Text = "0";
			Results = null;

			Results = Search(EditorText);
			if( !Results.IsNullOrEmpty() ) {
				SearchCountTextBlock.Text = Results.Count.ToString();
			}

			SearchChanged?.Invoke(this);
		}

		//...........................................................

		public List<Match> Search( string TEXT )
		{
			if( TEXT.IsNullOrEmpty() ) return new();

			// limit the insanity
			var search_text = SearchText;
			if( search_text.Length < 2 || search_text.Length > TEXT.Length ) return new();

			MatchCollection matches = null;
			try {
				var regex = search_text.CreateRegex(
					SearchCaseSensitiveButton.IsChecked,
					SearchRegexButton.IsChecked
				);
				matches = regex.Matches(TEXT);
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return new();
			}

			var results     = new List<Match>(matches.Count);
			var whole_words = SearchWholeWordButton.IsChecked;

			foreach( Match match in matches ) {
				var match_end = match.Index + match.Length;
				if( whole_words ) {
					if( !IsWordBorder(match.Index) ||
						!IsWordBorder(match_end)
					) continue;
				}
				results.Add(match);
			}

			return results;
		}

		//...........................................................

		protected void SelectResult( Match MATCH )
		{
			EditorView.EnsureVisualLines();

			EditorArea.Selection = avalon.Editing.Selection.Create(
				EditorArea, MATCH.Index, MATCH.Index + MATCH.Length
			);

			EditorCaret.Offset = MATCH.Index;
			EditorCaret.BringCaretToView();
			EditorCaret.Show();

			var line   = EditorArea.Selection.StartPosition.Line;
			if( line   > SearchScrollLineOffset ) line -= SearchScrollLineOffset;
			var offset = EditorView.GetVisualTopByDocumentLine(line);

			Editor.ScrollToVerticalOffset(offset);
		}
	}
}

//=============================================================================
