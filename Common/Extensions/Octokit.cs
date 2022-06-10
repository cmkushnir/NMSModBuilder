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
using Octokit;

//=============================================================================

namespace cmk
{
	public static class GitHub
	{
		/// <summary>
		/// Disable all GitHub queries.
		/// Must be set|changed before first access of a given instance Releases.
		/// </summary>
		public static bool Disabled = false;

		//...........................................................

		/// <summary>
		/// Create and return a new GitHubClient using the specified
		/// USER_AGENT_NAME and USER_AGENT_VERSION to build the http user agent header.
		/// Note: USER_AGENT_VERSION can be any string.
		/// </summary>
		public static GitHubClient CreateClient( string USER_AGENT_NAME, string USER_AGENT_VERSION = null )
		{
			return new GitHubClient(new ProductHeaderValue(USER_AGENT_NAME, USER_AGENT_VERSION));
		}
	}

	//=========================================================================

	public static partial class _x_
	{
		/// <summary>
		/// For unauthenticated requests, the rate limit allows for up to 60 requests per hour.
		/// Unauthenticated requests are associated with the originating IP address, and not the user making requests.
		/// </summary>
		public static Octokit.RateLimit Rate( this GitHubClient CLIENT )
		{
			var    rates = CLIENT?.Miscellaneous.GetRateLimits().Result;
			return rates?.Resources.Core;
		}

		//...........................................................

		/// <summary>
		/// The GitHub search API has a different rate limit than the other API's.
		/// </summary>
		public static Octokit.RateLimit RateSearch( this GitHubClient CLIENT )
		{
			var    rates = CLIENT?.Miscellaneous.GetRateLimits().Result;
			return rates?.Resources.Search;
		}

		//...........................................................

		/// <summary>
		/// If request would exceed rate limit returns null,
		/// else returns list of all releases for USER/REPOSITORY.
		/// To get latest or latest public caller should cache Releases
		/// and search to get required Release.
		/// </summary>
		public static List<Octokit.Release> Releases(
			this GitHubClient CLIENT,
			string USER,
			string REPOSITORY
		){
			if( (Rate(CLIENT)?.Remaining ?? 0) < 1 ) return null;
			return new(CLIENT.Repository.Release.GetAll(
				USER, REPOSITORY
			).Result);
		}

		//...........................................................

		/// <summary>
		/// Return first (latest) Release from RELEASES.
		/// </summary>
		public static Octokit.Release LatestRelease(
			this List<Octokit.Release> RELEASES
		){
			return RELEASES?[0];
		}

		//...........................................................

		/// <summary>
		/// Return first (latest) Release from RELEASES that isn't a Draft or Prerelease.
		/// </summary>
		public static Octokit.Release LatestPublicRelease(
			this List<Octokit.Release> RELEASES
		){
			if( RELEASES != null )
			foreach( var release in RELEASES ) {
				if( !release.Draft && !release.Prerelease ) return release;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Return first Release where TagName == RELEASE_TAGNAME (case-insensitive).
		/// </summary>
		public static Octokit.Release TaggedRelease(
			this List<Octokit.Release> RELEASES,
			string RELEASE_TAGNAME
		){
			if( (RELEASES?.Count ?? 0) < 1 || RELEASE_TAGNAME.IsNullOrEmpty() ) return null;

			foreach( var release in RELEASES ) {
				if( string.Equals(release.TagName, RELEASE_TAGNAME, System.StringComparison.OrdinalIgnoreCase) ) {
					return release;
				}
			}

			return null;
		}

		//...........................................................

		public static Octokit.ReleaseAsset TaggedReleaseAsset(
			this List<Octokit.Release> RELEASES,
			string RELEASE_TAGNAME,
			string ASSET_NAME
		){
			return Asset(TaggedRelease(RELEASES, RELEASE_TAGNAME), ASSET_NAME);
		}

		//...........................................................

		/// <summary>
		/// Scan all RELEASE assets for first with Name == ASSET_NAME (case-insensitive).
		/// </summary>
		public static Octokit.ReleaseAsset Asset(
			this Octokit.Release RELEASE,
			string ASSET_NAME
		){
			if( (RELEASE?.Assets?.Count ?? 0) < 1 || ASSET_NAME.IsNullOrEmpty() ) return null;

			foreach( var asset in RELEASE.Assets ) {
				if( string.Equals(asset.Name, ASSET_NAME, System.StringComparison.OrdinalIgnoreCase) ) {
					return asset;
				}
			}

			return null;
		}
	}
}

//=============================================================================
