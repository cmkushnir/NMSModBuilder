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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

//=============================================================================

namespace cmk.NMS.PAK.MBIN
{
	public class ExmlViewer
	: cmk.XmlViewer
	, Item.IViewer
	{
		public ExmlViewer() : this(null) {}

		public ExmlViewer( Data DATA, Log LOG = null ) : base()
		{
			LoadHighlighterExtension(".exml");
			Data = DATA;
		}

		//...........................................................

		public ImageButton ViewerButton { get; } = new() {
			ToolTip   = "Default",
			Uri       = Resource.Uri("PakItemExml.png")
		};

		//...........................................................

		protected NMS.PAK.MBIN.Data m_data;

		public NMS.PAK.MBIN.Data Data {
			get { return m_data; }
			set {
				if( m_data == value ) return;
				m_data          = value;
				EditorText      = m_data?.CreateEXML();
				SourceLabel.Text = m_data?.FilePath?.NameExt;
			}
		}
	}

	//=========================================================================

	public class EbinViewer
	: cmk.TextViewerFolding
	, Item.IViewer
	{
		// has no state, can use singleton
		public static readonly EbinColorBackgroundRenderer ColorBackgroundRenderer = new();
		public static readonly cmk.TextTabFoldingStrategy  TabFoldingStrategy      = new();

		//...........................................................

		public EbinViewer() : this(null) {}

		public EbinViewer( Data DATA, Log LOG = null ) : base("", "", TabFoldingStrategy)
		{
			LoadHighlighterExtension(".ebin");
			EditorBackgroundRenderers.Add(ColorBackgroundRenderer);

			ToolWrapPanelLeft.Children.Add(LanguageIdTextBox);
			ToolWrapPanelLeft.Children.Add(LanguageValueTextBox);

			ToolTipService.SetInitialShowDelay(LanguageIdTextBox, 0);
			ToolTipService.SetShowDuration(LanguageIdTextBox, 60000);

			LanguageIdTextBox.TextChanged += ( S, E ) => DoSearchLanguageId(SearchLanguageId);

			Data = DATA;
		}

		//...........................................................

		public readonly TextBox LanguageIdTextBox = new() {
			ToolTip             = "Language ID",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Top,
			Padding  = new( 6, 2, 6, 2),
			Margin   = new(16, 2, 2, 2),
			MinWidth = 60,
		};
		public readonly TextBox LanguageValueTextBox = new() {
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment  .Top,
			Background = Brushes.Transparent,
			Padding    = new(8,2,0,2),
			Margin     = new(2),
			Visibility = Visibility.Collapsed,
			MinWidth   = 0,
			IsReadOnly = true,
		};

		public ImageButton ViewerButton { get; } = new() {
			ToolTip   = "Default",
			Uri       = Resource.Uri("PakItemEbin.png")
		};

		//...........................................................

		protected NMS.PAK.MBIN.Data m_data;

		public NMS.PAK.MBIN.Data Data {
			get { return m_data; }
			set {
				if( m_data == value ) return;
				m_data             = value;
				EditorText         = m_data?.CreateEBIN();
				SourceLabel.Text    = m_data?.FilePath?.NameExt;
				Editor.CaretOffset = 0;
			}
		}

		//...........................................................

		protected NMS.PAK.Item.View m_viewer;

		protected override void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		{
			base.OnLoaded(SENDER, ARGS);

			// we start expanded to put less stress on color background renderer
			// which doesn't know to skip collapsed sections.
			//CollapseAll();  // optional

			// are we embedded in a PAK.Item.View control,
			// if so then catch d-click so we can set m_viewer.Breadcrumb.
			m_viewer = this.FindParent<NMS.PAK.Item.View>();
			if( m_viewer != null ) {
				EditorArea.MouseDoubleClick += OnMouseDoubleClick;
			}
		}

		//...........................................................

		public string SearchLanguageId {
			get { return LanguageIdTextBox.Text; }
			set { DoSearchLanguageId(value); }
		}

		//...........................................................

		protected void DoSearchLanguageId( string ID )
		{
			var found = NMS.Game.Data.Selected?.FindLanguageId(ID);
			ID        = found?.Id;
			var text  = found?.Text;

			LanguageIdTextBox   .Text = text.IsNullOrEmpty() ? null : ID;
			LanguageValueTextBox.Text = text;

			if( LanguageValueTextBox.Text.IsNullOrEmpty() ) {
				LanguageValueTextBox.Visibility = Visibility.Collapsed;
			}
			else {
				LanguageValueTextBox.Visibility = Visibility.Visible;
			}
		}

		//...........................................................

		protected void OnMouseDoubleClick( object SENDER, MouseButtonEventArgs ARGS )
		{
			var text = StringAtMouse();

			// seach even if text is null to clear search.
			// return if was able to match string to an ID.
			DoSearchLanguageId(text);
			if( !LanguageIdTextBox.Text.IsNullOrEmpty() ) return;

			// text not an id, check if it's an item path.
			text = NMS.PAK.Item.Path.NormalizeExtension(text, true);
			if( text.IsNullOrEmpty() ) return;

			// constrain search to Breadcrumb tree
			var node  = m_viewer.Breadcrumb.TreeSource.Find(text);
			if( node == null ) {
				if( text.EndsWith(".MBIN", StringComparison.OrdinalIgnoreCase) ) text += ".PC";
				node = m_viewer.Breadcrumb.TreeSource.Find(text);
			}
			if( node != null ) m_viewer.Breadcrumb.SelectedNode = node;
		}
	}
}

//=============================================================================
