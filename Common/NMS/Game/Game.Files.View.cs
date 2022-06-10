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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

//=============================================================================

namespace cmk.NMS.Game.Files
{
	/// <summary>
	/// Adds a combobox to select merged game pak tree or a mod pak tree as breadcrumb source.
	/// Combobox is rebuilt when Game is changed, and is updated whenever
	/// a pak file in the mods folder is changed.
	/// </summary>
	public class View
	: cmk.NMS.PAK.Item.View
	{
		// null entry in pak combobobx indicates should view merged pak tree.
		protected static readonly NMS.PAK.File.Loader s_null_pak_file = new();

		// s_null_pak_file (to represent merged game pak tree) and all mod pak's.
		protected readonly ObservableCollection<NMS.PAK.File.Loader> m_pak_list = new();

		//...........................................................

		public View() : base()
		{
			// insert after prev|next, before breadcrumb
			ToolWrapPanelLeft.Children.Insert(2, PakComboBox);

			var textblock = FrameworkElementFactory.CreateTextBlock(-1, -1, null, FontWeights.Normal, "Path.Name", "Path.Full");
			textblock.SetValue(TextBlock.PaddingProperty, new Thickness(0));

			PakComboBox.ItemTemplate = new() { VisualTree = textblock };
			PakComboBox.ItemsSource  = m_pak_list;
			if( PakComboBox.ItemsSource != null ) {
				BindingOperations.EnableCollectionSynchronization(PakComboBox.ItemsSource, PakComboBox.ItemsSource);
			}

			m_pak_list.CollectionChanged += OnPakListChanged;
			PakComboBox.SelectionChanged += OnPakComboBoxSelectionChanged;
		}

		//...........................................................

		public readonly ComboBox PakComboBox = new() {
			ToolTip    = "Select pak item tree: merged game paks (blank), or a mod pak.",
			Padding    = new(4, 0, 4, 0),
			IsEditable = false,
			IsReadOnly = true,
		};

		//...........................................................

		protected Game.Data m_game;

		public Game.Data Game {
			get { return m_game; }
			set {
				if( m_game == value ) return;
				if( m_game?.MODS != null ) {
					m_game.MODS.CollectionChanged -= OnMODSChanged;
				}

				m_pak_list.Clear();
				m_game = value;
				m_pak_list.Add(s_null_pak_file);

				if( m_game?.MODS != null ) {
					lock( m_game.MODS ) foreach( var pak in m_game.MODS.List ) {
						m_pak_list.Add(pak);
					}
					m_game.MODS.CollectionChanged += OnMODSChanged;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// If TreeSource is PAK.Item.Info.Node then select NODE,
		/// changing TreeSource if needed.
		/// </summary>
		public override void SelectItem( NMS.PAK.Item.Info.Node NODE )
		{
			var info   = NODE?.Tag as NMS.PAK.Item.Info;
			var file   = info?.File;
			var is_mod = !(file?.InPCBANKS ?? false);

			// triggers OnPakComboBoxSelectionChanged, which sets TreeSource.
			PakComboBox.SelectedItem = is_mod ? file : s_null_pak_file;
			Breadcrumb .SelectedNode = NODE;
		}

		//...........................................................

		/// <summary>
		/// Keep PakComboBox dropdown height within parent window height.
		/// </summary>
		protected override void OnLayoutUpdated( object SENDER, EventArgs ARGS )
		{
			base.OnLayoutUpdated(SENDER, ARGS);
			PakComboBox.MaxDropDownHeight = ClientGrid.ActualHeight - 4;
		}

		//...........................................................

		/// <summary>
		/// A MODS/*.pak file changed (add|delete|update|rename).
		/// </summary>
		protected void OnMODSChanged( object SENDER, NotifyCollectionChangedEventArgs ARGS )
		{
			Dispatcher.Invoke(() => {
				// add 1 to index' to account for null entry at start of PakList
				switch( ARGS.Action ) {
					case NotifyCollectionChangedAction.Add: {
						var new_item = ARGS.NewItems[0] as NMS.PAK.File.Loader;
						m_pak_list.Insert(ARGS.NewStartingIndex + 1, new_item);
						break;
					}
					case NotifyCollectionChangedAction.Remove: {
						var old_item = ARGS.OldItems[0] as NMS.PAK.File.Loader;
						if( PakComboBox.SelectedItem == old_item ) {
							PakComboBox.SelectedItem  = s_null_pak_file;
						}
						m_pak_list.Remove(old_item);
						break;
					}
					case NotifyCollectionChangedAction.Move: {
						var item = ARGS.NewItems[0] as NMS.PAK.File.Loader;
						m_pak_list.Move(ARGS.OldStartingIndex + 1, ARGS.NewStartingIndex + 1);
						break;
					}
					case NotifyCollectionChangedAction.Replace: {
						var item     = ARGS.NewItems[0] as NMS.PAK.File.Loader;
						var index    = ARGS.NewStartingIndex + 1;
						var selected = PakComboBox.SelectedIndex;
						PakComboBox.SelectedIndex = -1;
						m_pak_list[index]         = item;
						PakComboBox.SelectedIndex = selected;
						break;
					}
				}
			});
		}

		//...........................................................

		/// <summary>
		/// The list of mod pak files used for the combobox changed.
		/// </summary>
		protected void OnPakListChanged( object SENDER, NotifyCollectionChangedEventArgs ARGS )
		{
			if( PakComboBox.SelectedItem == null ) {
				PakComboBox.SelectedItem  = s_null_pak_file;
			}
		}

		//...........................................................

		/// <summary>
		/// A different pak tree was selected from the combobox.
		/// </summary>
		protected void OnPakComboBoxSelectionChanged( object SENDER, SelectionChangedEventArgs ARGS )
		{
			if( ARGS?.AddedItems == null || ARGS.AddedItems.Count < 1 ) {
				TreeSource = Game?.PCBANKS.InfoTree;
				return;
			}

			var file = ARGS.AddedItems[0] as NMS.PAK.File.Loader;
			var name = file?.Path.Name;

			TreeSource = name.IsNullOrEmpty() ?
				Game?.PCBANKS.InfoTree : file?.InfoTree
			;
		}
	}
}

//=============================================================================
