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
using System.Threading;
using System.Windows;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
    /// <summary>
    /// View PAK.Item from a specified PAK.Item.Info.Node.
    /// </summary>
    public class View
	: cmk.MainDockPanel
	{
		// browser like prev|next stack
		protected struct StackItem
		{
			public readonly NMS.PAK.Item.Info.Node Node;
			public readonly NMS.PAK.Item.Data      Data;
			public readonly UIElement              Viewer;
			public StackItem(
				NMS.PAK.Item.Info.Node NODE,
				NMS.PAK.Item.Data      DATA,
				UIElement              VIEWER
			){
				Node   = NODE;
				Data   = DATA;
				Viewer = VIEWER;
			}
		}
		protected List<StackItem> m_stack       = new();
		protected int             m_stack_index = -1;     // index of current data|view in m_stack
		protected bool            in_navigate   = false;  // hack

		//...........................................................

		public View() : base()
		{
			ToolWrapPanelLeft.Children.Add(PrevButton);
			ToolWrapPanelLeft.Children.Add(NextButton);

			ToolWrapPanelLeft.Children.Add(Breadcrumb);

			ToolWrapPanelLeft.Children.Add(CopyButton);
			ToolWrapPanelLeft.Children.Add(PasteButton);
			ToolWrapPanelLeft.Children.Add(SaveButton);
			ToolWrapPanelLeft.Children.Add(CancelSaveButton);

			PrevButton.Click += OnPrevClick;
			NextButton.Click += OnNextClick;

			Breadcrumb.SelectionChanged += OnItemBreadcrumbSelectionChanged;

			CopyButton      .Click += OnCopyClick;
			PasteButton     .Click += OnPasteClick;
			SaveButton      .Click += OnSaveClick;
			CancelSaveButton.Click += OnCancelSaveClick;

			LayoutUpdated += OnLayoutUpdated;
		}

		//...........................................................

		public readonly ImageButton PrevButton = new() {
			Uri       = Resource.Uri("Prev.png"),
			IsEnabled = false,
		};
		public readonly ImageButton NextButton = new() {
			Uri       = Resource.Uri("Next.png"),
			Margin    = new Thickness(0, 0, 4, 0),
			IsEnabled = false,
		};

		public readonly Breadcrumb Breadcrumb = new();

		public readonly ImageButton CopyButton = new() {
			ToolTip = "Copy Path To Clipboard",
			Uri     = Resource.Uri("Copy.png"),
			Margin  = new Thickness(4, 0, 0, 0),
		};
		public readonly ImageButton PasteButton = new() {
			ToolTip = "Paste Path From Clipboard",
			Uri     = Resource.Uri("Paste.png"),
			Margin  = new Thickness(0, 0, 4, 0),
		};

		public readonly ImageButton SaveButton = new() {
			ToolTip   = "Save",
			Uri       = Resource.Uri("Save.png"),
			IsEnabled = true
		};
		public readonly ImageButton CancelSaveButton = new() {
			ToolTip   = "Cancel Save",
			Uri       = Resource.Uri("Stop.png"),
			IsEnabled = false,
		};

		//...........................................................

		public UIElement Viewer {
			get {
				return ClientGrid.Children.Count < 1 ?
					null : ClientGrid.Children[0]
				;
			}
			protected set {
				ClientGrid.Children.Clear();
				if( value != null ) ClientGrid.Children.Add(value);
			}
		}

		//...........................................................

		protected NMS.PAK.Item.Info.Node m_tree;

		public NMS.PAK.Item.Info.Node TreeSource {
			get { return m_tree; }
			set {
				// may be called if pak is replaced e.g. built new mod
				//if( m_tree == value ) return;

				PrevButton.IsEnabled = false;
				NextButton.IsEnabled = false;

				var selected_path = Breadcrumb.SelectedPath;

				m_stack.Clear();
				m_stack_index = -1;

				Breadcrumb.SelectedNode = null;
				Breadcrumb.TreeSource   = null;

				m_tree = value;

				Breadcrumb.TreeSource   = m_tree;
				Breadcrumb.SelectedPath = selected_path;
			}
		}

		//...........................................................

		/// <summary>
		/// Try to select PATH in current TreeSource.
		/// </summary>
		public void SelectItem( string PATH, TextSearchData SEARCH = null )
		{
			SelectItem(TreeSource?.Find(PATH), SEARCH);
		}

		/// <summary>
		/// Try to get PAK.Item.Info.Node for INFO and select it.
		/// </summary>
		public void SelectItem( NMS.PAK.Item.Info INFO, TextSearchData SEARCH = null )
		{
			if( INFO == null ) return;
			SelectItem(INFO.TreeNode, SEARCH);
		}

		/// <summary>
		/// If TreeSource is PAK.Item.Info.Node then select NODE,
		/// changing TreeSource if needed.
		/// </summary>
		public virtual void SelectItem( NMS.PAK.Item.Info.Node NODE, TextSearchData SEARCH = null )
		{
			if( TreeSource != NODE?.Root ) TreeSource = NODE?.Root;
			Breadcrumb.SelectedNode = NODE;
			Search(SEARCH);
		}

		//...........................................................

		public NMS.PAK.Item.Info.Node SelectedNode => Breadcrumb.SelectedNode as PAK.Item.Info.Node;
		public NMS.PAK.Item.Data      SelectedData {
			get => SelectedNode?.Tag is NMS.PAK.Item.Info info ?
				info.ExtractData() : null
			;
		}

		//...........................................................

		public void Search( TextSearchData SEARCH )
		{
			if( SEARCH == null ) return;
			if( Viewer is TextViewer text_view ) {
				text_view.SearchPanel.Data = SEARCH;
			}
			if( Viewer is ITextDiffer text_diff ) {
				text_diff.TextViewerRight.SearchPanel.Data = SEARCH;
			}
		}

		//...........................................................

		protected UIElement GetViewer( NMS.PAK.Item.Data DATA )
		{
			if( DATA == null ) return null;

			NMS.PAK.Item.Data game_data = null;
			if( !DATA.FileInPCBANKS ) {
				game_data = NMS.Game.Data.Selected?.PCBANKS.ExtractData(DATA.Path.Full);
			}

			return DATA.GetViewer(game_data);
		}

		//...........................................................

		protected virtual void OnLayoutUpdated( object SENDER, EventArgs ARGS )
		{
			Breadcrumb.MaxDropDownHeight = ClientGrid.ActualHeight - 4;
		}

		//...........................................................

		protected void OnPrevClick( ImageButton SENDER )
		{
			if( m_stack_index >= 0 ) {
				if( --m_stack_index >= 0 ) {
					in_navigate = true;
					// triggers OnItemBreadcrumbSelectionChanged once per path segment
					Breadcrumb.SelectedNode = m_stack[m_stack_index].Node;
					in_navigate = false;
				}
			}
		}

		//...........................................................

		protected void OnNextClick( ImageButton SENDER )
		{
			if( m_stack_index < m_stack.Count ) {
				if( ++m_stack_index < m_stack.Count ) {
					in_navigate = true;
					// triggers OnItemBreadcrumbSelectionChanged once per path segment
					Breadcrumb.SelectedNode = m_stack[m_stack_index].Node;
					in_navigate = false;
				}
			}
		}

		//...........................................................

		protected void OnItemBreadcrumbSelectionChanged( Breadcrumb SENDER, IPathNode SELECTED )
		{
			if( in_navigate ) {  // handling prev|next click
				var current = m_stack[m_stack_index];
				Breadcrumb.ToolTip = current.Data.FilePath?.Full;
				Viewer             = current.Viewer;
			}
			else {
				var node = SELECTED  as PAK.Item.Info.Node;  // may be null
				var info = node?.Tag as NMS.PAK.Item.Info;
				var data = info?.ExtractData();

				Breadcrumb.ToolTip = data?.FilePath.Full;
				Viewer             = GetViewer(data);

				if( data != null ) {
					++m_stack_index;
					m_stack.RemoveRange(m_stack_index, m_stack.Count - m_stack_index);
					m_stack.Add(new(node, data, Viewer));
				}
			}

			PrevButton.IsEnabled = m_stack_index > 0;
			NextButton.IsEnabled = m_stack_index < (m_stack.Count - 1);

			if( m_stack_index > 8 ) { // trim old history
				m_stack.RemoveRange(0, m_stack_index - 8);
				m_stack_index = 8;
			}
		}

		//...........................................................

		protected void OnCopyClick( ImageButton SENDER )
		{
			// may be partial path, i.e. not a pak item (leaf)
			Clipboard.SetText(Breadcrumb.SelectedPath);
		}

		//...........................................................

		protected void OnPasteClick( ImageButton SENDER )
		{
			Breadcrumb.SelectedPath = NMS.PAK.Item.Path.NormalizeExtension(Clipboard.GetText(), true);
		}

		//...........................................................

		public CancellationTokenSource CancellationTokenSource { get; protected set; } = new();

		protected void OnSaveClick( ImageButton SENDER )
		{
			var selected_node  = SelectedNode;
			if( selected_node == null ) return;

			var selected_data  = SelectedData;
			if( selected_data == null ) {  // selected_node is a folder
				var path = "";
				try {  // pick location to save all items under selected_node
					var dialog = new cmk.SelectFolderDialog();
					if( dialog.ShowDialog() == true ) path = dialog.Path;
				}
				catch( Exception EX ) {
					Log.Default.AddFailure(EX);
					return;
				}
				if( path.IsNullOrEmpty() ||
					!System.IO.Directory.Exists(path)
				)	return;

				try {
					CancellationTokenSource = new();
					var cancel = CancellationTokenSource.Token;
					CancelSaveButton.IsEnabled = true;
					selected_node.ForEachTag(( INFO, CANCEL, LOG ) => {
						var file_path   = System.IO.Path.Join(path, INFO.Path);
						var data_stream = INFO.ExtractData()?.Raw;
						cmk.IO.File.WriteAllStream(file_path, data_stream);
					},	cancel, Log.Default);
				}
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
				finally { CancelSaveButton.IsEnabled = false; }
			}
			else if( m_stack_index >= 0 ) {  // selected_node is an item
				m_stack[m_stack_index].Data.SaveFileDialog();
				return;
			}
		}

		//...........................................................

		protected void OnCancelSaveClick( ImageButton SENDER )
		{
			CancellationTokenSource.Cancel();
		}
	}
}

//=============================================================================
