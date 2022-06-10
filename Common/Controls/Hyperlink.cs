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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

//=============================================================================

namespace cmk
{
	public class Hyperlink
	: System.Windows.Controls.TextBlock
	{
		protected bool m_is_over    = false,
					   m_is_pressed = false;

		public delegate void ClickEventHandler( Hyperlink SENDER, MouseButtonEventArgs ARGS );
		public event         ClickEventHandler Click;

		//...........................................................

		public Hyperlink() : base()
		{
			Cursor = Cursors.Hand;

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

		public Brush DisabledBrush { get; set; } = Brushes.Gray;
		public Brush NormalBrush   { get; set; } = Brushes.Blue;
		public Brush OverBrush     { get; set; } = Brushes.MediumBlue;
		public Brush PressedBrush  { get; set; } = Brushes.LightBlue;

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

		public string Url { get; set; }

		//...........................................................

		protected virtual void Update()
		{
			Foreground = NormalBrush;
			if( !IsEnabled ) {
				Foreground = DisabledBrush;
			}
			else {
				     if( m_is_pressed ) Foreground = PressedBrush;
				else if( m_is_over )    Foreground = OverBrush;
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

			if( IsEnabled ) OnClick(SENDER as Hyperlink, ARGS);
		}

		//...........................................................

		protected virtual void OnClick( Hyperlink SENDER, MouseButtonEventArgs ARGS )
		{
			Click?.Invoke(SENDER, ARGS);
			if( !ARGS.Handled ) try {
				var info = new ProcessStartInfo {
					FileName        = Url,
					UseShellExecute = true,
				};
				_ = Process.Start(info);
				ARGS.Handled = true;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
		}
	}
}

//=============================================================================
