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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace cmk
{
	public class SelectFolderDialog
	: System.Windows.Window
	{
		public SelectFolderDialog()
		{
			Title = "Select Folder";

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Topmost = true;
			Height  = 800;
			Width   = 450;

			Grid.Children.Add(PathLabel);
			Grid.Children.Add(TreeView);
			Grid.Children.Add(OkButton);
			Grid.Children.Add(CancelButton);

			Content = Grid;

			TreeView.SelectedItemChanged += OnFolderChanged;

			OkButton.Click += OnOK;
			CancelButton.Click += OnCancel;

			LoadDrives();
		}

		//...........................................................

		public string Path { get; protected set; }

		//...........................................................

		public readonly Grid Grid = new();

		public readonly Label PathLabel = new() {
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new(10, 10, 10, 0),
		};

		public readonly TreeView TreeView = new() {
			Margin = new(10, 35, 10, 35),
		};

		public readonly Button OkButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Content = "OK",
			Margin  = new(0, 0, 90, 10),
			Width   = 75,
		};
		public readonly Button CancelButton = new() {
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment   = VerticalAlignment.Bottom,
			Content = "Cancel",
			Margin  = new(0, 0, 10, 10),
			Width   = 75,
		};

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
				try { if( drive.TotalSize == 0 ) continue; }
				catch { continue; }  // e.g. CDRom w/ no disk

				DirectoryInfo sub;
				try { sub = new(drive.Name); }
				catch { continue; }

				var item   = NewItem(sub);
				var index  = TreeView.Items.Add(item);
				if( index >= 0 ) LoadBranch(item);
			}
		}

		//...........................................................

		protected void LoadBranch( TreeViewItem ITEM )
		{
			if( ITEM.Items.Count > 0 ) return;

			var folder = ITEM.Tag as DirectoryInfo;
			IEnumerable<DirectoryInfo> subs;

			try { subs = folder.EnumerateDirectories(); }
			catch { return; }  // e.g. access denined 'c:\System Volume Information'

			foreach( var sub in subs ) {
				if( sub.Name.First() == '$' ||
					(sub.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0
				)	continue;  // recycle bin, system info, ...
				var item = NewItem(sub);
				ITEM.Items.Add(item);
			}
		}

		//...........................................................

		protected void OnFolderChanged( object SENDER, RoutedPropertyChangedEventArgs<object> ARGS )
		{
			var item    = ARGS.NewValue as TreeViewItem;
			var folder  = item?.Tag     as DirectoryInfo;
			if( folder == null ) return;

			Path = folder.FullName + '\\';
			PathLabel.Content = Path;
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
			DialogResult = true;
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
