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
using System.Reflection;

//=============================================================================

namespace cmk.NMS
{
    // partial: static cached load methods
    public partial class MBINC
	{
		// cache of loaded libMBIN | MBINCompiler Assemblies
		protected static List<MBINC> s_cache = new();

		//...........................................................

		static MBINC()
		{
			// load any linked libMBIN | MBINCompiler.
			// note: trying to move away from having any linked.
			var asm = Assembly.GetAssembly(typeof(libMBIN.MBINHeader));
			Linked  = new(asm);
			Cache(Linked);
		}

		//...........................................................

		/// <summary>
		/// Cached list of all MBINC releases on GitHub.
		/// Only query once per app run to minimze chance of exceeding rate limit.
		/// Only acquires if GitHubReleases.Disabled = false.
		/// </summary>
		public static readonly GitHubReleases GitHubReleases = new(
			"cmkushnir",    "NMSModBuilder",  // user agent
			"monkeyman192", "MBINCompiler"    // query
		);

		// need to download MBINCompiler.exe for versions before this
		public static readonly Version FirstLibVersion = new(1, 53, 0, 2);

		// MBINC linked to current Application
		public static readonly MBINC Linked;

		//...........................................................

		/// <summary>
		/// Get exact match from cache, return null if not in cache.
		/// </summary>
		protected static MBINC Cached( Version MBINC_VERSION )
		{
			if( MBINC_VERSION.IsNullOrZero() ) return null;
			lock( s_cache ) {
				foreach( var mbinc in s_cache ) {
					var version  = mbinc.Assembly.GetName().Version.Normalize();
					if( version == MBINC_VERSION ) return mbinc;
				}
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Add ASSEMBLY to unique cache.
		/// Cache is maintained in sorted order by desc version.
		/// </summary>
		protected static void Cache( MBINC LIB )
		{
			if( LIB == null || LIB.Assembly == null ) return;
			var version = LIB.Assembly.GetName().Version;
			lock( s_cache ) {
				for( var i = 0; i < s_cache.Count; ++i ) {
					var cache_version  = s_cache[i].Assembly.GetName().Version;
					if( cache_version == version ) return;
					if( version > cache_version ) {
						s_cache.Insert(i, LIB);
						return;
					}
				}
				s_cache.Add(LIB);
			}
		}

		//...........................................................

		/// <summary>
		/// Load MbincVersion for specified game version.
		/// Return cached if available, load from disk if present,
		/// prompt to download if needed.
		/// If invalid GAME_VERSION return most recent.
		/// If GAME_VERSION not in Release list then returns most recent
		/// Release where GAME_VERSION >= Release.Game version.
		/// </summary>
		public static MBINC LoadGameVersion( Version GAME_VERSION )
		{
			var    release = Game.Releases.FindGameVersion(GAME_VERSION);
			return LoadRelease(release);
		}

		//...........................................................

		public static MBINC LoadMbincVersion( Version MBINC_VERSION )
		{
			var    release = Game.Releases.FindMbincVersion(MBINC_VERSION);
			return LoadRelease(release);
		}

		//...........................................................

		public static MBINC LoadMbincTag( string MBINC_TAG )
		{
			var    release = Game.Releases.FindMbincTag(MBINC_TAG);
			return LoadRelease(release);
		}

		//...........................................................

		/// <summary>
		/// Load libMBIN | MBINCompiler for RELEASE.
		/// If not already cached then load|cache from disk.
		/// If not on disk then prompt to download.
		/// </summary>
		public static MBINC LoadRelease( Game.Release RELEASE )
		{
			// since mbin's may be loaded in parallel we could get
			// flooded with popups to download various mbinc releases.
			// lock cache to serialize the popups.
			lock( s_cache ) {
				var lib = Cached(RELEASE.MbincVersion);
				return lib != null ? lib : LoadReleaseLocked(RELEASE);
			}
		}

		protected static MBINC LoadReleaseLocked( Game.Release RELEASE )
		{
			MBINC lib = null;

			// early releases had no libMBIN.dll, only MBINCompiler.exe

			// 1. try to find an existing "libMBIN_...dll" or "MBINCompiler_...exe" in app folder
			var target = Path.Join(Resource.AppDirectory, $"libMBIN_{RELEASE.MbincTag}.dll");
			if( System.IO.File.Exists(target) ) goto found;

			target = Path.Join(Resource.AppDirectory, $"MBINCompiler_{RELEASE.MbincTag}.dll");  // saved as dll
			if( System.IO.File.Exists(target) ) goto found;

			// 2. get mbinc releases from github, get asset for RELEASE
			//    todo: save|load release info so don't have to query each time, but loose octokit parsing
			var release  = GitHubReleases.Releases.TaggedRelease(RELEASE.MbincTag);
			if( release == null ) {
				Log.Default.AddFailure($"Can't find GitHub MBINCompiler release {RELEASE.MbincVersion}");
				return null;
			}
			var source = release.Asset("libMBIN.dll")?.BrowserDownloadUrl;
			    target = Path.Join(Resource.AppDirectory, $"libMBIN_{RELEASE.MbincTag}.dll");
			if( source.IsNullOrEmpty() ) {
				source = release.Asset("MBINCompiler.exe")?.BrowserDownloadUrl;
				target = Path.Join(Resource.AppDirectory, $"MBINCompiler_{RELEASE.MbincTag}.dll");  // save as dll
			}
			if( source.IsNullOrEmpty() ) {
				Log.Default.AddFailure($"Can't find GitHub libMBIN.dll|MBINCompiler.exe asset for release {RELEASE.MbincVersion}");
				return null;
			}

			// if not on disk then download from GitHub
			if( !File.Exists(target) ) {
				var action = () => {
					var dialog = new cmk.DownloadDialog() {
						DescriptionText =
						"You don't have the required version of libMBIN | MBINCompiler.\n" +
						"Would you like to download the required file ?",
						Source     = new(source),
						TargetText = target,
						TargetSelectEnabled = false,  // user can't change Target (path)
					};
					dialog.ShowDialog();
				};
				action.DispatcherInvoke();
				if( !File.Exists(target) ) return null;
			}

			found:
			try {
				// loads into current AppDomain|AssemblyLoadContext
				var asm = Assembly.LoadFile(target);
				lib = (asm == null) ? null : new(asm);
				Cache(lib);
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }

			return lib;
		}
	}
}

//=============================================================================
