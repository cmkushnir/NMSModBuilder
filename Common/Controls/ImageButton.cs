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
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
	public class ImageButton
	: System.Windows.Controls.Border
	{
		protected bool m_is_over    = false,
					   m_is_pressed = false;

		public delegate void ClickEventHandler( ImageButton SENDER, MouseButtonEventArgs ARGS );
		public event         ClickEventHandler Click;

		//...........................................................

		public ImageButton() : base()
		{
			VerticalAlignment   = VerticalAlignment.Center;
			HorizontalAlignment = HorizontalAlignment.Center;
			Padding             = new(4);
			BorderThickness     = new(1);
			BorderBrush         = Brushes.Transparent;

			Child = Image;

			IsEnabledChanged += ( S, E ) => Update();

			MouseEnter += OnMouseEnter;
			MouseLeave += OnMouseLeave;
			MouseDown  += OnMouseDown;
			MouseUp    += OnMouseUp;

			Loaded += OnLoaded;
		}

		//...........................................................

		public Brush  NormalBackground { get; set; } = Resource. NormalBackgroundBrush;
		public Brush    OverBackground { get; set; } = Resource.   OverBackgroundBrush;
		public Brush PressedBackground { get; set; } = Resource.PressedBackgroundBrush;

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

		public Uri Uri {
			set { Source = (value == null) ? null : new BitmapImage(value); }
		}

		public ImageSource Source {
			get { return Image.Source; }
			set {
				if( Image.Source == value ) return;
				Image.Source = value;
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

		protected void OnMouseEnter( object SENDER, MouseEventArgs ARGS )
		{
			m_is_over    = true;
			m_is_pressed = ARGS.LeftButton == MouseButtonState.Pressed;
			Update();
		}

		//...........................................................

		protected void OnMouseLeave( object SENDER, MouseEventArgs ARGS )
		{
			m_is_over    = false;
			m_is_pressed = false;
			Update();
		}

		//...........................................................

		protected void OnMouseDown( object SENDER, MouseButtonEventArgs ARGS )
		{
			m_is_over    = true;
			m_is_pressed = ARGS.LeftButton == MouseButtonState.Pressed;
			Update();
		}

		//...........................................................

		protected void OnMouseUp( object SENDER, MouseButtonEventArgs ARGS )
		{
			m_is_over    = true;
			m_is_pressed = ARGS.LeftButton == MouseButtonState.Pressed;
			Update();

			if( IsEnabled ) OnClick(SENDER as ImageButton, ARGS);
		}

		//...........................................................

		protected virtual void OnClick( ImageButton SENDER, MouseButtonEventArgs ARGS )
		{
			Click?.Invoke(SENDER, ARGS);
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
				     if( m_is_pressed ) Background = PressedBackground;
				else if( m_is_over    ) Background =    OverBackground;
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
