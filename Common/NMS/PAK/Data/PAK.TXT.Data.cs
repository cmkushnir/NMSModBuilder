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
using System.IO;
using System.Text;

//=============================================================================

namespace cmk.NMS.PAK.TXT
{
    public class Data
	: cmk.NMS.PAK.Item.Data
	{
		static Data()
		{
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(Viewer));
			extension_info.Differs.Insert(0, typeof(Differ));
			s_extensions[".TXT"] = extension_info;
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null, Log LOG = null )
		: base(INFO, RAW, LOG)
		{
			Text = RawToText(LOG);
		}

		//...........................................................

		public Data( string PATH, Stream RAW = null, Log LOG = null )
		: base(PATH, RAW, LOG)
		{
			Text = RawToText(LOG);
		}

		//...........................................................

		/// <summary>
		/// Callers should modify Text instead of Raw.
		/// Constructor will convert Raw to Text.
		/// Save will convert Text to Raw before saving.
		/// </summary>
		public string Text { get; set; }

		//...........................................................

		protected virtual string RawToText( Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				using( var reader = new StreamReader(Raw, Encoding.UTF8, leaveOpen: true) ) {
					return reader.ReadToEnd();
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected virtual bool TextToRaw( string TEXT, Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				Raw.SetLength(0);
				using( var writer = new StreamWriter(Raw, Encoding.UTF8, leaveOpen: true) ) {
					writer.Write(TEXT);
					return true;
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		public override bool Save( Log LOG = null )
		{
			if( RawToText(LOG) != Text ) {
				if( !TextToRaw(Text, LOG) ) return false;
				IsEdited = true;
			}
			return base.Save(LOG);
		}
	}
}

//=============================================================================
