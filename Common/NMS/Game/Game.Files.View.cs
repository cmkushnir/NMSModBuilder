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
using System.Windows.Controls;

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
		public View() : base()
		{
			// insert after prev|next, before breadcrumb
			ToolWrapPanelLeft.Children.Insert(2, PakComboBoxRhs);

			PakComboBoxRhs.SelectionChanged += OnPakComboBoxRhsSelectionChanged;
		}

		//...........................................................

		public readonly NMS.Game.Files.ComboBox PakComboBoxRhs = new();

		//...........................................................

		/// <summary>
		/// If TreeSource is PAK.Item.Info.Node then select NODE,
		/// changing TreeSource if needed.
		/// </summary>
		public override void SelectItem( NMS.PAK.Item.Info.Node NODE, TextSearchData SEARCH = null )
		{
			//base.SelectItem(NODE, SEARCH);

			var info   = NODE?.Tag as NMS.PAK.Item.Info;
			var file   = info?.File;
			var is_mod = !(file?.InPCBANKS ?? false);

			// _should_ trigger OnPakComboBoxRhsSelectionChanged, which sets TreeSource.
			PakComboBoxRhs.SelectedItem = is_mod ? file : PakComboBoxRhs.Items[0];
			if( TreeSource != PakComboBoxRhs.Tree ) {
				// OnPakComboBoxRhsSelectionChanged may not have been triggered ?
				// setting TreeSource always does work, even if setting to current value
				// in order to handle cases where a mod pak has been reloaded.
				TreeSource = PakComboBoxRhs.Tree;  
			}

			Breadcrumb.SelectedNode = NODE;
			Search(SEARCH);
		}

		//...........................................................

		/// <summary>
		/// Keep PakComboBox dropdown height within parent window height.
		/// </summary>
		protected override void OnLayoutUpdated( object SENDER, EventArgs ARGS )
		{
			base.OnLayoutUpdated(SENDER, ARGS);
			PakComboBoxRhs.MaxDropDownHeight = ClientGrid.ActualHeight - 4;
		}

		//...........................................................

		/// <summary>
		/// A different pak tree was selected from the combobox.
		/// </summary>
		protected void OnPakComboBoxRhsSelectionChanged( object SENDER, SelectionChangedEventArgs ARGS )
		{
			TreeSource = PakComboBoxRhs.Tree;
		}
	}
}

//=============================================================================
