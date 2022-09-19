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
using System.Globalization;
using System.IO;
using System.Net.Http;

//=============================================================================

namespace cmk.NMS.Game
{
    public class Release
	{
		public string   Name         { get; }  // e.g. "Fontiers"
		public DateTime Date         { get; }  // Steam NMS.exe build date from PE header
		public Version  GameVersion  { get; }  // release #
		public Version  MbincVersion { get; }  // libMBIN.dll or MBINCompiler.exe Assembly Version
		public string   MbincTag     { get; }  // GitHub TagName, case-insensitive

		//...........................................................

		public Release(
			string   NAME,
			DateTime DATE,
			Version  GAME_VERSION,
			Version  MBINC_VERSION,
			string   MBINC_TAG
		){
			Name         = NAME.Trim();
			Date         = DATE;
			GameVersion  = GAME_VERSION .Normalize();
			MbincVersion = MBINC_VERSION.Normalize();
			MbincTag     = MBINC_TAG.Trim();
		}

		//...........................................................

		/// <summary>
		/// Name.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}
	}

	//=========================================================================

	public class Releases
	{
		protected static readonly Release s_default = new(
			"0.00 Unknown", DateTime.MinValue,
			new(0,0,0,0), new(0,0,0,0), "v0.00.0-pre0"
		);

		public static List<Release> List { get; }

		//...........................................................

		static Releases()
		{
			try {
				var path   = Path.Join(Resource.AppDirectory, "cmkNMSReleases.txt");
				var stream = File.OpenRead(path);
				List = Load(stream);
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX, "Load local cmkNMSReleases.txt:\n"); }
			UpdateListFromGitHub();
		}

		//...........................................................

		protected static List<Release> Load( Stream STREAM )
		{
			var list = new List<Release>();
			if( STREAM != null ) {
				var reader = new StreamReader(STREAM);
				var line   = "";
				while( (line = reader.ReadLine()) != null ) {
					var parts = line.Split(',');
					if( parts.Length < 5 || parts[0][0] == '-' ) continue;
					var release = new Release(
						parts[0].Trim() + " " + parts[1].Trim(),
						DateTime.ParseExact(parts[2].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
						new(parts[0].Trim()),
						new(parts[3].Trim()),
						parts[4]
					);
					list.Add(release);
				}
			}
			return list;
		}

		//...........................................................

		protected static void UpdateListFromGitHub()
		{
			if( !GitHub.Disabled ) try {
				// get cmkNMSReleases.txt from GitHub
				Stream stream;
				using( var http  = new HttpClient() ) {
					http.Timeout = TimeSpan.FromSeconds(8);
					stream = http.GetStreamAsync(
						"https://raw.githubusercontent.com/cmkushnir/NMSModBuilder/main/Common/cmkNMSReleases.txt"
					).Result;
				}
				var git_hub_releases = Load(stream);

				// insert _new_ entries from GitHub into List,
				// don't replace any existing local entries.
				var local_first = List.IsNullOrEmpty() ? s_default : List[0];
				var index       = 0;
				foreach( var git_hub_release in git_hub_releases ) {
					if( git_hub_release.GameVersion <= local_first.GameVersion ) break;
					List.Insert(index++, git_hub_release);
				}
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX, "Download cmkNMSReleases.txt from GitHub:\n"); }
		}

		//...........................................................

		/// <summary>
		/// Get newest Release where BUILD_DATE >= Release.Date.
		/// </summary>
		public static Release FindBuilt( DateTime BUILD_DATE )
		{
			if( List == null ) return s_default;

			for( var i = 0; i < List.Count; ++i ) {
				if( BUILD_DATE >= List[i].Date ) return List[i];
			}

			return List[0];  // DATE before first release, return most recent
		}

		//...........................................................

		/// <summary>
		/// Get newest Release where GAME_VERSION >= Release.Game version.
		/// </summary>
		public static Release FindGameVersion( System.Version GAME_VERSION )
		{
			if( List == null ) return s_default;
			var version = GAME_VERSION.Normalize();

			for( var i = 0; i < List.Count; ++i ) {
				if( version >= List[i].GameVersion ) return List[i];
			}

			return List[0];  // pre v1.0 version, return most recent
		}

		//...........................................................

		/// <summary>
		/// Get oldest Release where Release.MBIN version >= MBIN_VERSION.
		/// </summary>
		public static Release FindMbincVersion( System.Version MBINC_VERSION )
		{
			if( List == null ) return s_default;

			var version = MBINC_VERSION.Normalize();
			if( version.Major < 1 ) return List[0];

			for( var i = List.Count; i-- > 0; ) {
				if( List[i].MbincVersion >= MBINC_VERSION ) return List[i];
			}

			return List[0];  // MBIN_VERSION higher than we know about, return most recent
		}

		//...........................................................

		/// <summary>
		/// Get oldest Release where Release.MBIN version >= MBIN_VERSION.
		/// </summary>
		public static Release FindMbincTag( string MBINC_TAG )
		{
			if( List == null ) return s_default;

			for( var i = 0; i < List.Count; ++i ) {
				if( string.Equals(List[i].MbincTag, MBINC_TAG, StringComparison.OrdinalIgnoreCase) ) return List[i];
			}

			return List[0];  // no match, return most recent
		}
	}
}

//=============================================================================
