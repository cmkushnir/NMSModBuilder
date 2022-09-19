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

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

//=============================================================================

namespace cmk
{
    public class ListBox
	: System.Windows.Controls.ListBox
	{
		public delegate void VisualChildrenChangedEventHandler( DependencyObject ADDED, DependencyObject REMOVED );
		public event         VisualChildrenChangedEventHandler VisualChildrenChanged;

		//...........................................................

		public ListBox() : base()
		{
			Construct(true);
		}

		//...........................................................

		public ListBox( bool IS_VIRTUALIZING ) : base()
		{
			Construct(IS_VIRTUALIZING);
		}

		//...........................................................

		protected void Construct( bool IS_VIRTUALIZING )
		{
			Background = Brushes.LightGray;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;

			Grid.SetIsSharedSizeScope(this, true);

			if( IS_VIRTUALIZING ) ItemsPanel = new() {
				VisualTree = new System.Windows.FrameworkElementFactory(typeof(VirtualizingStackPanel))
			};
			SetValue(VirtualizingStackPanel.IsVirtualizingProperty, IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.IsVirtualizingWhenGroupingProperty, IS_VIRTUALIZING);
			SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

			IsSynchronizedWithCurrentItem = true;
			SelectionMode = SelectionMode.Single;

			BuildItemContainerStyle();
		}

		//...........................................................

		public ListCollectionView ListCollectionView =>
			CollectionViewSource.GetDefaultView(ItemsSource) as ListCollectionView
		;

		//...........................................................

		/// <summary>
		/// e.g. SelectedBackgroundSetter.Value = Brushes.Yellow;
		/// </summary>
		public readonly Setter SelectedBackgroundSetter = new Setter(
			Grid.BackgroundProperty,
			Brushes.Transparent,
			"ItemFactory"
		);

		public readonly EventSetter PreviewGotKeyboardFocusSetter = new EventSetter(
			ListBoxItem.PreviewGotKeyboardFocusEvent,
			new KeyboardFocusChangedEventHandler(OnPreviewGotKeyboardFocus)
		);

		//...........................................................

		protected ScrollViewer m_scroll_viewer = null;

		/// <summary>
		/// Can only set after control loaded|initialized.
		/// </summary>
		public bool AutoScroll {
			get { return m_scroll_viewer != null; }
			set {
				value = value && HandlesScrolling;
				if( AutoScroll == value ) return;
				m_scroll_viewer = value ?
					this.GetFirstChild<ScrollViewer>() : null
				;
			}
		}

		//...........................................................

		protected void BuildItemContainerStyle()
		{
			var style    = new Style(typeof(ListBoxItem));
			var template = new ControlTemplate(typeof(ListBoxItem));

			var border    = new System.Windows.FrameworkElementFactory(typeof(Border), "ItemFactory");
			var presenter = new System.Windows.FrameworkElementFactory(typeof(ContentPresenter));
			border.AppendChild(presenter);

			template.VisualTree = border;

			var trigger = new Trigger {
				Property = ListBoxItem.IsSelectedProperty,
				Value    = true,
			};
			trigger.Setters.Add(SelectedBackgroundSetter);

			template.Triggers.Add(trigger);

			style.Setters.Add(new Setter(ListBoxItem.TemplateProperty, template));
			style.Setters.Add(PreviewGotKeyboardFocusSetter);

			ItemContainerStyle = style;
		}

		//...........................................................

		protected static void OnPreviewGotKeyboardFocus( object SENDER, KeyboardFocusChangedEventArgs ARGS )
		{
			var sender  = SENDER as ListBoxItem;
			if( sender != null ) sender.IsSelected = true;
		}

		//...........................................................

		/// <summary>
		/// Only called if AutoScroll == true.
		/// </summary>
		protected override void OnItemsChanged( NotifyCollectionChangedEventArgs ARGS )
		{
			base.OnItemsChanged(ARGS);
			if( ARGS.Action == NotifyCollectionChangedAction.Add ) {
				m_scroll_viewer?.ScrollToBottom();
			}
		}

		//...........................................................

		protected override void OnVisualChildrenChanged( DependencyObject ADDED, DependencyObject REMOVED )
		{
			base.OnVisualChildrenChanged(ADDED, REMOVED);
			VisualChildrenChanged?.Invoke(ADDED, REMOVED);
		}
	}
}

//=============================================================================
