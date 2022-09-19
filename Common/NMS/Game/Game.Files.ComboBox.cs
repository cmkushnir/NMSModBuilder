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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

//=============================================================================

namespace cmk.NMS.Game.Files
{
    public class ComboBox
	: cmk.ComboBox
	{
		// pcbanks and all mod pak's.
		protected static readonly ObservableCollection<NMS.PAK.Item.INamedCollection> m_list = new();

		public static NMS.Game.Data Game { get; protected set; } = null;

		//...........................................................

		static ComboBox()
		{
			NMS.Game.Data.SelectedChanged += OnGameSelectedChanged;
		}

		//...........................................................

		public ComboBox() : base(false)
		{
			ToolTip = "Select pak item collection: PCBANKS (blank) or mod";

			var textblock = FrameworkElementFactory.CreateTextBlock(-1, -1, null, FontWeights.Normal, "PakItemCollectionName");
			textblock.SetValue(TextBlock.PaddingProperty, new Thickness(0));

			ItemTemplate = new(){ VisualTree = textblock };
			ItemsSource  = m_list;

			BindingOperations.EnableCollectionSynchronization(ItemsSource, ItemsSource);

			m_list.CollectionChanged += OnListChanged;
		}

		//...........................................................

		public NMS.PAK.Item.INamedCollection Collection {
			get => SelectedItem as NMS.PAK.Item.INamedCollection;
		}

		public NMS.PAK.Item.Info.Node Tree {
			get => Collection?.PakItemCollectionTree;
		}

		//...........................................................

		protected static void OnGameSelectedChanged( Data OLD, Data NEW )
		{
			if( Game != null ) {
				Game.MODS.CollectionChanged -= OnGameMODSChanged;
			}
			m_list.Clear();

			Game = NEW;
			if( Game == null ) return;

			// will trigger combobox SelectionChanged
			m_list.Add(Game.PCBANKS);

			if( Game?.MODS != null ) {
				lock(Game.MODS) foreach( var pak in Game.MODS.List ) {
					m_list.Add(pak);
				}
				Game.MODS.CollectionChanged += OnGameMODSChanged;
			}
		}

		//...........................................................

		protected static void OnGameMODSChanged( object SENDER, NotifyCollectionChangedEventArgs ARGS )
		{
			// add 1 to index' to account for null entry at start of PakList
			switch( ARGS.Action ) {
				case NotifyCollectionChangedAction.Add: {
					var new_item = ARGS.NewItems[0] as NMS.PAK.File.Loader;
					m_list.Insert(ARGS.NewStartingIndex + 1, new_item);
					break;
				}
				case NotifyCollectionChangedAction.Remove: {
					var old_item = ARGS.OldItems[0] as NMS.PAK.File.Loader;
					m_list.Remove(old_item);
					break;
				}
				case NotifyCollectionChangedAction.Move: {
					var item = ARGS.NewItems[0] as NMS.PAK.File.Loader;
					m_list.Move(ARGS.OldStartingIndex + 1, ARGS.NewStartingIndex + 1);
					break;
				}
				case NotifyCollectionChangedAction.Replace: {
					var item  = ARGS.NewItems[0] as NMS.PAK.File.Loader;
					var index = ARGS.NewStartingIndex + 1;
					m_list[index] = item;
					break;
				}
			}
		}

		//...........................................................

		protected void OnListChanged( object SENDER, NotifyCollectionChangedEventArgs ARGS )
		{
			Dispatcher.Invoke(() => {
				if( SelectedItem == null && !m_list.IsNullOrEmpty() ) {
					SelectedItem  = m_list[0];
				}
			});
		}
	}
}

//=============================================================================
