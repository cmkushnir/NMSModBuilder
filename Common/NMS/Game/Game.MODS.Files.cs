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
using System.IO;

//=============================================================================

namespace cmk.NMS.Game.MODS
{
	/// <summary>
	/// Manage a collection of mod .pak files.
	/// </summary>
	public class Files
	: cmk.NMS.PAK.Files
	, System.Collections.Specialized.INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		//...........................................................

		/// <summary>
		/// GAME must be specified and valid,
		/// it cannot be changed after construction.
		/// </summary>
		public Files( Game.Data GAME ) : base(GAME, "GAMEDATA\\PCBANKS\\MODS\\")
		{
			if( Game == null ) return;

			FileSystemWatcher = new(Path, "*.pak") {
				IncludeSubdirectories = false,
				EnableRaisingEvents   = false,
				NotifyFilter = 0
				| NotifyFilters.FileName
				| NotifyFilters.LastWrite,
			};
			FileSystemWatcher.Created += ( S, E ) => OnAdded  (E.FullPath);
			FileSystemWatcher.Changed += ( S, E ) => OnChanged(E.FullPath);
			FileSystemWatcher.Deleted += ( S, E ) => OnDeleted(E.FullPath);
			FileSystemWatcher.Renamed += ( S, E ) => OnRenamed(E.OldFullPath, E.FullPath);

			FileSystemWatcher.EnableRaisingEvents = true;
		}

		//...........................................................

		// https://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
		// OnWatcherChanged gets called twice on each mod replace.
		// todo: Collect all events here in a queue.
		//       On each event check if same event in queue, if so replace existing w/ new event.
		//       If get different event than in queue then broadcast queue and put new event in queue.
		//       If no new event in timer span then broadcast queue and clear it.
		//       May need to increase FileSystemWatcher buffer to hold more events.
		public FileSystemWatcher FileSystemWatcher { get; }

		//...........................................................

		protected void OnAdded( string PATH )
		{
			var file  = new NMS.PAK.File.Loader(this, PATH);
			int index = -1;
			Lock.AquireWrite(int.MaxValue);
			try {
				index = List.IndexOfInsert(file.Path, ( FILE, KEY ) => {
					return FILE.CompareTo(KEY);
				});
				List.Insert(index, file);
			}
			finally { Lock.ReleaseWrite(); }
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add, file, index
				)
			);
		}

		//...........................................................

		protected void OnChanged( string PATH )
		{
			NMS.PAK.File.Loader file;
			int index = -1;
			Lock.AquireWrite(int.MaxValue);
			try {
				index = FindFileIndexFromPath(PATH);
				if( index < 0 ) return;

				file = List[index];
				file.Load();
			}
			finally { Lock.ReleaseWrite(); }
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace, file, file, index
				)
			);
		}

		//...........................................................

		protected void OnRenamed( string PATH_OLD, string PATH_NEW )
		{
			NMS.PAK.File.Loader file;
			int old_index = -1;
			int new_index = -1;
			Lock.AquireWrite(int.MaxValue);
			try {
				old_index = FindFileIndexFromPath(PATH_OLD);
				if( old_index < 0 ) return;

				new_index = List.IndexOfInsert(PATH_NEW, ( FILE, KEY ) => {
					return FILE.CompareTo(KEY);
				});
				if( new_index > old_index ) --new_index;

				file = List[old_index];
				file.Path.Full = PATH_NEW;

				if( new_index != old_index ) {
					List.RemoveAt(old_index);
					List.Insert(new_index, file);
				}
			}
			finally { Lock.ReleaseWrite(); }
			if( new_index != old_index ) {
				CollectionChanged?.Invoke(this,
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Move, file, new_index, old_index
					)
				);
			}
		}

		//...........................................................

		protected void OnDeleted( string PATH )
		{
			NMS.PAK.File.Loader file;
			int index = -1;
			Lock.AquireWrite(int.MaxValue);
			try {
				index = FindFileIndexFromPath(PATH);
				if( index < 0 ) return;

				file = List[index];
				List.RemoveAt(index);
			}
			finally { Lock.ReleaseWrite(); }
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Remove, file, index
				)
			);
		}
	}
}

//=============================================================================
