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
using System.Security.Principal;
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
			try {
				GoG = DiscoverGOG();
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }

			try {
				Steam   = DiscoverSteamFromUninstall();
				Steam ??= DiscoverSteamFromInstall();
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }

			try {
				GamePass   = DiscoverGamePassViaPackageManager();
				GamePass ??= DiscoverGamePassViaGamingRoot();
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }

			HasGOG      = GoG      != null;
			HasSteam    = Steam    != null;
			HasGamePass = GamePass != null;
		}

		//...........................................................

		/// <summary>
		/// We pass BUILD_DATE instead of getting from PATH to handle
		/// cases where may not be able to read NMS.exe e.g. gamepass.
		/// </summary>
		public Data( string PATH, DateTime BUILD_DATE, NMS.Game.Release RELEASE )
		{
			PATH = PATH?.Replace('/', '\\');  // steam: "g:/steam\dir1\dir2\name.ext"
			if( !IsValidGamePath(PATH) ) return;

			if( !PATH.EndsWith('\\') ) PATH = PATH + '\\';

			Path    = PATH;
			Built   = BUILD_DATE;
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
				System.IO.Directory.Exists(BuildPCBANKSPath(PATH)) &&
				System.IO.File     .Exists(BuildExePath(PATH))
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

		public static DateTime PEBuildDate( string PATH, Log LOG = null )
		{
			return cmk.IO.File.PEBuildDate(BuildExePath(PATH), null, LOG);
		}

		//...........................................................

		public static readonly bool HasGOG      = false;
		public static readonly bool HasSteam    = false;
		public static readonly bool HasGamePass = false;

		public static readonly Location.Data GoG;
		public static readonly Location.Data Steam;
		public static readonly Location.Data GamePass;  // xbox

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

		/// <summary>
		/// Try to find single GOG, Steam, or GamePass game instance on system.
		/// If more than 1 exist then prompt the user to select the game location.
		/// If user cancels selection then use GOG if present, else Steam if present, else GamePass if present.
		/// </summary>
		public static Location.Data Discover()
		{
			Location.Data data = null;

			var found_count = 0;
			if( HasGOG )      ++found_count;
			if( HasSteam )    ++found_count;
			if( HasGamePass ) ++found_count;

			if( found_count != 1 ) data = Select();

			if( data == null ) data = GoG;
			if( data == null ) data = Steam;
			if( data == null ) data = GamePass;

			return data;
		}

		//...........................................................

		/// <summary>
		/// Display Select Folder dialog to allow user to select game folder.
		/// </summary>
		public static Location.Data Select()
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
		protected static Location.Data DiscoverGOG()
		{
			var reg  = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games\1446213994");
			var path = reg?.GetValue("path") as string;
			var ver  = reg?.GetValue("ver")  as string;

			if( path.IsNullOrEmpty() ||
				ver .IsNullOrEmpty()
			)	return null;

			var index = ver.IndexOf('_');
			if( index < 0 ) return null;

			ver = ver.Substring(0, index);
			var version = new Version().Normalize();
			if( !Version.TryParse(ver, out version) ) return null;

			var built   = PEBuildDate(path);
			var release = NMS.Game.Releases.FindGameVersion(version);

			return !IsValidGamePath(path) || (release == null) ?
				null : new(path, built, release)
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
		protected static Location.Data DiscoverSteamFromInstall()
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

			return (release == null) ? null : new(found, built, release);
		}

		//...........................................................

		/// <summary>
		/// Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 275850\InstallLocation
		/// </summary>
		protected static Location.Data DiscoverSteamFromUninstall()
		{
			var reg  = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 275850");
			if( reg == null ) return null;

			var path = (string)reg.GetValue("InstallLocation");
			if( !IsValidGamePath(path) ) return null;

			var built   = PEBuildDate(path);
			var release = NMS.Game.Releases.FindBuilt(built);

			return (release == null) ? null : new(path, built, release);
		}

		//...........................................................

		/// <summary>
		/// Use UWP PackageManager to find if NMS is installed.
		/// </summary>
		protected static Location.Data DiscoverGamePassViaPackageManager()
		{
			var manager = new Windows.Management.Deployment.PackageManager();
			var user    = WindowsIdentity.GetCurrent().User;

			// requires admin privileges if we don't supply the current user
			var packages = manager.FindPackagesForUser(user.Value);

			foreach( var package in packages ) {
				if( package.Id.Name != "HelloGames.NoMansSky" ) continue;

				var path = package.InstalledPath;  // C:\Program Files\WindowsApps\HelloGames.NoMansSky_3.991.26223.0_x64__bs190hzg1sesy
				if( !IsValidGamePath(path) ) return null;

				var minor  = package.Id.Version.Minor;  // 991 => 99.1
				var build  = 0;
				if( minor >  99 ) {
					minor /= 10;
					build  = package.Id.Version.Minor - (minor * 10);
				}

				var version = new Version(package.Id.Version.Major, minor, build).Normalize();

				// var built = PEBuildDate(path);  // no read access
				var release = NMS.Game.Releases.FindGameVersion(version);

				return (release == null) ? null : new(path, release.Date, release);
			}

			return null;
		}

		/// <summary>
		/// Check the root of each drive for a ".GamingRoot" file.
		/// If it exists it should start with 52 47 42 58 01 00 00 00
		/// followed by charw path of where xbox games are installed on the drive.
		/// Append "No Man's Sky" to the path to check if it's installed on that drive.
		/// The actual game would be in "No Man's Sky/Content/"
		/// </summary>
		protected static Location.Data DiscoverGamePassViaGamingRoot()
		{
			foreach( var drive_info in System.IO.DriveInfo.GetDrives() ) {
				var gaming_root_path = System.IO.Path.Combine(drive_info.Name, ".GamingRoot");

				if( drive_info.DriveType != DriveType.Fixed ||
					!System.IO.File.Exists(gaming_root_path)
				)	continue;

				var gaming_root_bytes = System.IO.File.ReadAllBytes(gaming_root_path);
				var xbox_path_bytes   = gaming_root_bytes.AsSpan(8);
				var xbox_path         = System.Text.Encoding.Unicode.GetString(xbox_path_bytes);
				var nms_path          = System.IO.Path.Combine(xbox_path, "No Man's Sky/Content");
				if( !IsValidGamePath(nms_path) ) continue;

				var version = new Version().Normalize();
				// get game version 

				// var built = PEBuildDate(nms_path);  // no read access
				var release = NMS.Game.Releases.FindGameVersion(version);

				return (release == null) ? null : new(nms_path, release.Date, release);
			}
			return null;
		}
	}
}

//=============================================================================
