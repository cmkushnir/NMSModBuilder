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
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(Viewer));
			extension_info.Differs.Insert(0, typeof(Differ));
			s_extensions[".EXML"] = extension_info;
			s_extensions[".XML"] = extension_info;
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null, Log LOG = null )
		: base(INFO, RAW, LOG)
		{
		}

		//...........................................................

		public Data( string PATH, Stream RAW = null, Log LOG = null )
		: base(PATH, RAW, LOG)
		{
		}
	}
}

//=============================================================================
