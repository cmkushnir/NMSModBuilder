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
	/// Manage a collection (directory) of .pak files.
	/// </summary>
	public class Files
	: cmk.NMS.PAK.Item.ICollection
	{
		/// <summary>
		/// PATH must be valid, it cannot be changed after construction.
		/// </summary>
		/// <param name="PATH">Full path of folder with pak files.</param>
		public Files( string PATH )
		{
			if(  PATH.IsNullOrEmpty() ) return;
			if( !PATH.EndsWith('\\') )  PATH += '\\';

			Path      = PATH;
			IsPCBANKS = NMS.Game.Location.Data.IsPCBANKS(Path);
			IsMODS    = NMS.Game.Location.Data.IsMODS   (Path);

			     if( IsPCBANKS ) SubPath = NMS.Game.Location.Data.SubFolderPCBANKS;
			else if( IsMODS )    SubPath = NMS.Game.Location.Data.SubFolderMODS;
			else                 SubPath = Path;

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
				var pak = new NMS.PAK.File.Loader(FILE_INFO.FullName);
				lock( List ) List.Add(pak);
			});
			List.Sort();

			Log.Default.AddInformation($"Loaded item info from {SubPath}*.pak");
		}

		//...........................................................

		/// <summary>
		/// Full path to directory with .pak files.
		/// </summary>
		public string Path      { get; protected set; } = "";
		public string SubPath   { get; protected set; } = "";     // "\\GAMEDATA\\PCBANKS\\", "\\GAMEDATA\\PCBANKS\\MODS\\", or Path
		public bool   IsPCBANKS { get; protected set; } = false;  // Path.EndsWith("\\GAMEDATA\\PCBANKS\\")
		public bool   IsMODS    { get; protected set; } = false;  // Path.EndsWith("\\GAMEDATA\\PCBANKS\\MODS\\")

		//...........................................................

		// Lock protects List.
		// the (mod) pak files folder is watched for changes and will
		// update List; as such, we Lock all read|write access to List.
		public readonly cmk.ReadWriteLock         Lock = new();
		public readonly List<NMS.PAK.File.Loader> List = new();

		//...........................................................

		public void ClearEbinCache()
		{
			Lock.AcquireWrite();
			try {
				_ = Parallel.ForEach(List, LOADER
					=> LOADER.ClearEbinCache()
				);
			}
			finally { Lock.ReleaseWrite(); }
		}

		//...........................................................

		/// <summary>
		/// Find the index of a pak file in this collection.
		/// PATH is fully qualified path.
		/// Case-sensitive scan of files.
		/// </summary>
		public int FindFileIndexFromPath( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = cmk.IO.Path.Normalize(PATH);
			Lock.AcquireRead();
			try {
				return List.FindIndex(FILE =>
					cmk.IO.Path.Equals(FILE.Path, PATH)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		public File.Loader FindFileFromPath( string PATH, bool NORMALIZE = false )
		{
			Lock.AcquireRead();
			try {
				var index = FindFileIndexFromPath(PATH, NORMALIZE);
				return (index < 0) ? null : List[index];
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// Find the index of a pak file in this collection.
		/// NAME_EXT is the pak file name and extension - no directory.
		/// Case-sensitive scan of files.
		/// </summary>
		public int FindFileIndexFromNameExt( string NAME_EXT )
		{
			NAME_EXT = NAME_EXT.ToUpper();
			Lock.AcquireRead();
			try {
				return List.FindIndex(FILE =>
					string.Equals(FILE.Path.NameExt.ToUpper(), NAME_EXT)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		public File.Loader FindFileFromNameExt( string NAME_EXT )
		{
			Lock.AcquireRead();
			try {
				var index = FindFileIndexFromNameExt(NAME_EXT);
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
			NAME = NAME.ToUpper();
			Lock.AcquireRead();
			try {
				return List.FindIndex(FILE =>
					string.Equals(FILE.Path.Name.ToUpper(), NAME)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		public File.Loader FindFileFromName( string NAME )
		{
			Lock.AcquireRead();
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
		/// Find first where info.Path == PATH.
		/// Searches through pak files in reverse order.
		/// This means we get the same pak item the game would
		/// when a mod contains an override of a game pak item.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);
			if( PATH.IsNullOrEmpty() ) return null;
 			Lock.AcquireRead();
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
		/// Scan all files for all unique MATCHing Info.
		/// Searches through pak files in reverse order.
		/// This means we get the same pak item the game would
		/// when a mod contains an override of a game pak item.
		/// </summary>
		public List<NMS.PAK.Item.Info> FindInfo( Predicate<NMS.PAK.Item.Info> MATCH, bool SORT = true )
		{
			var list = new List<NMS.PAK.Item.Info>();
			Lock.AcquireRead();
			try {
				for( var i = List.Count; i-- > 0; ) {
					var file_list  = List[i].FindInfo(MATCH, false);
					list.Capacity += file_list.Count;
					list.AddRange(file_list);
				}
			}
			finally { Lock.ReleaseRead(); }
			if( SORT ) list.Sort();
			return list;
		}

		//...........................................................

		public List<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoStartsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoContains( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoContains(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoEndsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoRegex( string PATTERN, bool SORT = true, bool WHOLE_WORDS = false, bool CASE_SENS = true, bool PATTERN_IS_REGEX = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoRegex(PATTERN, SORT, WHOLE_WORDS, CASE_SENS, PATTERN_IS_REGEX);

		public List<NMS.PAK.Item.Info> FindInfo( Regex REGEX, bool SORT = true, bool WHOLE_WORDS = false )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfo(REGEX, SORT, WHOLE_WORDS);

		//...........................................................

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		=> ((NMS.PAK.Item.ICollection)this).DefaultExtractData(PATH, NORMALIZE, LOG, CANCEL);

		/// <summary>
		/// Extract the PATH item and wrap in a type specific PAK.Item.Data derived object.
		/// </summary>
		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data
		{
			Lock.AcquireRead();
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
		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null, CancellationToken CANCEL = default )
		{
			Lock.AcquireRead();
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
		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			Lock.AcquireRead();
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
		/// foreach info call HANDLER(INFO, LOG, CANCEL).
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			Lock.AcquireRead();
			try     { List.ForEach(FILE => FILE.ForEachInfo(HANDLER, LOG, CANCEL)); }
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// foreach data extracted call HANDLER(data, LOG, CANCEL).
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			Lock.AcquireRead();
			try     { List.ForEach(FILE =>FILE.ForEachData(HANDLER, LOG, CANCEL)); }
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		/// <summary>
		/// foreach mbin data extracted call HANDLER(mbin, LOG, CANCEL).
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			Lock.AcquireRead();
			try     { List.ForEach(FILE => FILE.ForEachMbin(HANDLER, LOG, CANCEL)); }
			finally { Lock.ReleaseRead(); }
		}
	}
}

//=============================================================================
