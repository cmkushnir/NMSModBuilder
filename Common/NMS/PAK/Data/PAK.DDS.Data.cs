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

//=============================================================================

namespace cmk.NMS.PAK.DDS
{
	public class Data
	: cmk.NMS.PAK.Item.Data
	{
		static Data()
		{
			s_extensions[".DDS"] = new(typeof(Data), typeof(Viewer), typeof(Differ));
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null )
		: base(INFO, RAW)
		{
			Dds = RawToDDS();
		}

		//...........................................................

		public Data( Game.Data GAME, string PATH, Stream RAW = null )
		: base(GAME, PATH, RAW)
		{
			Dds = RawToDDS();
		}

		//...........................................................

		public Pfim.Dds Dds { get; set; }

		//...........................................................

		protected Pfim.Dds RawToDDS()
		{
			lock( Raw ) try {
				Raw.Position = 0;	
				var config = new Pfim.PfimConfig();
				return Pfim.Dds.Create(Raw, config);
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected bool DDSToRaw( Pfim.Dds DDS )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				throw new NotImplementedException();
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		public override bool Save()
		{
			if( Array_x.MemCmp(RawToDDS()?.Data, Dds?.Data) != 0 ) {
				if( !DDSToRaw(Dds) ) return false;
				IsEdited = true;
			}
			return true;
		}
	}
}

//=============================================================================
