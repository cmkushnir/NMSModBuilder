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
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk
{
	public interface ITextViewerFoldingStrategy
	{
		void UpdateFoldings(
			avalon.Folding.FoldingManager MANAGER,
			avalon.Document.TextDocument DOCUMENT
		);
	}

	//=============================================================================

	public partial class TextViewerFolding
	: cmk.TextViewer
	{
		public TextViewerFolding(
			string TEXT = "",
			string PAKFILENAME = "",
			ITextViewerFoldingStrategy FOLDING_STRATEGY = null
		) : base(TEXT, PAKFILENAME)
		{
			FoldingStrategy  = FOLDING_STRATEGY;
			IsFoldingEnabled = FoldingStrategy != null;
		}

		//...........................................................

		protected readonly ImageButton FoldingsCollapseAllButton = new() {
			ToolTip = "Collapse All",
			Uri     = Resource.Uri("CollapseAll.png"),
		};
		protected readonly ImageButton FoldingsExpandAllButton = new() {
			ToolTip = "Expand All",
			Uri     = Resource.Uri("ExpandAll.png"),
			Margin  = new Thickness(0, 0, 8, 0),
		};

		//...........................................................

		public override string EditorText {
			get { return base.EditorText; }
			set {
				base.EditorText = value;
				FoldingStrategy?.UpdateFoldings(FoldingManager, EditorDocument);
			}
		}

		//...........................................................

		public ITextViewerFoldingStrategy    FoldingStrategy { get; protected set; }
		public avalon.Folding.FoldingManager FoldingManager  { get; protected set; }

		//...........................................................

		// derived, optional
		//protected override void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		//{
		//	base.OnLoaded(SENDER, ARGS);
		//	CollapseAll();  // optional
		//}

		//...........................................................

		public bool IsFoldingEnabled {
			get { return FoldingManager != null; }
			set {
				if( IsFoldingEnabled == value ) return;
				if( value ) {
					FoldingManager = avalon.Folding.FoldingManager.Install(EditorArea);
					FoldingStrategy?.UpdateFoldings(FoldingManager, EditorDocument);
					ToolWrapPanelLeft.Children.Insert(0, FoldingsCollapseAllButton);
					ToolWrapPanelLeft.Children.Insert(1, FoldingsExpandAllButton);
					FoldingsCollapseAllButton.Click += OnFoldingsCollapseAllClick;
					FoldingsExpandAllButton  .Click += OnFoldingsExpandAllClick;
				}
				else {
					FoldingsExpandAllButton  .Click -= OnFoldingsExpandAllClick;
					FoldingsCollapseAllButton.Click -= OnFoldingsCollapseAllClick;
					ToolWrapPanelLeft.Children.Remove(FoldingsExpandAllButton);
					ToolWrapPanelLeft.Children.Remove(FoldingsCollapseAllButton);
					avalon.Folding.FoldingManager.Uninstall(FoldingManager);
					FoldingManager = null;
				}
			}
		}

		//...........................................................

		public void FoldingsCollapseAll()
		{
			var foldings  = FoldingManager?.AllFoldings;
			if( foldings == null ) return;

			foreach( var folding in foldings ) {
				folding.IsFolded = true;
			}

			var first  = FoldingManager.GetNextFolding(0);
			if( first != null ) first.IsFolded = false;
		}

		//...........................................................

		public void FoldingsExpandAll()
		{
			var foldings  = FoldingManager?.AllFoldings;
			if( foldings == null ) return;

			foreach( var folding in foldings ) {
				folding.IsFolded = false;
			}
		}

		//...........................................................

		protected void OnFoldingsCollapseAllClick( object SENDER, RoutedEventArgs ARGS )
		{
			FoldingsCollapseAll();
		}

		//...........................................................

		protected void OnFoldingsExpandAllClick( object SENDER, RoutedEventArgs ARGS )
		{
			FoldingsExpandAll();
		}
	}
}

//=============================================================================
