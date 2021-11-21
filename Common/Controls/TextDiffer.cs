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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using avalon = ICSharpCode.AvalonEdit;
using diffplex = DiffPlex.DiffBuilder;

//=============================================================================

namespace cmk
{
	public class TextDiffer<VIEWER_T>
	: System.Windows.Controls.Grid
	where VIEWER_T : TextViewer, new()
	{
		public TextDiffer( string LHS = "", string RHS = "" ) : base()
		{
			ColumnDefinitions.AddStar();
			ColumnDefinitions.AddPixel(16);
			ColumnDefinitions.AddStar();

			Grid.SetColumn(ViewerLeft,  0);
			Grid.SetColumn(ScrollBar,   1);
			Grid.SetColumn(ViewerRight, 2);

			// disable folding, can't listen for folding events, can't sync foldings between viewers
			if( ViewerLeft  is TextViewerFolding folding_left  ) folding_left .IsFoldingEnabled = false;
			if( ViewerRight is TextViewerFolding folding_right ) folding_right.IsFoldingEnabled = false;

			EditorLeft.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			EditorLeft.VerticalScrollBarVisibility   = ScrollBarVisibility.Hidden;

			EditorRight.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			EditorRight.VerticalScrollBarVisibility   = ScrollBarVisibility.Hidden;

			ScrollBar.Orientation = Orientation.Vertical;

			BackgroundRendererLeft  = new(EditorLeft);
			BackgroundRendererRight = new(EditorRight);

			Children.Add(ViewerLeft);
			Children.Add(ScrollBar);
			Children.Add(ViewerRight);

			DiffModel = diffplex.SideBySideDiffBuilder.Diff(
				LHS, RHS, true, false
			);

			string text_left  = "",
				   text_right = "";

			Parallel.Invoke(
				() => {
					StringBuilder builder = new(DiffModel.OldText.Lines.Count * 80);
					foreach( var line in DiffModel.OldText.Lines ) {
						builder.Append(line?.Text);
						builder.Append(Environment.NewLine);
					}
					text_left = builder.ToString();
				},
				() => {
					StringBuilder builder = new(DiffModel.NewText.Lines.Count * 80);
					foreach( var line in DiffModel.NewText.Lines ) {
						builder.Append(line?.Text);
						builder.Append(Environment.NewLine);
					}
					text_right = builder.ToString();
				}
			);

			EditorLeft .Text = text_left;
			EditorRight.Text = text_right;

			BackgroundRendererLeft .DiffLines = DiffModel.OldText.Lines;
			BackgroundRendererRight.DiffLines = DiffModel.NewText.Lines;

			// Insert(0), always want it to be first one (bottom layer)
			EditorLeft .TextArea.TextView.BackgroundRenderers.Insert(0, BackgroundRendererLeft);
			EditorRight.TextArea.TextView.BackgroundRenderers.Insert(0, BackgroundRendererRight);

			ScrollBar.ValueChanged += OnScrollBarChanged;

			EditorLeft.TextArea.TextView.VisualLinesChanged  += OnEditorLeftScrollVChanged;
			EditorLeft.TextArea.TextView.ScrollOffsetChanged += OnEditorLeftScrollHChanged;

			EditorRight.TextArea.TextView.VisualLinesChanged  += OnEditorRightScrollVChanged;
			EditorRight.TextArea.TextView.ScrollOffsetChanged += OnEditorRightScrollHChanged;

			Loaded      += OnLoaded;
			SizeChanged += ( S, E ) => ScrollBarInit();
		}

		//...........................................................

		public readonly ScrollBar ScrollBar = new();

		public readonly VIEWER_T ViewerLeft  = new();
		public readonly VIEWER_T ViewerRight = new();

		public avalon.TextEditor EditorLeft  { get { return ViewerLeft .Editor; } }
		public avalon.TextEditor EditorRight { get { return ViewerRight.Editor; } }

		public TextDifferBackgroundRenderer BackgroundRendererLeft  { get; }
		public TextDifferBackgroundRenderer BackgroundRendererRight { get; }

		public diffplex.Model.SideBySideDiffModel DiffModel { get; protected set; }

		//...........................................................

		protected void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		{
			Loaded -= OnLoaded;
			EditorLeft .TextArea.TextView.EnsureVisualLines();
			EditorRight.TextArea.TextView.EnsureVisualLines();
			ScrollBarInit();
		}

		//...........................................................

		protected void ScrollBarInit()
		{
			ScrollBar.Minimum      = 0;
			ScrollBar.Maximum      = EditorLeft.ExtentHeight - EditorLeft.ViewportHeight;
			ScrollBar.ViewportSize = EditorLeft.ViewportHeight;
			ScrollBar.SmallChange  = EditorLeft.TextArea.TextView.DefaultLineHeight;
			ScrollBar.LargeChange  = EditorLeft.ViewportHeight;
			CreateDiffBitmap();
		}

		//...........................................................

		protected void CreateDiffBitmap()
		{
			if( ScrollBar.ActualHeight < 1 ) return;

			// normally the height of the thumb is scaled to represent the ViewportHeight.
			// for large files the height of the thumb may exceed the ViewportHeight,
			// that is the thumb height represent more than 1 page of text.
			// todo: adjust bitmap to handle case where scaled thumb height > ViewportHeight.

			var height = (int)Math.Round(ScrollBar.Track.ActualHeight);
			var width  = (int)Math.Round(ScrollBar.Track.ActualWidth);
			var half   = width / 2;

			var builder = new BitmapBuilder(width, height);
			builder.Clear(Colors.Black);

			Parallel.Invoke(
				() => MarkDiffBitmapMod(builder, DiffModel.OldText.Lines, 0,    half),
				() => MarkDiffBitmapMod(builder, DiffModel.NewText.Lines, half, width)
			);
			Parallel.Invoke(
				() => MarkDiffBitmapInsDel(builder, DiffModel.OldText.Lines, 0,    half),
				() => MarkDiffBitmapInsDel(builder, DiffModel.NewText.Lines, half, width)
			);

			var bitmap = builder.CreateBitmap();
			ScrollBar.Background = new ImageBrush(bitmap) {
				Stretch = Stretch.None
			};
		}

		//...........................................................

		protected void MarkDiffBitmapMod( BitmapBuilder BUILDER, List<diffplex.Model.DiffPiece> LINES, int LEFT, int RIGHT )
		{
			if( LINES.Count < 1 ) return;

			var scale = ((double)BUILDER.Height) / LINES.Count;

			for( var i = 0; i < LINES.Count; ++i ) {
				if( LINES[i].Type != diffplex.Model.ChangeType.Modified ) continue;
				var y0 = (int)Math.Round( i      * scale);
				var y1 = (int)Math.Round((i + 1) * scale);
				if( y0 >= BUILDER.Height ) y0 = BUILDER.Height - 1;
				if( y1 >  BUILDER.Height ) y1 = BUILDER.Height;
				lock( BUILDER ) do {
					BUILDER.HLine(y0, Colors.Cyan, LEFT, RIGHT);
				}	while( ++y0 < y1 );
			}
		}

		//...........................................................

		protected void MarkDiffBitmapInsDel( BitmapBuilder BUILDER, List<diffplex.Model.DiffPiece> LINES, int LEFT, int RIGHT )
		{
			if( LINES.Count < 1 ) return;

			var scale = ((double)BUILDER.Height) / LINES.Count;
			var color = Colors.Transparent;

			for( var i = 0; i < LINES.Count; ++i ) {
				switch( LINES[i].Type ) {
					case diffplex.Model.ChangeType.Deleted:  color = Colors.Red;  break;
					case diffplex.Model.ChangeType.Inserted: color = Colors.Lime; break;
					default: continue;
				}
				var y0 = (int)Math.Round( i      * scale);
				var y1 = (int)Math.Round((i + 1) * scale);
				if( y0 >= BUILDER.Height ) y0 = BUILDER.Height - 1;
				if( y1 >  BUILDER.Height ) y1 = BUILDER.Height;
				lock( BUILDER ) do {
					BUILDER.HLine(y0, color, LEFT, RIGHT);
				}	while( ++y0 < y1 );
			}
		}

		//...........................................................

		protected void OnScrollBarChanged( object SENDER, RoutedPropertyChangedEventArgs<double> ARGS )
		{
			var sender = SENDER as ScrollBar;
			var offset = sender.Value;
			EditorLeft .ScrollToVerticalOffset(offset);
			EditorRight.ScrollToVerticalOffset(offset);
		}

		//...........................................................

		protected void OnEditorLeftScrollVChanged( object SENDER, EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			ScrollBar.Value = sender.VerticalOffset;
		}

		//...........................................................

		protected void OnEditorLeftScrollHChanged( object SENDER, EventArgs ARGS )
		{
			var sender  = SENDER as avalon.Rendering.TextView;
			var offsetL = sender.HorizontalOffset  / (EditorLeft .ExtentWidth - EditorLeft .ViewportWidth);
			var offsetR =                  offsetL * (EditorRight.ExtentWidth - EditorRight.ViewportWidth);
			EditorRight.ScrollToHorizontalOffset(offsetR);
		}

		//...........................................................

		protected void OnEditorRightScrollVChanged( object SENDER, EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			ScrollBar.Value = sender.VerticalOffset;
		}

		//...........................................................

		protected void OnEditorRightScrollHChanged( object SENDER, EventArgs ARGS )
		{
			var sender  = SENDER as avalon.Rendering.TextView;
			var offsetR = sender.HorizontalOffset  / (EditorRight.ExtentWidth - EditorRight.ViewportWidth);
			var offsetL =                  offsetR * (EditorLeft .ExtentWidth - EditorLeft .ViewportWidth);
			EditorLeft.ScrollToHorizontalOffset(offsetL);
		}
	}
}

//=============================================================================
