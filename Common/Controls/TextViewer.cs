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
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk
{
	public class TextViewer
	: cmk.MainDockPanel
	{
		public static readonly avalon.TextViewPosition MousePositionInvalid = new() {
			Line          = -1,
			Column        = -1,
			VisualColumn  = -1,
			IsAtEndOfLine = true,
		};

		//...........................................................

		public TextViewer( string TEXT = "" ) : base()
		{
			ToolGrid.Background = Brushes.Silver;

			ToolWrapPanelLeft.Children.Add(SearchPanel);
			ClientGrid.Children.Add(Editor);

			Editor.Background      = Brushes.LightGray;
			Editor.FontFamily      = Resource.DefaultFont;
			Editor.FontSize        = Resource.DefaultFontSize;
			Editor.ShowLineNumbers = true;
			Editor.IsReadOnly      = true;

			Editor.Options.EnableHyperlinks           = true;
			Editor.Options.EnableEmailHyperlinks      = true;
			Editor.Options.AllowScrollBelowDocument   = true;
			Editor.Options.EnableRectangularSelection = true;

			SearchPanel.Editor = Editor;

			Editor.MouseMove += OnMouseMove;
			Loaded           += OnLoaded;

			EditorText = TEXT;
		}

		//...........................................................

		public readonly avalon.TextEditor Editor      = new();
		public readonly TextSearchPanel   SearchPanel = new();

		public avalon.TextViewPosition MousePosition { get; protected set; }
		public int                     MouseOffset   { get; protected set; }

		//...........................................................

		public avalon.Editing.TextArea EditorArea {
			get { return Editor.TextArea; }
		}

		//...........................................................

		public avalon.Rendering.TextView EditorView {
			get { return EditorArea.TextView; }
		}

		//...........................................................

		public avalon.Document.TextDocument EditorDocument {
			get { return Editor.Document; }
		}

		//...........................................................

		public virtual string EditorText {
			get { return EditorDocument.Text  ?? ""; }
			set { EditorDocument.Text = value ?? ""; }
		}

		//...........................................................

		public avalon.Editing.Caret EditorCaret {
			get { return EditorArea?.Caret; }
		}

		//...........................................................

		public IList<avalon.Rendering.IBackgroundRenderer> EditorBackgroundRenderers {
			get { return EditorView.BackgroundRenderers; }
		}

		//...........................................................

		protected virtual void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		{
			Loaded -= OnLoaded;
		}

		//...........................................................

		protected virtual void OnMouseMove( object SENDER, MouseEventArgs ARGS )
		{
			MousePosition = Editor.GetPositionFromPoint(ARGS.GetPosition(Editor)) ?? MousePositionInvalid;
			MouseOffset   = MousePosition == MousePositionInvalid ?
				-1 : EditorDocument.GetOffset(MousePosition.Line, MousePosition.Column)
			;
		}

		//...........................................................

		protected string StringAtMouse()
		{
			if( MouseOffset < 0 ) return null;

			var line  = EditorDocument.GetLineByNumber(MousePosition.Line);
			if( line == null || line.Length < 1 ) return null;

			var column = MousePosition.VisualColumn;
			if( column < 0 ) column = MousePosition.Column;

			var text = EditorText.Substring(line.Offset, line.Length);

			try {
				var matches = Resource.StringRegex.Matches(text);
				foreach( Match match in matches ) {
					if( column >= match.Index && column < (match.Index + match.Length) ) {
						return text.Substring(match.Index, match.Length);
					}
				}
			}
			catch {}

			return null;
		}

		//...........................................................

		public void LoadHighlighterExtension( string KNOWN_EXTENSION )
		{
			var highlighter  = HighlightingManager.Instance.GetDefinitionByExtension(KNOWN_EXTENSION);
			if( highlighter == null ) highlighter = LoadHighlighterResource(KNOWN_EXTENSION);
			if( highlighter != null ) Editor.SyntaxHighlighting = highlighter;
		}

		//...........................................................

		protected IHighlightingDefinition LoadHighlighterResource( string EXTENSION )
		{
			IHighlightingDefinition defn = null;

			string name = "";
			Stream xshd = null;
			switch( EXTENSION ) {
				case ".exml": name = "EXML"; xshd = new MemoryStream(cmk.Common.Properties.Resources.Avalon_exml); break;
				case ".ebin": name = "EBIN"; xshd = new MemoryStream(cmk.Common.Properties.Resources.Avalon_ebin); break;
				case ".csx":  name = "CSX";  xshd = new MemoryStream(cmk.Common.Properties.Resources.Avalon_csx);  break;
			}
			if( xshd == null ) return defn;

			var reader = new XmlTextReader(xshd);
			defn = HighlightingLoader.Load(reader, HighlightingManager.Instance);

			HighlightingManager.Instance.RegisterHighlighting(
				name, new[] { EXTENSION }, defn
			);

			return defn;
		}
	}
}

//=============================================================================
