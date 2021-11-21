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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk.NMS.PAK
{
	/// <summary>
	/// Manage a collection of .pak files.
	/// </summary>
	public class Files
	: cmk.NMS.PAK.Item.ICollection
	{
		/// <summary>
		/// Both GAME and SUB_PATH must be specified and valid,
		/// they cannot be changed after construction.
		/// SUB_PATH is relative to GAME.Path.
		/// </summary>
		public Files( Game.Data GAME, string SUB_PATH )
		{
			if( GAME == null || SUB_PATH.IsNullOrEmpty() ) return;
			if( !SUB_PATH.EndsWith('\\') ) SUB_PATH += '\\';

			Game    = GAME;
			SubPath = SUB_PATH;
			Path    = System.IO.Path.Combine(Game.Location.Path, SubPath);

			Log.Default.AddInformation($"Loading item info from {SubPath}*.pak");

			var  dir = new cmk.IO.Directory(Path);
			if( !dir.EnsureExists() ) return;  // EnsureExists instead of Exists to create MODS if doesn't exist

			// note: seems game will load anything w/ "pak" (or ".pak" ?) in path (or name ?).
			// Would need to change dir.Files("*.pak") to dir.Files() and then string.Contains
			// on each returned path.  Would also need to change Game.MODS.Files to listen
			// for all changes in dir and have it do string.Contains on each detected change.

			// Collect list of .pak files in Path.
			// The constructor of each will load its manifest and build its own merged InfoTree.
			_ = Parallel.ForEach(dir.Files("*.pak"), FILE_INFO => {
				var pak = new NMS.PAK.File.Loader(this, FILE_INFO.FullName);
				lock( List ) List.Add(pak);
			});
			List.Sort(( LHS, RHS ) => {
				return LHS.CompareTo(RHS);
			});

			Log.Default.AddInformation($"Loaded item info from {SubPath}*.pak");
		}

		//...........................................................

		/// <summary>
		/// Game instance this collection is from.
		/// </summary>
		public readonly Game.Data Game = null;

		/// <summary>
		/// Path to directory with .pak files, relative to Game Path.
		/// </summary>
		public readonly string SubPath = "";

		/// <summary>
		/// Full path to directory with .pak files.
		/// </summary>
		public readonly string Path = "";

		public readonly List<NMS.PAK.File.Loader> List = new();

		//...........................................................

		// the (mod) pak files folder is watched for changes and will
		// update List; as such, we Lock all read|write access to List.
		public readonly cmk.ReadWriteLock Lock = new();

		//...........................................................

		/// <summary>
		/// Find the index of a pak file in this collection.
		/// PATH is fully qualified path.
		/// Case-sensitive binary search of files.
		/// </summary>
		public int FindFileIndexFromPath( string PATH, bool NORMALIZE = false )
		{
			// bsearch overkill for the few # of pak's we generally have
			if( NORMALIZE ) PATH = cmk.IO.Path.Normalize(PATH);
			Lock.AcquireRead(int.MaxValue);
			try {
				return List.IndexOf(PATH, ( FILE, KEY ) => {
					return FILE.CompareTo(KEY);
				});
			}
			finally { Lock.ReleaseRead(); }
		}

		public File.Loader FindFileFromPath( string PATH, bool NORMALIZE = false )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				var index = FindFileIndexFromPath(PATH, NORMALIZE);
				return (index < 0) ? null : List[index];
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Find the index of a pak file in this collection.
		/// NAME is the pak file name - no directory, no extension.
		/// Case-sensitive scan of files.
		/// </summary>
		public int FindFileIndexFromName( string NAME )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				return List.FindIndex(FILE =>
					string.Equals(FILE.Path.Name, NAME)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		public File.Loader FindFileFromName( string NAME )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				var index = FindFileIndexFromName(NAME);
				return (index < 0) ? null : List[index];
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................
		// cmk.NMS.PAK.Item.Interface
		//...........................................................

		/// <summary>
		/// Find first where MATCH.
		/// Searches through pak files in reverse order.
		/// This means we get the same pak item the game would
		/// when a mod contains an override of a game pak item.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( Predicate<NMS.PAK.Item.Info> MATCH )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				for( var i = List.Count; i-- > 0; ) {
					var info  = List[i].FindInfo(MATCH);
					if( info != null ) return info;
				}
				return null;
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Find first where info.Path == PATH.
		/// Searches through pak files in reverse order.
		/// This means we get the same pak item the game would
		/// when a mod contains an override of a game pak item.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);
			Lock.AcquireRead(int.MaxValue);
			try {
				for( var i = List.Count; i-- > 0; ) {
					var info  = List[i].FindInfo(PATH, false);
					if( info != null ) return info;
				}
				return null;
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Info.Path starts with the case-sensitive PATTERN.
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				foreach( var file in List ) {
					foreach( var info in file.FindInfoStartsWith(PATTERN) ) {
						yield return info;
					}
				}
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Info.Path contains the case-sensitive PATTERN.
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoContains( string PATTERN )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				foreach( var file in List ) {
					foreach( var info in file.FindInfoContains(PATTERN) ) {
						yield return info;
					}
				}
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Info.Path ends with the case-sensitive PATTERN.
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				foreach( var file in List ) {
					foreach( var info in file.FindInfoEndsWith(PATTERN) ) {
						yield return info;
					}
				}
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Find all matching info with REGEX Path.
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoRegex( Regex REGEX )
		{
			if( REGEX != null ) {
				Lock.AcquireRead(int.MaxValue);
				try {
					foreach( var file in List ) {
						foreach( var info in file.FindInfoRegex(REGEX) ) {
							yield return info;
						}
					}
				}
				finally { Lock.ReleaseRead(); }
			}
		}

		//...........................................................

		/// <summary>
		/// Extract the PATH item and wrap in a type specific PAK.Item.Data derived object.
		/// </summary>
		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : NMS.PAK.Item.Data
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				var info  = FindInfo(PATH, NORMALIZE);
				if( info == null ) {
					LOG.AddFailure($"{PATH} - unable to find info in {SubPath}*.pak");
					return null;
				}
				return info.ExtractData<AS_T>(LOG);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Extract DDS item and convert to a BitmapSource.
		/// Discards PAK.DDS.Data wrapper after conversion.
		/// </summary>
		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null )
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				var info  = FindInfo(PATH, NORMALIZE);
				if( info == null ) {
					LOG.AddFailure($"{PATH} - unable to find info in {SubPath}*.pak");
					return null;
				}
				return info.ExtractDdsBitmapSource(HEIGHT, LOG);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Extract MBIN or MBIN.PC item then decompile NMSTemplate based object.
		/// Discards PAK.MBIN.Data wrapper after decompiling.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			Lock.AcquireRead(int.MaxValue);
			try {
				var info  = FindInfo(PATH, NORMALIZE);
				if( info == null ) {
					LOG.AddFailure($"{PATH} - unable to find info in {SubPath}*.pak");
					return null;
				}
				return info.ExtractMbin<AS_T>(LOG);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach info call HANDLER(INFO, CANCEL, LOG).
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			Lock.AcquireRead(int.MaxValue);
			try {
				_ = Parallel.ForEach(List,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					FILE => FILE.ForEachInfo(HANDLER, CANCEL, LOG)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach data extracted call HANDLER(data, CANCEL, LOG).
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			Lock.AcquireRead(int.MaxValue);
			try {
				_ = Parallel.ForEach(List,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					FILE => FILE.ForEachData(HANDLER, CANCEL, LOG)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach mbin data extracted call HANDLER(mbin, CANCEL, LOG).
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			Lock.AcquireRead(int.MaxValue);
			try {
				_ = Parallel.ForEach(List,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					FILE => FILE.ForEachMbin(HANDLER, CANCEL, LOG)
				);
			}
			finally { Lock.ReleaseRead(); }
		}
	}
}

//=============================================================================
