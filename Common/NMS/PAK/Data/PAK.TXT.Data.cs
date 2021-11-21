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
			s_extensions[".TXT"] = new(typeof(Data), typeof(Viewer), typeof(Differ));
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null )
		: base(INFO, RAW)
		{
			Text = RawToText();
		}

		//...........................................................

		public Data( Game.Data GAME, string PATH, Stream RAW = null )
		: base(GAME, PATH, RAW)
		{
			Text = RawToText();
		}

		//...........................................................

		/// <summary>
		/// Callers should modify Text instead of Raw.
		/// Constructor will convert Raw to Text.
		/// Save will convert Text to Raw before saving.
		/// </summary>
		public string Text { get; set; }

		//...........................................................

		protected virtual string RawToText()
		{
			lock( Raw ) try {
				Raw.Position = 0;
				using( var reader = new StreamReader(Raw, Encoding.UTF8, leaveOpen:true) ) {
					return reader.ReadToEnd();
				}
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected virtual bool TextToRaw( string TEXT )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				using( var writer = new StreamWriter(Raw, Encoding.UTF8, leaveOpen:true) ) {
					writer.Write(TEXT);
					return true;
				}
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		public override bool Save()
		{
			if( RawToText() != Text ) {
				if( !TextToRaw(Text) ) return false;
				IsEdited = true;
			}
			return true;
		}
	}
}

//=============================================================================
