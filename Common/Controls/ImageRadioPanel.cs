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

//=============================================================================

namespace cmk
{
    public class ImageRadioPanel
	: System.Windows.Controls.Border
	{
		public delegate void RadioButtonChangedEventHandler( ImageRadioPanel SENDER, ImageRadioButton BUTTON );
		public event         RadioButtonChangedEventHandler RadioButtonChanged;

		//...........................................................

		public ImageRadioPanel() : base()
		{
			Child = StackPanel;
			StackPanel.VisualChildrenChanged += OnStackPanelVisualChildrenChanged;
		}

		//...........................................................

		public readonly StackPanel StackPanel = new() {
			VerticalAlignment   = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		//...........................................................

		public UIElementCollection Children => StackPanel.Children;

		//...........................................................

		public Orientation Orientation {
			get { return StackPanel.Orientation; }
			set { StackPanel.Orientation = value; }
		}

		//...........................................................

		protected int m_selected_index = 0;

		public int SelectedIndex {
			get { return m_selected_index; }
			set {
				m_selected_index = value;
				if( m_selected_index >= Children.Count ) m_selected_index = 0;
				for( var i = 0; i < Children.Count; ++i ) {
					var button  = Children[i] as ImageRadioButton;
					if( button != null ) {
						button.IsChecked = (i == m_selected_index);
					}
				}
				RadioButtonChanged?.Invoke(this, Selected);
			}
		}

		//...........................................................

		public ImageRadioButton Selected {
			get {
				return SelectedIndex >= Children.Count ?
					null : Children[SelectedIndex] as ImageRadioButton
				;
			}
			set {
				for( var i = 0; i < Children.Count; ++i ) {
					if( Children[i] == value ) {
						SelectedIndex = i;
						return;
					}
				}
				SelectedIndex = 0;
			}
		}

		//...........................................................

		protected void OnStackPanelVisualChildrenChanged( DependencyObject ADDED, DependencyObject REMOVED )
		{
			var radio_button  = REMOVED as ImageRadioButton;
			if( radio_button != null ) {
				radio_button.Click -= OnImageRadioButtonClick;
				if( Selected == radio_button ) SelectedIndex = 0;
			}

			radio_button = ADDED as ImageRadioButton;
			if( radio_button != null ) radio_button.Click += OnImageRadioButtonClick;
		}

		//...........................................................

		protected void OnImageRadioButtonClick( object SENDER )
		{
			Selected = SENDER as ImageRadioButton;
		}
	}

	//=========================================================================

	public class ImageRadioButton
	: cmk.ImageButton
	{
		protected bool m_is_checked = false;

		public bool IsChecked {
			get { return m_is_checked; }
			set {
				if( m_is_checked == value ) return;
				m_is_checked = value;
				Update();
			}
		}

		//...........................................................

		protected override void Update()
		{
			base.Update();
			if( IsEnabled ) {
				     if( m_is_pressed ) Background = PressedBackground;
				else if( m_is_over )    Background = OverBackground;
				else if( m_is_checked ) Background = PressedBackground;
			}
		}
	}
}

//=============================================================================
