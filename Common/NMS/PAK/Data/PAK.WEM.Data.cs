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
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PAK.WEM
{
	public class Data
	: cmk.NMS.PAK.Item.Data
	{
		static Data()
		{
			var extension = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension.Viewers.Insert(0, typeof(Viewer));
			extension.Differs.Insert(0, typeof(Differ));

			s_extensions[".WEM"] = extension;
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

		//...........................................................

		/// <summary>
		/// Use ww2ogg.exe to convert.
		/// </summary>
		protected MemoryStream RawToOgg( Log LOG = null )
		{
			return new MemoryStream();
			//lock( Raw ) try {
			//	Raw.Position = 0;
			//	using( Process process = new() ) {
			//		process.StartInfo.FileName  = "ww2ogg.exe";
			//		process.StartInfo.Arguments = "-stdin -stdout";  // '-stdin -stdout' use stdin and stdout
			//		process.StartInfo.UseShellExecute        = false;
			//		process.StartInfo.CreateNoWindow         = true;
			//		process.StartInfo.RedirectStandardInput  = true;
			//		process.StartInfo.RedirectStandardOutput = true;
			//		process.Start();
			//		Raw.CopyTo(process.StandardInput.BaseStream);
			//		process.StandardInput.Close();  // required

			//		var stream = new MemoryStream();
			//		process.StandardOutput.BaseStream.CopyTo(stream);
			//		return stream;
			//	}
			//}
			//catch( Exception EX ) {
			//	LOG.AddFailure(EX);
			//	return null;
			//}
		}

		//...........................................................

		protected bool OggToRaw( MemoryStream OGG, Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				OGG.Position = 0;
				throw new NotImplementedException();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		protected override void SaveFilePrepare( SaveFileDialog DIALOG, Log LOG = null )
		{
			base.SaveFilePrepare(DIALOG, LOG);
			DIALOG.Filter  += "|OGG|*.OGG";
			DIALOG.FileName = System.IO.Path.ChangeExtension(DIALOG.FileName, ".WEM");
		}

		//...........................................................

		protected override void SaveFileTo( string PATH, Log LOG = null )
		{
			if( PATH.EndsWith(".OGG") ) {
				if( Raw == null || PATH.IsNullOrEmpty() ) return;
				try {
					var ogg = RawToOgg();
					using( var file = System.IO.File.Create(PATH) ) {
						ogg.CopyTo(file);
					}
				}
				catch( Exception EX ) { LOG.AddFailure(EX); }
			}
			else base.SaveFileTo(PATH, LOG);
		}
	}
}

//=============================================================================
