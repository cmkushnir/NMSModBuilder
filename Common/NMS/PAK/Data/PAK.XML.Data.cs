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

using System.IO;

//=============================================================================

namespace cmk.NMS.PAK.XML
{
	public class Data
	: cmk.NMS.PAK.TXT.Data
	{
		static Data()
		{
			s_extensions[".EXML"] = new(typeof(Data), typeof(Viewer), typeof(Differ));
			s_extensions[".XML"]  = new(typeof(Data), typeof(Viewer), typeof(Differ));
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null )
		: base(INFO, RAW)
		{
		}

		//...........................................................

		public Data( Game.Data GAME, string PATH, Stream RAW = null )
		: base(GAME, PATH, RAW)
		{
		}
	}
}

//=============================================================================
