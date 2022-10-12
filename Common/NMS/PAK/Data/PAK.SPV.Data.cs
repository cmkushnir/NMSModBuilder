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
		=> RawToTextSpirvCross(Raw, LOG).Replace("_RESERVED_IDENTIFIER_FIXUP", "");

		//...........................................................

		protected override bool TextToRaw( string TEXT, Log LOG = null )
		{
			try {
				var stream = TextToRawVeldrid(TEXT, LOG);
				lock( Raw ) {
					Raw.Position = 0;
					Raw.SetLength(0);
					stream.CopyTo(Raw);
				}
				return true;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return false;
			}
		}

		//...........................................................

		public string RawToTextSpirvCross( Stream RAW, Log LOG = null )
		{
			lock( RAW ) try {
				RAW.Position = 0;
				using( Process process = new() ) {
					process.StartInfo.FileName  = "spirv-cross.exe";
					process.StartInfo.Arguments = "- -V";  // '-' use stdin, '-V' output vulkan glsl
					process.StartInfo.UseShellExecute        = false;
					process.StartInfo.CreateNoWindow         = true;
					process.StartInfo.RedirectStandardInput  = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
					process.Start();
					RAW.CopyTo(process.StandardInput.BaseStream);
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

		protected readonly Veldrid.SPIRV.CrossCompileOptions m_Veldrid_Cross_Options = new(){
			NormalizeResourceNames = true
		};

		public string RawToTextVeldridCompute( Stream RAW, Log LOG = null )
		{
			try {
				byte[] bytes = null;
				lock( RAW ) {
					RAW.Position = 0;
					bytes = RAW.ToArray();
				}
				var result = Veldrid.SPIRV.SpirvCompilation.CompileCompute(
					bytes, Veldrid.SPIRV.CrossCompileTarget.GLSL, m_Veldrid_Cross_Options
				);
				return result.ComputeShader;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		protected readonly Veldrid.SPIRV.GlslCompileOptions m_Veldrid_Glsl_Options = new(
			true  // preserve debug info
			//, param MacroDefinition[]
		);

		public MemoryStream TextToRawVeldrid( string TEXT, Log LOG = null )
		{
			try {
				Veldrid.ShaderStages stages = Veldrid.ShaderStages.None;
				     if( Path.Name.Contains("_COMP_") ) stages = Veldrid.ShaderStages.Compute;
				else if( Path.Name.Contains("_FRAG_") ) stages = Veldrid.ShaderStages.Fragment;
				else if( Path.Name.Contains("_VERT_") ) stages = Veldrid.ShaderStages.Vertex;

				var spirv = Veldrid.SPIRV.SpirvCompilation.CompileGlslToSpirv(
					TEXT, string.Empty, stages, m_Veldrid_Glsl_Options
				);

				var bytes = spirv.SpirvBytes;
				return new MemoryStream(bytes);
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		public string Main( Log LOG = null )
		{
			var text = RawToTextSpirvCross(Raw, LOG);

			var start = text.IndexOf("void main()");
			var end   = text.LastIndexOf('}') + 1;
			if( start < 0 || end < start ) return "";

			return text.Substring(start, end - start);
		}

		//...........................................................

		public string Main( string MAIN, bool UPDATE = true, Log LOG = null )
		{
			var text = RawToTextSpirvCross(Raw, LOG);

			var start = text.IndexOf("void main()");
			var end   = text.LastIndexOf('}') + 1;
			if( start < 0 || end < start ) return "";

			text = text.Remove(start, end - start);
			text = text.Insert(start, MAIN);

			if( UPDATE ) Text = text;

			return text;
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
