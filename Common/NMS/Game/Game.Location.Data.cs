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
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.Game.Location
{
	public class Data
	{
		public const string SubFolderPCBANKS = "GAMEDATA\\PCBANKS\\";
		public const string SubFolderMODS    = SubFolderPCBANKS + "MODS\\";

		//...........................................................

		static Data()
		{
			GoG     = DiscoverGOG();
			Steam   = DiscoverSteamFromUninstall();
			Steam ??= DiscoverSteamFromInstall();

			HasGOG   = GoG   != null;
			HasSteam = Steam != null;
		}

		//...........................................................

		public Data( string PATH, NMS.Game.Release RELEASE )
		{
			PATH = PATH?.Replace('/', '\\');  // steam: "g:/steam\dir1\dir2\name.ext"
			if( !IsValidGamePath(PATH) ) return;

			if( !PATH.EndsWith('\\') ) PATH = PATH + '\\';

			Path    = PATH;
			Built   = PEBuildDate(PATH);
			Release = RELEASE;
		}

		//...........................................................

		public static bool IsValidGamePath( cmk.IO.Path PATH )
		{
			return IsValidGamePath(PATH?.Directory);
		}
		public static bool IsValidGamePath( string PATH )
		{
			return
				Directory.Exists(BuildPCBANKSPath(PATH)) &&
				System.IO.File.Exists(BuildExePath(PATH))
			;
		}

		public static bool IsPCBANKS( cmk.IO.Path PATH )
		{
			return IsPCBANKS(PATH?.Directory);
		}
		public static bool IsPCBANKS( string PATH )
		{
			return PATH?.EndsWith(
				NMS.Game.Location.Data.SubFolderPCBANKS,
				StringComparison.OrdinalIgnoreCase
			)	?? false;
		}

		public static bool IsMODS( cmk.IO.Path PATH )
		{
			return IsMODS(PATH?.Directory);
		}
		public static bool IsMODS( string PATH )
		{
			return PATH?.EndsWith(
				NMS.Game.Location.Data.SubFolderMODS,
				StringComparison.OrdinalIgnoreCase
			)	?? false;
		}

		//...........................................................

		public static string BuildExePath( string PATH )
		{
			return PATH.IsNullOrEmpty() ?
				"" : System.IO.Path.Join(PATH, "Binaries", "NMS.exe")
			;
		}

		//...........................................................

		public static string BuildPCBANKSPath( string PATH )
		{
			return PATH.IsNullOrEmpty() ?
				"" : System.IO.Path.Join(PATH, SubFolderPCBANKS)
			;
		}

		//...........................................................

		public static string BuildMODSPath( string PATH )
		{
			return PATH.IsNullOrEmpty() ?
				"" : System.IO.Path.Join(PATH, SubFolderMODS)
			;
		}

		//...........................................................

		public static DateTime PEBuildDate( string PATH )
		{
			return cmk.IO.File.PEBuildDate(BuildExePath(PATH));
		}

		//...........................................................

		public static readonly bool HasGOG   = false;
		public static readonly bool HasSteam = false;

		public static readonly Location.Data GoG;
		public static readonly Location.Data Steam;

		public static List<Location.Data> Custom { get; } = new();

		//...........................................................

		public string   Path    { get; }  // game install location
		public DateTime Built   { get; }  // link date from NMS.exe PE header
		public Release  Release { get; }  // best guess at release info

		//...........................................................

		public string ExePath     => BuildExePath    (Path);
		public string PCBANKSPath => BuildPCBANKSPath(Path);
		public string MODSPath    => BuildMODSPath   (Path);

		//...........................................................

		public bool IsValid {
			get { return !Path.IsNullOrEmpty() && Release != null; }
		}

		//...........................................................

		public bool Launch()
		{
			if( IsValid ) try {
				using( Process process = new() ) {
					process.StartInfo.FileName = ExePath;
					//	process.StartInfo.Arguments = "";
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow  = true;
					process.Start();
				}
				return true;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
			return false;
		}

		//...........................................................

		/// <summary>
		/// Try to find single GOG or Steam game instance on system.
		/// If neither or both exist then prompt the user to select the game location.
		/// If user cancels selection then use GOG if present, else Steam if present.
		/// </summary>
		public static Data Discover()
		{
			Data data = null;

			if( HasGOG == HasSteam ) data = Select();
			if( data == null ) data = GoG;
			if( data == null ) data = Steam;

			return data;
		}

		//...........................................................

		/// <summary>
		/// Display Select Folder dialog to allow user to select game folder.
		/// </summary>
		public static Data Select()
		{
			var dialog = new Location.Dialog();
			if( dialog.ShowDialog() != true ) return null;
			return dialog.Data;
		}

		//...........................................................

		/// <summary>
		/// HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1446213994\
		///   path == "G:\GoG\No Man's Sky"
		///   ver  == "3.22_Companions_69111"
		/// </summary>
		protected static Data DiscoverGOG()
		{
			var reg  = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games\1446213994");
			var path = reg?.GetValue("path") as string;
			var ver  = reg?.GetValue("ver")  as string;

			ver = ver?.Substring(0, 4);

			if( path.IsNullOrEmpty() ||
				ver .IsNullOrEmpty()
			)	return null;

			var version = new Version().Normalize();
			if( !Version.TryParse(ver, out version) ) return null;

			var release = NMS.Game.Releases.FindGameVersion(version);

			return !IsValidGamePath(path) || release == null ?
				null : new(path, release)
			;
		}

		//...........................................................

		/// <summary>
		/// https://steamdb.info/ - ID for No Man's Sky == 275850
		/// HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\Apps\275850\
		///   Installed == 1|0
		///   Name      == "No Man's Sky"
		/// HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\
		///   SteamPath == "g:/steam"
		/// g:/steam/steamapps/libraryfolders.vdf - json like, but not json
		/// "LibraryFolders" {
		///    "TimeNextStatsReport"   "xxxxxxxxxxx"
		///    "ContentStatsID"        "xxxxxxxxxxx"
		///    "1"                     "x:/games/steam"
		/// }
		/// g:/steam/steamapps/appmanifest_275850.acf - json like, but not json
		/// If appmanifest_275850 doesn't exist in main steamapps folder then look for it in other
		/// install locations specified in libraryfolders e.g. "x:/games/steam/steamapps/appmanifest_275850.acf".
		/// "AppState" {
		///     ...
		///	    "installdir"  "No Man's Sky"
		///     ...
		/// }
		/// SteamGamePath == <SteamPath>/steamapps/common/<installdir>
		/// </summary>
		protected static Data DiscoverSteamFromInstall()
		{
			// is it: i) an owned Steam game, ii) installed	
			var reg  = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps\275850");
			if( reg == null || (int)reg.GetValue("Installed") == 0 ) return null;

			// is main steam path specified, and does it exist
			reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
			var main_steam_path = reg?.GetValue("SteamPath") as string;
			if( main_steam_path.IsNullOrEmpty() || !System.IO.Directory.Exists(main_steam_path) ) return null;

			var steam_paths = new List<string> { main_steam_path };

			// collect any other user specified steam install locations.
			// as the user adds and removes alternate install locations steam seems to adjust
			// the numbering i.e. the first alternate path will always be "1" "<path>".
			var vdf_path = System.IO.Path.Join(main_steam_path, "steamapps", "libraryfolders.vdf");
			if( System.IO.File.Exists(vdf_path) ) {
				var  vdf = new Steam.File.Loader(vdf_path);  // top-level Attr{"libraryfolders", List)
				if( !vdf.Root.IsNullOrEmpty() ) {
					foreach( var instance in vdf.Root[0].Values ) {
						var kv = instance.Values?.Find(KEY_VALUE =>
							string.Equals(KEY_VALUE.Key, "path", StringComparison.OrdinalIgnoreCase)
						);
						var  path = kv?.Value;
						if( !path.IsNullOrEmpty() ) steam_paths.Add(path);
					}
				}
			}

			var found = "";

			// look through all known steam install locations looking for appmanifest,
			// could also look in vdf for instance that has id in apps list.
			foreach( var steam_path in steam_paths ) {
				var acf_path = System.IO.Path.Join(steam_path, "steamapps", "appmanifest_275850.acf");
				if( !System.IO.File.Exists(acf_path) ) continue;

				var acf = new Steam.File.Loader(acf_path);  // top-level Attr{"AppState", List)
				if( acf.Root.IsNullOrEmpty() ) continue;

				var kv = acf?.Root[0].Values?.Find(KEY_VALUE =>
					string.Equals(KEY_VALUE.Key, "installdir", StringComparison.OrdinalIgnoreCase)
				);

				var path = System.IO.Path.Join(steam_path, "steamapps", "common", kv?.Value) + '\\';
				if( IsValidGamePath(path) ) {
					found = path;
					break;
				}
			}

			if( !IsValidGamePath(found) ) return null;

			var built   = PEBuildDate(found);
			var release = NMS.Game.Releases.FindBuilt(built);

			return release == null ? null : new(found, release);
		}

		//...........................................................

		/// <summary>
		/// Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 275850\InstallLocation
		/// </summary>
		protected static Data DiscoverSteamFromUninstall()
		{
			var reg  = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 275850");
			if( reg == null ) return null;

			var path = (string)reg.GetValue("InstallLocation");
			if( !IsValidGamePath(path) ) return null;

			var built   = PEBuildDate(path);
			var release = NMS.Game.Releases.FindBuilt(built);

			return release == null ? null : new(path, release);
		}
	}
}

//=============================================================================
