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



//=============================================================================

namespace cmk.NMS.Game.Recipes.Cook
{
	public class Collection
	: cmk.NMS.Game.Recipes.Collection
	{
		public Collection( Game.Data GAME ) : base(GAME, 1000)  // 3.71 - 857
		{
		}

		//...........................................................

		public void Load()
		{
			Load(true);
		}
	}
}

//=============================================================================
