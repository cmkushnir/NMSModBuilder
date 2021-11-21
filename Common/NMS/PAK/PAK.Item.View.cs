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
using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
	/// <summary>
	/// View PAK.Item from a specified PAK.Item.Info.Node or PAK.Item.Data.Node tree.
	/// </summary>
	public class View
	: cmk.MainDockPanel
	{
		// browser like prev|next stack
		protected struct StackItem
		{
			public readonly IPathNode         Node;
			public readonly NMS.PAK.Item.Data Data;
			public readonly UIElement         Viewer;
			public StackItem( IPathNode NODE, NMS.PAK.Item.Data DATA, UIElement VIEWER )
			{
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
			ToolWrapPanelLeft.Children.Add(SaveButton);

			ToolTipService.SetShowDuration(Breadcrumb, 60000);

			PrevButton.Click += OnPrevClick;
			NextButton.Click += OnNextClick;

			Breadcrumb.SelectionChanged += OnItemBreadcrumbSelectionChanged;

			CopyButton.Click += OnCopyClick;
			SaveButton.Click += OnSaveClick;

			LayoutUpdated += OnLayoutUpdated;
		}

		//...........................................................

		public readonly ImageButton PrevButton = new() {
			Uri       = Resource.Uri("Prev.png"),
			IsEnabled = false,
		};
		public readonly ImageButton NextButton = new() {
			Uri       = Resource.Uri("Next.png"),
			IsEnabled = false,
		};

		public readonly Breadcrumb Breadcrumb = new();

		public readonly ImageButton CopyButton = new() {
			ToolTip = "Copy Path To Clipboard",
			Uri     = Resource.Uri("Copy.png"),
		};
		public readonly ImageButton SaveButton = new() {
			ToolTip   = "Save",
			Uri       = Resource.Uri("Save.png"),
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

		protected IPathNode m_tree;  // PAK.Item.Info.Node or PAK.Item.Data.Node

		/// <summary>
		/// PAK.Item.Info.Node or PAK.Item.Data.Node.
		/// </summary>
		public IPathNode TreeSource {
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
		public void SelectItem( string PATH )
		{
			SelectItem(TreeSource?.Find(PATH));
		}

		/// <summary>
		/// Try to get PAK.Item.Info.Node for INFO and select it.
		/// </summary>
		public void SelectItem( NMS.PAK.Item.Info INFO )
		{
			if( INFO == null ) return;

			// if INFO is from Game.PAK collection then get node from merged Game.PAK.InfoTree,
			// else get node from specific pak file InfoTree.
			var file  = INFO.File;
			var files = file?.Parent as NMS.Game.PCBANKS.Files;
			var tree  = files?.InfoTree ?? file?.InfoTree;
			var node  = tree?.Find(INFO.Path);

			SelectItem(node);
		}

		/// <summary>
		/// If TreeSource is PAK.Item.Info.Node or PAK.Item.Data.Node then select NODE,
		/// changing TreeSource if needed.
		/// </summary>
		public virtual void SelectItem( IPathNode NODE )
		{
			if( TreeSource != NODE?.Root ) TreeSource = NODE?.Root;
			Breadcrumb.SelectedNode = NODE;
		}

		//...........................................................

		protected UIElement GetViewer( NMS.PAK.Item.Data DATA )
		{
			if( DATA == null ) return null;

			NMS.PAK.Item.Data game_data = null;
			if( !DATA.FileInPCBANKS ) {
				game_data = ((ICollection)DATA.Game.PCBANKS).ExtractData(DATA.Path);
			}

			return DATA.GetViewer(game_data);
		}

		//...........................................................

		protected virtual void OnLayoutUpdated( object SENDER, EventArgs ARGS )
		{
			Breadcrumb.MaxDropDownHeight = ClientGrid.ActualHeight - 4;
		}

		//...........................................................

		protected void OnPrevClick( object SENDER, RoutedEventArgs ARGS )
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

		protected void OnNextClick( object SENDER, RoutedEventArgs ARGS )
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
			if( in_navigate ) {
				if( SELECTED.Tag == null ) return;  // building path piece by piece
				var selected = m_stack[m_stack_index];
				Breadcrumb.ToolTip   = selected.Data.FilePath;
				SaveButton.IsEnabled = selected.Data.Raw != null;
				Viewer               = selected.Viewer;
			}
			else {
				NMS.PAK.Item.Data data = null;
				if( SELECTED?.Tag is NMS.PAK.Item.Info info ) data = info.ExtractData();
				else                                          data = SELECTED?.Tag as NMS.PAK.Item.Data;

				Breadcrumb.ToolTip   = data?.FilePath;
				SaveButton.IsEnabled = data?.Raw != null;

				Viewer = GetViewer(data);

				if( data != null ) {
					++m_stack_index;
					m_stack.RemoveRange(m_stack_index, m_stack.Count - m_stack_index);
					m_stack.Add(new(SELECTED, data, Viewer));
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

		protected void OnCopyClick( object SENDER, RoutedEventArgs ARGS )
		{
			// may be partial path, i.e. not a pak item (leaf)
			Clipboard.SetText(Breadcrumb.SelectedPath);
		}

		//...........................................................

		protected void OnSaveClick( object SENDER, RoutedEventArgs ARGS )
		{
			if( m_stack_index >= 0 ) {
				m_stack[m_stack_index].Data.SaveFileDialog();
			}
		}
	}
}

//=============================================================================
