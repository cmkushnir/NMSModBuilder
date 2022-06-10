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
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

//=============================================================================

namespace cmk
{
	public class GitHubButton
	: cmk.ImageButton
	{
		public GitHubButton( string USER, string REPOSITORY ) : base()
		{
			Uri        = Resource.Uri("GitHub.png");
			User       = USER;
			Repository = REPOSITORY;

			ToolTipService.SetInitialShowDelay(this, 0);
			ToolTipService.SetShowDuration(this, 60000);
		}

		//...........................................................

		protected string m_user = "";

		public string User {
			get { return m_user; }
			set {
				if( m_user == value ) return;
				m_user  = value;
				ToolTip = Url;
			}
		}

		//...........................................................

		protected string m_repository = "";

		public string Repository {
			get { return m_repository; }
			set {
				if( m_repository == value ) return;
				m_repository = value;
				ToolTip      = Url;
			}
		}

		//...........................................................

		public string Url {
			get { return $"https://github.com/{User}/{Repository}"; }
		}

		//...........................................................

		protected override void OnClick( ImageButton SENDER, MouseButtonEventArgs ARGS )
		{
			base.OnClick(SENDER, ARGS);
			try {
				var info = new ProcessStartInfo {
					FileName        = Url,
					UseShellExecute = true,
				};
				_ = Process.Start(info);
				ARGS.Handled = true;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
		}
	}
}

//=============================================================================
