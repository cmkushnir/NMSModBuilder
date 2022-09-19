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

using System.Threading.Tasks;

//=============================================================================

namespace cmk.NMS.Game
{
    public partial class Data
	{
		public delegate void SelectedChangedEventHandler( NMS.Game.Data OLD, NMS.Game.Data NEW );
		public static event  SelectedChangedEventHandler SelectedChanged;

		//...........................................................

		/// <summary>
		/// Ensures sure only valid Game objects are created.
		/// </summary>
		public static async Task<NMS.Game.Data> CreateAsync( NMS.Game.Location.Data LOCATION )
		{
			if( (LOCATION == null) || !LOCATION.IsValid ) return null;
			var data = await Task.Run(() => new Data(LOCATION));
			return (data?.LanguageId == null) ? null : data;
			// ok if couldn't load language, items, recipes
		}

		//...........................................................

		/// <summary>
		/// Discovered GoG Game instance.  May be null.
		/// </summary>
		public static async Task<NMS.Game.Data> CreateGoGAsync()
		=> await CreateAsync(NMS.Game.Location.Data.GoG);

		//...........................................................

		/// <summary>
		/// Discovered Steam Game instance.  May be null.
		/// </summary>
		public static async Task<NMS.Game.Data> CreateSteamAsync()
		=> await CreateAsync(NMS.Game.Location.Data.Steam);

		//...........................................................

		/// <summary>
		/// Discovered Game Pass Game instance.  May be null.
		/// </summary>
		public static async Task<NMS.Game.Data> CreateGamePassAsync()
		=> await CreateAsync(NMS.Game.Location.Data.GamePass);

		//...........................................................

		/// <summary>
		/// Prompt the user to select a game folder.
		/// If they select the discovered GoG or Steam folder then
		/// returns the GoG or Steam Game instance, don't create a new instance.
		/// </summary>
		public static async Task<NMS.Game.Data> SelectAsync()
		{
			var location  = NMS.Game.Location.Data.Select();
			if( location == null ) return null;

			if( string.Equals(location.Path, NMS.Game.Location.Data.GoG     ?.Path, System.StringComparison.OrdinalIgnoreCase) ) return await CreateGoGAsync();
			if( string.Equals(location.Path, NMS.Game.Location.Data.Steam   ?.Path, System.StringComparison.OrdinalIgnoreCase) ) return await CreateSteamAsync();
			if( string.Equals(location.Path, NMS.Game.Location.Data.GamePass?.Path, System.StringComparison.OrdinalIgnoreCase) ) return await CreateGamePassAsync();

			return await CreateAsync(location);
		}

		//...........................................................

		protected static Data s_selected;

		/// <summary>
		/// The global 'current' Game instance.
		/// Objects that need a Game instance should use this
		/// if they don't provide the ability to specify one.
		/// </summary>
		public static NMS.Game.Data Selected {
			get { return s_selected; }
			set {
				if( s_selected == value ) return;

				var old = s_selected;
				s_selected = value;

				var name = s_selected?.GetType().FullName ?? typeof(Data).FullName;
				var path = s_selected?.Location.Path      ?? "null";

				SelectedChanged?.Invoke(old, s_selected);
				Log.Default.AddHeading($"{name}.Selected changed to {path}");
			}
		}
	}
}

//=============================================================================
