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
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
	public class ImageCheckButton
	: System.Windows.Controls.Border
	{
		protected bool m_is_over = false;

		public delegate void CheckedChangedEventHandler( ImageCheckButton SENDER, bool IS_CHECKED );
		public event         CheckedChangedEventHandler CheckedChanged;

		//...........................................................

		public ImageCheckButton() : base()
		{
			VerticalAlignment   = VerticalAlignment.Center;
			HorizontalAlignment = HorizontalAlignment.Center;
			Padding             = new(4);
			BorderThickness     = new(1);
			BorderBrush         = Brushes.Transparent;

			Child = Image;

			ToolTipService.SetInitialShowDelay(this, 0);
			ToolTipService.SetShowDuration(this, 60000);

			IsEnabledChanged += ( S, E ) => Update();

			MouseEnter += OnMouseEnter;
			MouseLeave += OnMouseLeave;
			MouseDown  += OnMouseDown;
			MouseUp    += OnMouseUp;

			Loaded += OnLoaded;
		}

		//...........................................................

		public Brush NormalBackground  { get; set; } = Resource.NormalBackgroundBrush;
		public Brush OverBackground    { get; set; } = Resource.OverBackgroundBrush;
		public Brush CheckedBackground { get; set; } = Resource.PressedBackgroundBrush;

		//...........................................................

		public readonly Image Image = new() {
			Stretch             = Stretch.None,
			VerticalAlignment   = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		//...........................................................

		protected bool m_is_enabled = true;

		new public bool IsEnabled {
			get { return m_is_enabled; }
			set {
				if( m_is_enabled == value ) return;
				m_is_enabled = value;
				Update();
			}
		}

		//...........................................................

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

		public Uri Uri {
			set { Source = (value == null) ? null : new BitmapImage(value); }
		}
		public Uri UriNormal {
			set { SourceNormal = (value == null) ? null : new BitmapImage(value); }
		}
		public Uri UriChecked {
			set { SourceChecked = (value == null) ? null : new BitmapImage(value); }
		}

		//...........................................................

		protected ImageSource  m_normal_source,
							  m_checked_source;

		public ImageSource Source {
			set {
				SourceNormal  = value;
				SourceChecked = value;
			}
		}
		public ImageSource SourceNormal {
			get { return m_normal_source; }
			set {
				if( m_normal_source == value ) return;
				m_normal_source = value;
				Update();
			}
		}
		public ImageSource SourceChecked {
			get { return m_checked_source; }
			set {
				if( m_checked_source == value ) return;
				m_checked_source = value;
				Update();
			}
		}

		//...........................................................

		protected virtual void OnLoaded( object SENDER, RoutedEventArgs ARGS )
		{
			Loaded -= OnLoaded;
			Update();
		}

		//...........................................................

		protected void OnMouseEnter( object SENDER, System.Windows.Input.MouseEventArgs ARGS )
		{
			m_is_over = true;
			Update();
		}

		//...........................................................

		protected void OnMouseLeave( object SENDER, System.Windows.Input.MouseEventArgs ARGS )
		{
			m_is_over = false;
			Update();
		}

		//...........................................................

		protected void OnMouseDown( object SENDER, System.Windows.Input.MouseButtonEventArgs ARGS )
		{
			m_is_over = true;
			Update();
		}

		//...........................................................

		protected void OnMouseUp( object SENDER, System.Windows.Input.MouseButtonEventArgs ARGS )
		{
			m_is_over    = true;
			m_is_checked = !m_is_checked;
			Update();

			if( IsEnabled ) OnCheckedChanged(m_is_checked);
		}

		//...........................................................

		protected virtual void OnCheckedChanged( bool IS_CHECKED )
		{
			CheckedChanged?.Invoke(this, IS_CHECKED);
		}

		//...........................................................

		protected virtual void Update()
		{
			Background = NormalBackground;

			if( !IsEnabled ) {
				Image.Opacity = Resource.DisabledOpacity;
			}
			else {
				Image.Opacity = 1.0;
				if( m_is_checked ) {
					Background   = CheckedBackground;
					Image.Source = SourceChecked;
				}
				else {
					if( m_is_over ) Background = OverBackground;
					Image.Source = SourceNormal;
				}
			}

			Height = Image.Height
			+   (BorderThickness.Top + BorderThickness.Bottom)
			+   (Padding.Top         + Padding.Bottom)
			;
			Width = Image.Width
			+   (BorderThickness.Left + BorderThickness.Right)
			+   (Padding.Left         + Padding.Right)
			;
		}
	}
}

//=============================================================================
