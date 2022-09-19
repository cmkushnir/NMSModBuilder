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
using System.Linq;
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
			extension_info.Differs.Insert(0, typeof(ExmlDiffer));
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
		: base(PATH, RAW, LOG)
		{
			Header = new Header(Raw, LOG);
			PostConstruct(LOG);
		}

		//...........................................................

		public Header Header { get; protected set; }

		/// <summary>
		/// MBINC required|used to decode|modify this mbin|mbin.pc item.
		/// </summary>
		public MBINC Mbinc                { get; protected set; } = null;
		public bool  IsMbincLinkedVersion => Mbinc?.IsLinkedVersion ?? false;

		public MBINC.ClassInfo Class     { get; protected set; }  // top-level class
		public string          ClassName => Class?.Name ?? "";
		public ulong           ClassGuid => Class?.NMSAttributeGUID ?? 0;

		protected object m_mod_object = null;
		protected Stream m_mod_meta   = null;

		//...........................................................

		protected void PostConstruct( Log LOG )
		{
			if( Header.Version == null ) return;

			// language mbin's haven't changed since forever,
			// so ignore the header version info.
			if( Path.Full.StartsWith("LANGUAGE/") ) {
				Mbinc = MBINC.Linked;
			}
			else {
				Mbinc = MBINC.LoadMbincVersion(Header.Version);
			}
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
		public object RawObject( Log LOG = null )
		{
			if( Mbinc == null ||
				ClassName.IsNullOrEmpty() ||
				Raw == null || Raw.Length < Header.Size
			)	return null;
			lock( this ) {
				Raw.Position = Header.Size;  // skip header
				return Mbinc.RawToNMSTemplate(Raw, ClassName, LOG);
			}
		}

		public AS_T RawObjectAs<AS_T>( Log LOG )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return RawObject(LOG) as AS_T;
		}

		//...........................................................

		/// <summary>
		/// Get new MemoryStream of mod_meta-data tacked onto end of mbin.
		/// </summary>
		public Stream RawMeta( Log LOG = null )
		{
			Stream meta = new MemoryStream();
			if( Raw != null &&
				Header.RawPadding > Header.Size &&
				Header.RawPadding < (ulong)Raw.Length
			) lock( this ) {
				Raw.Position = (long)Header.RawPadding;
				Raw.CopyTo(meta);
			}
			return meta;
		}

		//...........................................................


		/// <summary>
		/// Get|set cached instance.
		/// Should only be called on items that are to be modified
		/// as it assumes mbin uses linked libMBIN.
		/// </summary>
		public object ModObject( Log LOG = null )
		{
			if( m_mod_object == null ) {
				lock( this ) if( m_mod_object == null ) {
					m_mod_object = RawObject(LOG);
				}
			}
			return m_mod_object;
		}
		public void ModObject( object OBJECT, Log LOG = null )
		{
			lock( this ) m_mod_object = OBJECT;
		}

		public AS_T ModObjectAs<AS_T>( Log LOG = null )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return ModObject(LOG) as AS_T;
		}

		//...........................................................

		/// <summary>
		/// ModMeta data is added to the end of the file.
		/// It is a distinct (memory) stream from Raw.
		/// </summary>
		public Stream ModMeta( Log LOG = null )
		{
			if( m_mod_meta == null ) {
				lock( this ) if( m_mod_meta == null ) {
					m_mod_meta = RawMeta(LOG);
				}
			}
			return m_mod_meta;
		}
		public void ModMeta( Stream STREAM, Log LOG = null )
		{
			lock( this ) m_mod_meta = STREAM;
		}

		//...........................................................

		/// <summary>
		/// Write any changes to Data back to Raw.
		/// </summary>
		public override bool Save( Log LOG = null )
		{
			var mod_object  = ModObject(LOG);  // may be cached
			if( mod_object == null ) return false;

			var raw_object = RawObject(LOG);  // always gets new from Raw
			var raw_bytes  = Mbinc.NMSTemplateToBytes(raw_object);  // convert current Raw to byte[]
			var mod_bytes  = Mbinc.NMSTemplateToBytes(mod_object);  // convert any cached object to byte[]
			if( raw_bytes == null || mod_bytes == null ) return false;

			var is_edited = raw_bytes.Length != mod_bytes.Length ?
				true : PInvoke.memcmp(raw_bytes, mod_bytes, mod_bytes.Length) != 0
			;

			// once file data flagged as IsEdited, keep flagged as IsEdited
			if(  is_edited ) IsEdited = true;
			if( !is_edited ) return true;  // nothing to update this Save

			lock( this ) {
				var new_raw  = Mbinc?.NMSTemplateToRaw(mod_object, LOG);
				if( new_raw == null ) return false;

				Raw.Position = 0;
				Raw.SetLength(0);

				Header.Format = IsGameReplacement ? HeaderFormat.V0 : HeaderFormat.V2;
				Header.SaveTo(Raw, LOG);  // write header
				new_raw.CopyTo(Raw);      // append data after header

				// Header.Padding is only set when we parse raw
				// or set header Revision, so safe to test for 0,
				// if 0 then unused i.e. not a TkAnim or TkGeom class.
				var mod_meta  = ModMeta(LOG);
				if( mod_meta == null || mod_meta.Length < 1 || Header.RawPadding != 0 ) return true;

				var meta_offset = Raw.Position;  // end of header + data
				mod_meta.CopyTo(Raw);  // append mod_meta after header and data

				// update mod_meta offset in header
				var writer = new BinaryWriter(Raw, Encoding.ASCII);
				Raw.Position = Header.Size - 8;
				writer.Write(meta_offset);

				Raw.Position = 0;
				return base.Save(LOG);
			}
		}

		//...........................................................

		/// <summary>
		/// Clone Raw mbin.
		/// </summary>
		public NMS.PAK.MBIN.Data Clone( string PATH, Log LOG = null )
		{
			var raw = new MemoryStream();

			lock( this ) {
				Raw.Position = 0;
				Raw.CopyTo(raw);
			}

			var clone = new NMS.PAK.MBIN.Data(PATH, raw, LOG);
			clone.IsGameReplacement = false;
			clone.IsEdited          = true;

			return clone;
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to EBIN.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> ebin.
		/// </summary>
		public string RawEBIN( Log LOG = null )
		{
			return Mbinc?.MbinToEbin(Path, RawObject(LOG), LOG) ?? "";
		}

		public string ModEBIN( Log LOG = null )
		{
			return Mbinc?.MbinToEbin(Path, ModObject(LOG), LOG) ?? "";
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to new instance of EXML.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> exml.
		/// </summary>
		public string RawEXML( Log LOG = null )
		{
			return Mbinc?.NMSTemplateToExml(RawObject(LOG), LOG) ?? "";
		}

		public string ModEXML( Log LOG = null )
		{
			return Mbinc?.NMSTemplateToExml(ModObject(LOG), LOG) ?? "";
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
			     if( PATH.EndsWith(".EBIN") ) System.IO.File.WriteAllText(PATH, RawEBIN(LOG));
			else if( PATH.EndsWith(".EXML") ) System.IO.File.WriteAllText(PATH, RawEXML(LOG));
			else                              base.SaveFileTo(PATH, LOG);
		}
	}
}

//=============================================================================
