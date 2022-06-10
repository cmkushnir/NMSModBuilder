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
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PAK.SPV
{
	public class Data
	: cmk.NMS.PAK.TXT.Data
	{
		static Data()
		{
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(Viewer));
			extension_info.Differs.Insert(0, typeof(Differ));

			s_extensions[".SPV"] = extension_info;
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
		/// Use spirv-cross.exe to decompile.
		/// </summary>
		protected override string RawToText( Log LOG = null )
		{
			lock( Raw ) try {
				Raw.Position = 0;
				using( Process process = new() ) {
					process.StartInfo.FileName  = "spirv-cross.exe";
					process.StartInfo.Arguments = "- -V";  // '-' use stdin, '-V' output vulkan glsl
					process.StartInfo.UseShellExecute        = false;
					process.StartInfo.CreateNoWindow         = true;
					process.StartInfo.RedirectStandardInput  = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
					process.Start();
					Raw.CopyTo(process.StandardInput.BaseStream);
					process.StandardInput.Close();  // required
					return process.StandardOutput.ReadToEnd();
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected override bool TextToRaw( string TEXT, Log LOG = null )
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

		protected override void SaveFilePrepare( SaveFileDialog DIALOG, Log LOG = null )
		{
			base.SaveFilePrepare(DIALOG, LOG);
			DIALOG.Filter  += "|GLSL|*.GLSL";
			DIALOG.FileName = System.IO.Path.ChangeExtension(DIALOG.FileName, ".SPV");
		}

		//...........................................................

		protected override void SaveFileTo( string PATH, Log LOG = null )
		{
			if( PATH.EndsWith(".GLSL") ) System.IO.File.WriteAllText(PATH, Text);
			else                         base.SaveFileTo(PATH, LOG);
		}
	}
}

//=============================================================================
