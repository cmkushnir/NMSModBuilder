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
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PAK.MBIN
{
	// Replaces libMBIN.MBINFile
	public class Data
	: cmk.NMS.PAK.Item.Data
	{
		// log_mbin_guid_mismatch command-line setting:
		// normally don't want to flood log window w/ a bunch of warnings
		// about mbin guid != class guid, since the user can't do anything
		// about them, and the ones that are there are likely known to all
		// e.g. new game release breaks stuff, bad mbin in game pak's.
		public static bool LogGuidMismatch = false;

		//...........................................................

		static Data()
		{
			var extension_info = new NMS.PAK.Item.Extension{ Data = typeof(Data) };
			extension_info.Viewers.Insert(0, typeof(ExmlViewer));
			extension_info.Viewers.Insert(0, typeof(EbinViewer));
			extension_info.Differs.Insert(0, typeof(EbinDiffer));

			s_extensions[".MBIN"] = extension_info;
			s_extensions[".PC"]   = extension_info;  // all .PC are .MBIN.PC
		}

		public Data() : base()
		{
		}

		//...........................................................

		/// <summary>
		/// Loads header from Raw.
		/// Does not use MBINC to read header, should work on any version.
		/// </summary>
		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null, Log LOG = null )
		: base(INFO, RAW, LOG)
		{
			Header = INFO?.MbinHeader ?? new Header(Raw, LOG);
			PostConstruct(LOG);
		}

		//...........................................................

		/// <summary>
		/// Create new mbin using a given RAW MBINFile.
		/// </summary>
		public Data( string PATH, Stream RAW = null, Log LOG = null )
		: base(PATH, RAW)
		{
			Header = new Header(Raw, LOG);
			PostConstruct(LOG);
			IsEdited = true;
		}

		//...........................................................

		/// <summary>
		/// Create new mbin using a given RAW MBINFile.
		/// </summary>
		public Data( string PATH, string CLASS, Version MBIN_VERSION, Log LOG = null )
		: base(PATH, null, LOG)
		{
			Header = new Header(CLASS, MBIN_VERSION);
			PostConstruct(LOG);
			if( Class?.Type != null ) {
				IsEdited = true;
				Object(Activator.CreateInstance(Class.Type), LOG);
			}
		}

		//...........................................................

		public Header Header { get; protected set; }

		/// <summary>
		/// MBINC required|used to decode|modify this mbin|mbin.pc item.
		/// </summary>
		public MBINC Mbinc                { get; protected set; }
		public bool  IsMbincLinkedVersion { get => Mbinc?.IsLinkedVersion ?? false; }

		public MBINC.ClassInfo Class     { get; protected set; }  // top-level class
		public string          ClassName { get => Class?.Name ?? ""; }
		public ulong           ClassGuid { get => Class?.NMSAttributeGUID ?? 0; }

		//...........................................................

		protected void PostConstruct( Log LOG )
		{
			if( Header.Version == null ) return;

			Mbinc = MBINC.LoadMbincVersion(Header.Version);
			if( Mbinc == null ) return;

			Class = Mbinc.FindClass(Header.ClassName);

			if( LogGuidMismatch && Header.ClassGuid != ClassGuid ) {
				LOG.AddWarning($"{Path} mbin GUID {Header.ClassGuid:x16} != struct GUID {ClassGuid:x16}");
			}
		}

		//...........................................................

		/// <summary>
		/// Parse current Raw, return new instance of top-level class.
		/// </summary>
		public object ExtractObject( Log LOG = null )
		{
			if( Mbinc == null ||
				ClassName.IsNullOrEmpty() ||
				Raw == null || Raw.Length < Header.Size
			)	return null;
			Raw.Position = Header.Size;  // skip header
			return Mbinc.RawToNMSTemplate(Raw, ClassName, LOG);
		}

		public AS_T ExtractObjectAs<AS_T>( Log LOG )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return ExtractObject(LOG) as AS_T;
		}

		//...........................................................

		/// <summary>
		/// Get new MemoryStream of meta-data tacked onto end of mbin.
		/// </summary>
		public Stream ExtractMeta( Log LOG = null )
		{
			Stream meta = new MemoryStream();
			if( Raw != null &&
				Header.RawPadding > Header.Size &&
				Header.RawPadding < (ulong)Raw.Length
			) {
				Raw.Position = (long)Header.RawPadding;
				Raw.CopyTo(meta);
			}
			return meta;
		}

		//...........................................................

		protected object m_object;

		/// <summary>
		/// Get|set cached instance.
		/// Should only be called on items that are to be modified
		/// as it assumes mbin uses linked libMBIN.
		/// </summary>
		public object Object( Log LOG = null )
		{
			if( m_object == null ) {
				m_object  = ExtractObject(LOG);
			}
			return m_object;
		}
		public void Object( object OBJECT, Log LOG = null )
		{
			m_object = OBJECT;
			Save(LOG);
		}

		public AS_T ObjectAs<AS_T>( Log LOG = null )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return Object(LOG) as AS_T;
		}

		//...........................................................

		protected Stream m_meta;

		/// <summary>
		/// Meta data is added to the end of the file.
		/// It is a distinct (memory) stream from Raw.
		/// </summary>
		public Stream Meta( Log LOG = null )
		{
			if( m_meta == null ) {
				m_meta  = ExtractMeta(LOG);
			}
			return m_meta;
		}
		public void Meta( Stream STREAM, Log LOG = null )
		{
			m_meta = STREAM;
			Save(LOG);
		}

		//...........................................................

		/// <summary>
		/// Write any changes to Data back to Raw.
		/// </summary>
		public override bool Save( Log LOG = null )
		{
			var new_object  = Object(LOG);  // verifies Raw != null
			if( new_object == null ) return false;

			// once file data flagged as edited, keep flagged as edited
			if( !IsEdited ) {
				var old_object = ExtractObject(LOG);
				var old_bytes  = Mbinc.NMSTemplateToBytes(old_object);  // convert current    Raw to data
				var new_bytes  = Mbinc.NMSTemplateToBytes(new_object);  // convert any cached Raw to data
				if( old_bytes == null || new_bytes == null ) return false;
				IsEdited = old_bytes.Length != new_bytes.Length ?
					true : PInvoke.memcmp(old_bytes, new_bytes, new_bytes.Length) != 0
				;
			}
			if( !IsEdited ) return true;

			var new_raw  = Mbinc?.NMSTemplateToRaw(new_object, LOG);
			if( new_raw == null ) return false;

			Raw.Position = 0;
			Raw.SetLength(0);

			Header.Format = IsGameReplacement ? HeaderFormat.V0 : HeaderFormat.V2;
			Header.SaveTo(Raw, LOG);  // write header
			new_raw.CopyTo(Raw);      // append data after header

			// Header.Padding is only set when we parse raw
			// or set header Revision, so safe to test for 0,
			// if 0 then unused i.e. not a TkAnim or TkGeom class.
			var meta  = Meta(LOG);
			if( meta == null || meta.Length < 1 || Header.RawPadding != 0 ) return true;

			var meta_offset = Raw.Position;  // end of header + data
			meta.CopyTo(Raw);  // append meta after header and data

			// update meta offset in header
			var writer = new BinaryWriter(Raw, Encoding.ASCII);
			Raw.Position = Header.Size - 8;
			writer.Write(meta_offset);

			Raw.Position = 0;
			return true;
		}

		//...........................................................

		/// <summary>
		/// Clone this mbin.
		/// </summary>
		public NMS.PAK.MBIN.Data Clone( string PATH, Log LOG = null )
		{
			var raw = new MemoryStream();

			Raw.Position = 0;
			Raw.CopyTo(raw);

			var clone = new NMS.PAK.MBIN.Data(PATH, raw, LOG);
			clone.IsGameReplacement = false;
			clone.IsEdited          = true;

			return clone;
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to new instance of EBIN.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> ebin.
		/// </summary>
		public string CreateEBIN( Log LOG = null )
		{
			return Mbinc?.MbinToEbin(Path, ExtractObject(LOG), LOG) ?? "";
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to new instance of EXML.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> exml.
		/// </summary>
		public string CreateEXML( Log LOG = null )
		{
			return Mbinc?.NMSTemplateToExml(ExtractObject(LOG), LOG) ?? "";
		}
		// we don't support converting exml back to mbin here, use Mbinc.ExmlToNMSTemplate

		//...........................................................

		/// <summary>
		/// Called when saving to disk.
		/// </summary>
		protected override void SaveFilePrepare( SaveFileDialog DIALOG, Log LOG = null )
		{
			base.SaveFilePrepare(DIALOG, LOG);
			DIALOG.Filter  += "|EBIN|*.EBIN" + "|EXML|*.EXML";
			DIALOG.FileName = System.IO.Path.ChangeExtension(DIALOG.FileName, ".MBIN");
		}

		//...........................................................

		/// <summary>
		/// Called when saving to disk.
		/// </summary>
		protected override void SaveFileTo( string PATH, Log LOG = null )
		{
			     if( PATH.EndsWith(".EBIN") ) System.IO.File.WriteAllText(PATH, CreateEBIN(LOG));
			else if( PATH.EndsWith(".EXML") ) System.IO.File.WriteAllText(PATH, CreateEXML(LOG));
			else                              base.SaveFileTo(PATH, LOG);
		}
	}
}

//=============================================================================
