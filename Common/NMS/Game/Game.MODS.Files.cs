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
	: cmk.NMS.Game.Files.Cache
	, System.Collections.Specialized.INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		//...........................................................

		/// <summary>
		/// GAME must be specified and valid,
		/// it cannot be changed after construction.
		/// </summary>
		public Files( Game.Data GAME, Language.Identifier LANGUAGE_ID )
		: base(GAME, System.IO.Path.Join(GAME.Location.Path, "GAMEDATA", "PCBANKS", "MODS"))
		{
			LanguageId = LANGUAGE_ID ?? NMS.Game.Language.Identifier.Default;

			FileSystemWatcher = new(Path, "*.pak") {
				IncludeSubdirectories = false,
				EnableRaisingEvents   = false,
				NotifyFilter = 0
				| NotifyFilters.FileName
				| NotifyFilters.LastWrite,
				InternalBufferSize = 16384
				// each event takes 16 bytes + (length of file (name+ext+null) * 2 (charw))
				// if we use 128 bytes per event then expect file name+ext <= 55.
				// and an InternalBufferSize = 16384 gives 128 events.
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
		public readonly FileSystemWatcher FileSystemWatcher;

		protected bool m_ReloadCacheOnCollectionChanged = false;

		public bool ReloadCacheOnCollectionChanged {
			get => m_ReloadCacheOnCollectionChanged;
			set {
				if( m_ReloadCacheOnCollectionChanged == value ) return;
				m_ReloadCacheOnCollectionChanged = value;
				ReloadCache(m_ReloadCacheOnCollectionChanged);
			}
		}

		//...........................................................

		public void Delete( NMS.PAK.File.Loader FILE )
		{
			if( FILE == null ) return;

			var path = FILE.Path.Full;

			try   { System.IO.File.Delete(path); }
			catch { return; }

			// wait for OnDeleted to handle the delete
			for(;;) {
				System.Threading.Thread.Sleep(8);
				if( FindFileIndexFromPath(path) < 0 ) break;
			}
		}

		//...........................................................

		protected void OnAdded( string PATH )
		{
			System.Threading.Thread.Sleep(100);

			var file = new NMS.PAK.File.Loader(PATH);
			if( file.Length < 1 ) return;

			int index = -1;

			Lock.AcquireWrite();
			try {
				index = FindFileIndexFromPath(PATH);
				if( index >= 0 ) return;  // already exists

				index = List.BsearchIndexOfInsert(file.Path,
					(ITEM, KEY) => ITEM.CompareTo(KEY)
				);
				List.Insert(index, file);
			}
			finally { Lock.ReleaseWrite(); }

			ReloadCache(ReloadCacheOnCollectionChanged);
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Add, file, index
				)
			);
		}

		//...........................................................

		protected void OnChanged( string PATH )
		{
			System.Threading.Thread.Sleep(100);

			NMS.PAK.File.Loader file_old,
								file_new;
			int index = -1;

			Lock.AcquireWrite();
			try {
				index = FindFileIndexFromPath(PATH);
				if( index < 0 ) return;

				file_old = List[index];
				file_new = new PAK.File.Loader(file_old.Path.Full);

				if( NMS.PAK.File.Loader.Equals(file_old, file_new) ||
					file_new.Length < 1
				)	return;

				List[index] = file_new;
			}
			finally { Lock.ReleaseWrite(); }

			ReloadCache(ReloadCacheOnCollectionChanged);
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Replace, file_new, file_old, index
				)
			);
		}

		//...........................................................

		protected void OnRenamed( string PATH_OLD, string PATH_NEW )
		{
			System.Threading.Thread.Sleep(100);
			if( !File.Exists(PATH_NEW) ) return;

			NMS.PAK.File.Loader file;

			int old_index = -1;
			int new_index = -1;

			Lock.AcquireWrite();
			try {
				old_index = FindFileIndexFromPath(PATH_OLD);
				if( old_index < 0 ) return;

				new_index = List.BsearchIndexOfInsert(PATH_NEW,
					(ITEM, KEY) => ITEM.CompareTo(KEY)
				);
				if( new_index > old_index ) --new_index;  // delete then add

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
			System.Threading.Thread.Sleep(100);
			if( File.Exists(PATH) ) return;

			NMS.PAK.File.Loader file;
			int index = -1;

			Lock.AcquireWrite();
			try {
				index = FindFileIndexFromPath(PATH);
				if( index < 0 ) return;

				file = List[index];
				List.RemoveAt(index);
			}
			finally { Lock.ReleaseWrite(); }

			ReloadCache(ReloadCacheOnCollectionChanged);
			CollectionChanged?.Invoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Remove, file, index
				)
			);
		}
	}
}

//=============================================================================
