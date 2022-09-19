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
using System.Windows.Media.Imaging;
using Pfim;

//=============================================================================

namespace cmk.NMS.PAK.DDS
{
    public class Data
	: cmk.NMS.PAK.Item.Data
	{
		static Data()
		{
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(Viewer));
			extension_info.Differs.Insert(0, typeof(Differ));
			s_extensions[".DDS"] = extension_info;
		}

		public Data() : base()
		{
		}

		//...........................................................

		public Data( NMS.PAK.Item.Info INFO, Stream RAW, Log LOG = null )
		: base(INFO, RAW, LOG)
		{
			Dds = RawToDDS(LOG);
		}

		//...........................................................

		public Data( string PATH, Stream RAW, Log LOG = null )
		: base(PATH, RAW, LOG)
		{
			Dds = RawToDDS(LOG);
		}

		//...........................................................

		public Pfim.Dds Dds { get; set; }

		public BitmapSource GetBitmap( bool FREEZE = true )
		=> Dds.GetBitmap(FREEZE);

		public BitmapSource GetBitmap( int HEIGHT, bool FREEZE = true )
		=> Dds.GetBitmap(HEIGHT, FREEZE);

		//...........................................................

		protected Pfim.Dds RawToDDS( Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				var config = new Pfim.PfimConfig();
				return Pfim.Dds.Create(Raw, config);
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected bool DDSToRaw( Pfim.Dds DDS, Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				throw new NotImplementedException();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		public override bool Save( Log LOG = null )
		{
			if( Array_x.MemCmp(RawToDDS(LOG)?.Data, Dds?.Data) != 0 ) {
				if( !DDSToRaw(Dds, LOG) ) return false;
				IsEdited = true;
			}
			return base.Save(LOG);
		}
	}
}

//=============================================================================
