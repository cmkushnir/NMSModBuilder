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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

//=============================================================================

namespace cmk
{
	public class DownloadDialog
	: System.Windows.Window
	{
		protected static HttpClient s_http_client;

		//...........................................................

		public DownloadDialog() : base()
		{
			Title = "Download File";

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SizeToContent         = SizeToContent.WidthAndHeight;
			Topmost  = true;
			MinWidth = 400;

			ProgressBar.Minimum =   0;
			ProgressBar.Maximum = 100;

			Grid.Children.Add(DescriptionTextBox);
			Grid.Children.Add(SourceLabelTextBlock);
			Grid.Children.Add(SourceTextBox);
			Grid.Children.Add(TargetLabelTextBlock);
			Grid.Children.Add(TargetTextBox);
			Grid.Children.Add(TargetSelectButton);
			Grid.Children.Add(ProgressBar);
			Grid.Children.Add(ResultTextBox);
			Grid.Children.Add(DownloadButton);
			Grid.Children.Add(CancelCloseButton);

			Content = Grid;

			TargetSelectButton.Click += OnSelectTargetClick;
			    DownloadButton.Click += OnDownloadClick;
			 CancelCloseButton.Click += OnCancelCloseClick;
		}

		//...........................................................

		public readonly Grid Grid = new();

		public readonly TextBox DescriptionTextBox = new() {
			TextWrapping    = TextWrapping.Wrap,
			BorderThickness = new(0),
			Margin          = new(10, 10, 10, 100),
			IsReadOnly      = true,
		};
		public readonly TextBlock SourceLabelTextBlock = new() {
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment   = VerticalAlignment.Bottom,
			TextWrapping = TextWrapping.NoWrap,
			Margin       = new(10, 0, 0, 78),
			Text         = "Source:",
		};
		public readonly TextBox SourceTextBox = new() {
			VerticalAlignment = VerticalAlignment.Bottom,
			TextWrapping      = TextWrapping.NoWrap,
			BorderThickness   = new(0),
			Margin            = new(55, 0, 10, 78),
			IsReadOnly        = true,
		};
		public readonly TextBlock TargetLabelTextBlock = new() {
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment   = VerticalAlignment.Bottom,
			TextWrapping = TextWrapping.NoWrap,
			Margin       = new(10, 0, 0, 57),
			Text         = "Target:",
		};
		public readonly TextBox TargetTextBox = new() {
			VerticalAlignment = VerticalAlignment.Bottom,
			TextWrapping      = TextWrapping.NoWrap,
			BorderThickness   = new(0),
			Margin            = new(55, 0, 31, 57),
			IsReadOnly        = true,
		};
		public readonly Button TargetSelectButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Margin  = new(0, 0, 10, 57),
			Width   = 16,
			Height  = 16,
			Content = "...",
			ToolTip = "Select save location.",
		};
		public readonly ProgressBar ProgressBar = new() {
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new(10, 0, 10, 35),
			Height = 16,
		};
		public readonly TextBox ResultTextBox = new() {
			VerticalAlignment = VerticalAlignment.Bottom,
			TextWrapping      = TextWrapping.NoWrap,
			BorderThickness   = new(0),
			Margin            = new(10, 0, 170, 12),
			IsReadOnly        = true,
		};
		public readonly Button DownloadButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Margin  = new(0, 0, 90, 10),
			Width   = 75,
			Content = "Download",
		};
		public readonly Button CancelCloseButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Margin  = new(0, 0, 10, 10),
			Width   = 75,
			Content = "Close",
		};

		//...........................................................

		public string DescriptionText {
			get { return DescriptionTextBox.Text; }
			set { DescriptionTextBox.Text = value; }
		}

		//...........................................................

		protected Uri m_source;

		public Uri Source {
			get { return m_source; }
			set {
				m_source = value;
				SourceTextBox.Text = m_source.OriginalString;
			}
		}

		//...........................................................

		public string TargetText {
			get { return TargetTextBox.Text; }
			set { TargetTextBox.Text = value; }
		}

		//...........................................................

		public bool TargetSelectEnabled {
			get { return TargetSelectButton.IsEnabled; }
			set { TargetSelectButton.IsEnabled = value; }
		}

		//...........................................................

		public string ResultText {
			get { return ResultTextBox.Text; }
			set { ResultTextBox.Text = value; }
		}

		//...........................................................

		protected void OnSelectTargetClick( object SENDER, RoutedEventArgs ARGS )
		{
			var target_dir = System.IO.Path.GetDirectoryName(TargetText);
			var init_dir   = Directory.Exists(target_dir) ?
				target_dir : Resource.AppDirectory
			;

			var dialog = new SaveFileDialog {
				InitialDirectory = init_dir,
				FileName         = TargetText,
				CreatePrompt     = true,
			};

			if( dialog.ShowDialog() == true ) {
				TargetText = dialog.FileName;
			}
		}

		//...........................................................

		protected CancellationTokenSource m_cancel;

		//...........................................................

		protected async void OnDownloadClick( object SENDER, RoutedEventArgs ARGS )
		{
			if( File.Exists(TargetText) ) File.Delete(TargetText);

			if( m_source == null ||
				m_source.OriginalString.IsNullOrEmpty()
			)	return;

			var sync = SynchronizationContext.Current;
			DownloadButton.Visibility = Visibility.Collapsed;
			CancelCloseButton.Content = "Cancel";
			var result_text = "Download complete.";

			try {
				using( var stream = File.OpenWrite(TargetText) ) {
					var progress = new Progress<byte>(
						PERCENT => ProgressBar.Value = PERCENT
					);
					using( m_cancel = new() ) {
						if( s_http_client == null ) s_http_client = new();
						await s_http_client.RecvAsync(
							m_source, stream,
							progress, m_cancel.Token
						);
					}
				}
			}
			catch( Exception EX ) {
				result_text = EX.Message;
				File.Delete(TargetText);
			}
			finally {
				sync.Send(_ => {
					m_cancel                  = null;
					ResultTextBox.Text        = result_text;
					CancelCloseButton.Content = "Close";
				},	null);
			}
		}

		//...........................................................

		protected void OnCancelCloseClick( object SENDER, RoutedEventArgs ARGS )
		{
			var cancel  = m_cancel;
			if( cancel != null ) cancel.Cancel();
			else                 Close();
		}
	}
}

//=============================================================================
