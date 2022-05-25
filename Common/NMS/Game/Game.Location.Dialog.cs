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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace cmk.NMS.Game.Location
{
	public class Dialog
	: System.Windows.Window
	{
		public Dialog()
		{
			Title = "Select Game Location";

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Topmost = true;
			Height  = 800;
			Width   = 450;

			Grid.Children.Add(Label);
			Grid.Children.Add(PathLabel);
			Grid.Children.Add(NMSBuiltLabel);
			Grid.Children.Add(ReleasesComboBox);
			Grid.Children.Add(FolderTreeView);
			Grid.Children.Add(OkButton);
			Grid.Children.Add(CancelButton);

			Content = Grid;

			ReleasesComboBox.ItemsSource       = Game.Releases.List;
			ReleasesComboBox.SelectionChanged += OnReleaseChanged;

			FolderTreeView.SelectedItemChanged += OnFolderChanged;

			OkButton    .Click += OnOK;
			CancelButton.Click += OnCancel;

			LoadDrives();
		}

		//...........................................................

		protected readonly Grid  Grid  = new();
		protected readonly Label Label = new() {
			VerticalAlignment = VerticalAlignment.Top,
			Content = "Select game install directory and correct the detected version if needed.",
			Margin  = new(10,10,10,0),
		};
		protected readonly Label PathLabel = new() {
			VerticalAlignment = VerticalAlignment.Top,
			Margin  = new(10,41,10,0),
		};
		protected readonly Label NMSBuiltLabel = new() {
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment   = VerticalAlignment.Top,
			Margin  = new(10,74,0,0),
		};
		protected readonly ComboBox ReleasesComboBox = new() {
			VerticalAlignment = VerticalAlignment.Top,
			Margin   = new(292,76,10,0),
			MinWidth = 130,
		};
		protected readonly TreeView FolderTreeView = new() {
			Margin = new(10,103,10,35),
		};
		protected readonly Button OkButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Content = "OK",
			Margin  = new(0,0,90,10),
			Width   = 75,
		};
		protected readonly Button CancelButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Content = "Cancel",
			Margin  = new(0,0,10,10),
			Width   = 75,
		};

		//...........................................................

		// GameLocation parts:
		public string   Path    { get; protected set; }
		public DateTime Built   { get; protected set; }
		public Release  Release { get; protected set; }

		public bool IsValid {
			get {
				return
					!Path.IsNullOrEmpty() &&
					Built   != DateTime.MinValue &&
					Release != null
			  ;
			}
		}

		public Game.Location.Data Data {
			get { return Release == null ? null : new(Path, Release); }
		}

		//...........................................................

		protected TreeViewItem NewItem( DirectoryInfo INFO )
		{
			var item   = new TreeViewItem {
				Header = INFO.Name,
				Tag    = INFO
			};
			item.Expanded += OnExpanded;
			return item;
		}

		//...........................................................

		protected void LoadDrives()
		{
			foreach( var drive in DriveInfo.GetDrives() ) {
				try   { if( drive.TotalSize == 0 ) continue; }
				catch { continue; }  // e.g. CDRom w/ no disk

				DirectoryInfo sub;
				try   { sub = new(drive.Name); }
				catch { continue; }

				var item   = NewItem(sub);
				var index  = FolderTreeView.Items.Add(item);
				if( index >= 0 ) LoadBranch(item);
			}
		}

		//...........................................................

		protected void LoadBranch( TreeViewItem ITEM )
		{
			if( ITEM.Items.Count > 0 ) return;

			var folder = ITEM.Tag as DirectoryInfo;
			IEnumerable<DirectoryInfo> subs;

			try   { subs = folder.EnumerateDirectories(); }
			catch { return; }  // e.g. access denied 'c:\System Volume Information'

			foreach( var sub in subs ) {
				if(  sub.Name.First() == '$' ||
					(sub.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0
				)	continue;  // recycle bin, system info, ...
				var item = NewItem(sub);
				ITEM.Items.Add(item);
			}
		}

		//...........................................................

		protected void OnReleaseChanged( object SENDER, SelectionChangedEventArgs ARGS )
		{
			Release = ReleasesComboBox.SelectedItem as Release;
		}

		//...........................................................

		protected void OnFolderChanged( object SENDER, RoutedPropertyChangedEventArgs<object> ARGS )
		{
			var item    = ARGS.NewValue as TreeViewItem;
			var folder  = item?.Tag     as DirectoryInfo;
			if( folder == null ) return;

			var exe_path = System.IO.Path.Combine(folder.FullName, "Binaries", "NMS.exe");
			if( !File.Exists(exe_path) ) {
				Path    = null;
				Built   = DateTime.MinValue;
				Release = null;
				NMSBuiltLabel.Content  = null;
				PathLabel.Content = null;
				ReleasesComboBox.SelectedItem = null;
			}
			else {
				Path    = folder.FullName + '\\';
				Built   = cmk.IO.File.PEBuildDate(exe_path);
				Release = Game.Releases.FindBuilt(Built);
				NMSBuiltLabel.Content = string.Format(
					"NMS.exe : {0:0000}-{1:00}-{2:00}",
					Built.Year, Built.Month, Built.Day
				);
				PathLabel.Content = Path;
				ReleasesComboBox.SelectedItem = Release;
			}
		}

		//...........................................................

		protected void OnExpanded( object SENDER, RoutedEventArgs ARGS )
		{
			var sender = SENDER as TreeViewItem;
			foreach( TreeViewItem item in sender.Items ) {
				LoadBranch(item);
			}
		}

		//...........................................................

		protected void OnOK( object SENDER, RoutedEventArgs ARGS )
		{
			DialogResult = IsValid;
			Close();
		}

		//...........................................................

		protected void OnCancel( object SENDER, RoutedEventArgs ARGS )
		{
			DialogResult = false;
			Close();
		}
	}
}

//=============================================================================
