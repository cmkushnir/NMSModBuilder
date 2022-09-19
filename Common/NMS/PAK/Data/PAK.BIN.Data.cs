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

namespace cmk.NMS.PAK.BIN
{
    /// <summary>
    /// Note: some .bin files are binary, some are text.
    /// todo: try to determine if binary and display binary w/ hex viewer.
    /// </summary>
    public class Data
	: cmk.NMS.PAK.TXT.Data
	{
		static Data()
		{
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(Viewer));
			extension_info.Differs.Insert(0, typeof(Differ));
			s_extensions[".BIN"] = extension_info;
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW, Log LOG = null )
		: base(INFO, RAW, LOG)
		{
			CheckIfBinary();
		}

		//...........................................................

		public Data( string PATH, Stream RAW, Log LOG = null )
		: base(PATH, RAW, LOG)
		{
			CheckIfBinary();
		}

		//...........................................................

		public bool IsBinary { get; protected set; } = false;

		protected void CheckIfBinary()
		{
			Raw.Position = 0;

			// some bin are xml, some are actual binary
			if( Raw.ReadByte() != '<' ||
				Raw.ReadByte() != '!' ||
				Raw.ReadByte() != '-'
			) {
				IsBinary = true;
				Text     = "";
			}

			Raw.Position = 0;
		}

		//...........................................................

		protected override string RawToText( Log LOG = null )
		{
			if( IsBinary ) return null;
			return base.RawToText(LOG);
		}

		//...........................................................

		protected override bool TextToRaw( string TEXT, Log LOG = null )
		{
			if( IsBinary ) return false;
			return base.TextToRaw(TEXT, LOG);
		}
	}
}

//=============================================================================
