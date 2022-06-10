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
	public class XmlFoldingStrategyAdapter
	: cmk.ITextViewerFoldingStrategy
	{
		public static avalon.Folding.XmlFoldingStrategy XmlFoldingStrategy = new() {
			ShowAttributesWhenFolded = true
		};

		//...........................................................

		public void UpdateFoldings(
			avalon.Folding.FoldingManager MANAGER,
			avalon.Document.TextDocument DOCUMENT
		){
			if( MANAGER != null && DOCUMENT != null ) {
				XmlFoldingStrategy.UpdateFoldings(MANAGER, DOCUMENT);
			}
		}
	}

	//=========================================================================

	public class XmlViewer
	: cmk.TextViewerFolding
	{
		public XmlViewer( string XML = "", string PAKFILENAME = "" )
		: base(XML, PAKFILENAME, new XmlFoldingStrategyAdapter())
		{
			LoadHighlighterExtension(".xml");
		}

		//...........................................................

		protected override void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		{
			base.OnLoaded(SENDER, ARGS);
			FoldingsCollapseAll();
		}
	}
}

//=============================================================================
