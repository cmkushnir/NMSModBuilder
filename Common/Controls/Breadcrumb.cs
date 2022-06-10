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
using System.Windows.Controls;
using System.Windows.Input;

//=============================================================================

namespace cmk
{
	public class Breadcrumb
	: System.Windows.Controls.WrapPanel
	{
		protected static readonly Thickness s_padding = new(0, 0, 1, 0);

		public delegate void SelectionChangedEventHandler( Breadcrumb SENDER, IPathNode SELECTED );
		public event         SelectionChangedEventHandler SelectionChanged;

		//...........................................................

		public Breadcrumb() : base()
		{
			VerticalAlignment = VerticalAlignment.Center;
			Children.Add(Root);
			Root.SelectionChanged += OnSelectionChanged;

			ToolTipService.SetInitialShowDelay(this, 0);
			ToolTipService.SetShowDuration(this, 60000);
		}

		//...........................................................

		protected IPathNode m_tree;

		public IPathNode TreeSource {
			get { return m_tree; }
			set {
				if( m_tree == value ) return;
				SelectedNode     = null;
				m_tree           = value;
				Root.ItemsSource = value?.Items;
			}
		}

		//...........................................................

		public readonly ComboBox Root = new() {
			Padding       = s_padding,
			IsEditable    = false,
			IsReadOnly    = true,
			SelectedIndex = -1,
			Tag           =  1,
		};

		//...........................................................

		public Style ComboboxStyle {
			get { return Root.Style; }
			set {
				if( Root.Style != value )
				foreach( ComboBox combobox in Children ) {
					combobox.Style = value;
				}
			}
		}

		//...........................................................

		public double MaxDropDownHeight {
			get { return Root.MaxDropDownHeight; }
			set {
				if( Root.MaxDropDownHeight != value )
				foreach( ComboBox combobox in Children ) {
					combobox.MaxDropDownHeight = value;
				}
			}
		}

		//...........................................................

		public string SelectedPath {
			get { return SelectedNode?.Path ?? ""; }
			set { SelectedNode = TreeSource?.Find(value); }
		}

		//...........................................................

		public IPathNode SelectedNode {
			get {
				var node  = GetSelectedChild(Children.Count - 1);
				if( node == null ) {
					node  = GetSelectedChild(Children.Count - 2);
				}
				return node;
			}
			set {
				if( SelectedNode == value ) return;

				var path = value?.PathNodes;
				if( path.IsNullOrEmpty() ) {
					Root.SelectedIndex = -1;
					return;
				}

				for( int i = 1; i < path.Count && i <= Children.Count; ++i ) {
					var child = Children[i - 1] as ComboBox;
					child.SelectedItem = path[i];
				}
			}
		}

		//...........................................................

		protected IPathNode GetSelectedChild( int INDEX )
		{
			if( INDEX < 0 || INDEX >= Children.Count ) return null;
			var    child = Children[INDEX] as ComboBox;
			return child?.SelectedItem     as IPathNode;
		}

		//...........................................................

		protected void OnSelectionChanged( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var sender = SENDER as ComboBox;
			var index  = (int)sender.Tag;

			while( Children.Count > index ) {
				Children.RemoveAt(Children.Count - 1);
			}

			var selected = sender.SelectedItem as IPathNode;
			SelectionChanged?.Invoke(this, selected);
			if( selected == null ) return;

			sender.MoveFocus(new(FocusNavigationDirection.Next));
			if( selected.Items == null ) return;

			var combobox = new ComboBox{
				ItemsSource       = selected.Items,
				Style             = Root.Style,
				MaxDropDownHeight = Root.MaxDropDownHeight,
				Padding           = s_padding,
				IsEditable        = false,
				IsReadOnly        = true,
				SelectedIndex     = -1,
				Tag               = index + 1,
			};
			combobox.SelectionChanged += OnSelectionChanged;

			Children.Add(combobox);
		}
	}
}

//=============================================================================
