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
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk
{
	public partial class TextSearchPanel
	: System.Windows.Controls.WrapPanel
	{
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
		public readonly ComboBox SearchScrollLineOffsetComboBox = new() {
			ToolTip = "Search Line Offset",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			IsEditable = false,
			Padding    = new(4, 2, 4, 2),
			Margin     = new(2),
			MinWidth   = 10,
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
					EditorBackgroundRenderers.Remove(SearchBackgroundRenderer);
				}
				m_editor = value;
				if( m_editor != null ) {
					// Add, always want it to be last one (draw last on top)
					EditorBackgroundRenderers.Add(SearchBackgroundRenderer);
				}
			}
		}

		//...........................................................

		public avalon.Document .TextDocument EditorDocument => Editor?.Document;
		public avalon.Editing  .TextArea     EditorArea     => Editor?.TextArea;
		public avalon.Editing  .Caret        EditorCaret    => EditorArea?.Caret;
		public avalon.Rendering.TextView     EditorView     => EditorArea?.TextView;

		public IList<avalon.Rendering.IBackgroundRenderer> EditorBackgroundRenderers =>
			EditorView?.BackgroundRenderers
		;

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
			SearchCountTextBlock.Text = "0";

			EditorArea.ClearSelection();
			EditorView.InvalidateLayer(avalon.Rendering.KnownLayer.Selection);

			Results = null;

			// limit the insanity
			if( SearchText.Length < 2 ) return;

			MatchCollection matches = null;
			try {
				var regex = SearchText.CreateRegex(
					SearchCaseSensitiveButton.IsChecked,
					SearchRegexButton.IsChecked
				);
				matches = regex.Matches(EditorText);
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return;
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

			SearchCountTextBlock.Text = results.Count.ToString();
			Results = results;
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

		protected void SelectResult( Match MATCH )
		{
			EditorArea.Selection = avalon.Editing.Selection.Create(
				EditorArea, MATCH.Index, MATCH.Index + MATCH.Length
			);

			EditorCaret.Offset = MATCH.Index;
			EditorCaret.BringCaretToView();
			EditorCaret.Show();

			if( SearchScrollLineOffset >= 0 ) {
				var lines      = EditorDocument.Lines.Count;
				var height     = EditorView.ActualHeight;
				var max_offset = (EditorView.DefaultLineHeight * (lines + 1)) - height;

				var offset = EditorView.GetVisualTopByDocumentLine(EditorCaret.Line);

				offset -= (EditorView.DefaultLineHeight * SearchScrollLineOffset);
				if( offset < 0 ) return;

				if( offset > max_offset ) offset = max_offset;
				Editor.ScrollToVerticalOffset(offset);
			}
		}

		//...........................................................

		protected void OnFirstClick( object SENDER, RoutedEventArgs ARGS )
		{
			var  results = Results;
			if( !results.IsNullOrEmpty() ) SelectResult(results[0]);
		}

		//...........................................................

		protected void OnPrevClick( object SENDER, RoutedEventArgs ARGS )
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

		protected void OnNextClick( object SENDER, RoutedEventArgs ARGS )
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

		protected void OnLastClick( object SENDER, RoutedEventArgs ARGS )
		{
			var  results = Results;
			if( !results.IsNullOrEmpty() ) SelectResult(results[results.Count - 1]);
		}
	}
}

//=============================================================================
