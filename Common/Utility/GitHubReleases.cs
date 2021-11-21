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
using System.Windows;

//=============================================================================

namespace cmk
{
	/// <summary>
	/// Cache a list of releases for a specific GitHub repository.
	/// </summary>
	public class GitHubReleases
	{
		public GitHubReleases(
			string USER_AGENT_NAME,    // GitHub user       name for app
			string USER_AGENT_VERSION, // GitHub repository name for app
			string USER,               // GitHub user       name for query
			string REPOSITORY          // GitHub repository name for query
		){
			UserAgentName    = USER_AGENT_NAME;
			UserAgentVersion = USER_AGENT_VERSION;
			User             = USER;
			Repository       = REPOSITORY;
		}

		//...........................................................

		public string UserAgentName    { get; }
		public string UserAgentVersion { get; }
		public string User             { get; }
		public string Repository       { get; }

		//...........................................................

		protected List<Octokit.Release> m_releases;

		public List<Octokit.Release> Releases {
			get {
				lock( this )
				if( m_releases == null && !GitHub.Disabled ) {
					if( UserAgentName   .IsNullOrEmpty() ||
						UserAgentVersion.IsNullOrEmpty() ||
						User            .IsNullOrEmpty() ||
						Repository      .IsNullOrEmpty()
					) {
						m_releases = new();  // invalid query info, don't try again
					}
					else try {
						var client = GitHub.CreateClient(UserAgentName, UserAgentVersion);
						var rate   = client.Rate();
						if( rate.Remaining > 0 ) {
							m_releases = client.Releases(User, Repository);
						}
						else {
							MessageBox.Show(  // don't init m_releases, allow try again later
								$"Hit GitHub query rate limit {rate.Limit}/hr\nResets: {rate.Reset.LocalDateTime}.",
								"Version Check",
								MessageBoxButton.OK,
								MessageBoxImage.Error
							);
						}
					}
					catch( Exception EX ) { Log.Default.AddFailure(EX); }
				}
				return m_releases;
			}
		}
	}
}

//=============================================================================
