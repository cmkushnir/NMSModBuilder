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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PAK.MBIN
{
	// Replaces libMBIN.MBINFile
	public class Data
	: cmk.NMS.PAK.Item.Data
	{
		static Data()
		{
			s_extensions[".MBIN"] = new(typeof(Data), typeof(EbinViewer), typeof(EbinDiffer));;
			s_extensions[".PC"]   = new(typeof(Data), typeof(EbinViewer), typeof(EbinDiffer));;  // all .PC are .MBIN.PC

			//Viewers["GcDebugOptions"] = typeof(ExmlViewer);
		}

		public Data() : base()
		{
		}

		//...........................................................

		/// <summary>
		/// Registered Viewers for different top-level class names.
		/// </summary>
		public readonly static Dictionary<string,Type> Viewers = new();
		public readonly static Dictionary<string,Type> Differs = new();

		//...........................................................

		/// <summary>
		/// Loads header from Raw.
		/// Does not use MBINC to read header, should work on any version.
		/// </summary>
		public Data( NMS.PAK.Item.Info INFO, Stream RAW = null )
		: base(INFO, RAW)
		{
			Header = INFO?.MbinHeader ?? new Header(Game, File, Raw);
			PostConstruct();
		}

		//...........................................................

		/// <summary>
		/// Create new mbin using a given RAW MBINFile.
		/// </summary>
		public Data( Game.Data GAME, string PATH, Stream RAW = null )
		: base(GAME, PATH, RAW)
		{
			Header = new Header(Game, File, Raw);
			PostConstruct();
			IsEdited = true;
		}

		//...........................................................

		/// <summary>
		/// Create new mbin using a given RAW MBINFile.
		/// </summary>
		public Data( Game.Data GAME, string PATH, string CLASS )
		: base(GAME, PATH, null)
		{
			Header = new Header(GAME, CLASS);
			PostConstruct();
			if( Class?.Type != null ) {
				IsEdited = true;
				Object   = Activator.CreateInstance(Class.Type);
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

		protected void PostConstruct()
		{
			Mbinc = MBINC.LoadMbincVersion(Header.Version);
			if( Mbinc == null ) return;

			Class = Mbinc.FindClass(Header.ClassName);

			if( Header.ClassGuid != ClassGuid ) {
				Log.Default.AddWarning($"{Path} Header.ClassGuid {Header.ClassGuid:x16} != ClassInfo.NMSAttributeGUID {ClassGuid:x16}, using ClassInfo");
			}
		}

		//...........................................................

		/// <summary>
		/// Parse current Raw, return new instance of top-level class.
		/// </summary>
		public object ExtractObject()
		{
			if( Mbinc == null ||
				ClassName.IsNullOrEmpty() ||
				Raw == null || Raw.Length < 0x60
			)	return null;
			Raw.Position = 0x60;  // skip header
			return Mbinc.RawToNMSTemplate(Raw, ClassName);
		}

		public AS_T ExtractObjectAs<AS_T>()
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return ExtractObject() as AS_T;
		}

		//...........................................................

		/// <summary>
		/// Get new MemoryStream of meta-data tacked onto end of mbin.
		/// </summary>
		public Stream ExtractMeta()
		{
			Stream meta = new MemoryStream();
			if( Raw != null &&
				Header.MetaOffset > 0x60 &&
				Header.MetaOffset < Raw.Length
			) {
				Raw.Position = Header.MetaOffset;
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
		public object Object {
			get {
				if( m_object == null ) {
					m_object  = ExtractObject();
				}
				return m_object;
			}
			set {
				m_object = value;
				Save();
			}
		}

		public AS_T ObjectAs<AS_T>()
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			return Object as AS_T;
		}

		//...........................................................

		protected Stream m_meta;

		/// <summary>
		/// Meta data is added to the end of the file.
		/// It is a distinct (memory) stream from Raw.
		/// </summary>
		public Stream Meta {
			get {
				if( m_meta == null ) {
					m_meta  = ExtractMeta();
				}
				return m_meta;
			}
			set {
				m_meta = value;
				Save();
			}
		}

		//...........................................................

		/// <summary>
		/// Write any changes to Data back to Raw.
		/// </summary>
		public override bool Save()
		{
			// once file data flagged as edited, keep flagged as edited
			if( !IsEdited && IsMbincLinkedVersion ) {
				var old_data = ExtractObject() as libMBIN.NMSTemplate;  // convert current Raw to data
				var new_data =        Object   as libMBIN.NMSTemplate;

				// bytes starting at root NMSTemplate i.e. doesn't include header.
				var old_bytes = old_data?.SerializeBytes();
				var new_bytes = new_data?.SerializeBytes();

				IsEdited = Array_x.MemCmp(old_bytes, new_bytes) != 0;
			}
			if( IsEdited ) {
				Header.Save(this, IsGameReplacement ? 0 : 2);
				SaveData();
				SaveMeta();
			}
			return true;
		}

		//...........................................................

		protected bool SaveData()
		{
			Raw.Position = 0x60;
			var writer   = new BinaryWriter(Raw, Encoding.ASCII);

			var data = Mbinc?.NMSTemplateToRaw(Object);
			data.CopyTo(Raw);

			if( Meta != null && Meta.Length > 0 ) {
				Header.MetaOffset = Raw.Position;
				Raw.Position = 0x60 - 8;
				writer.Write(Header.MetaOffset);
			}

			return true;
		}

		//...........................................................

		protected bool SaveMeta()
		{
			if( Header.MetaOffset > 0x60 && Meta != null && Meta.Length > 0 ) {
				Raw.Position = Header.MetaOffset;
				Meta.CopyTo(Raw);
			}
			return true;
		}

		//...........................................................

		/// <summary>
		/// Clone this mbin.
		/// </summary>
		public NMS.PAK.MBIN.Data Clone( string PATH )
		{
			var raw = new MemoryStream();

			Raw.Position = 0;
			Raw.CopyTo(raw);

			var clone = new NMS.PAK.MBIN.Data(Game, PATH, raw);
			clone.IsGameReplacement = false;
			clone.IsEdited          = true;

			return clone;
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to new instance of EBIN.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> ebin.
		/// </summary>
		public string CreateEBIN()
		{
			return Mbinc?.MbinToEbin(Path, ExtractObject()) ?? "";
		}

		//...........................................................

		/// <summary>
		/// Serailzize Raw to new instance of EXML.
		/// Will use correct MbincVersion version to convert Raw -> mbin -> exml.
		/// </summary>
		public string CreateEXML()
		{
			return Mbinc?.NMSTemplateToExml(ExtractObject()) ?? "";
		}
		// we don't support converting exml back to mbin here, use Mbinc.ExmlToNMSTemplate

		//...........................................................

		/// <summary>
		/// If LHS != null then it is the game version of this mbin
		/// and we should return a new Differ, else return new Viewer.
		/// </summary>
		public override UIElement GetViewer( NMS.PAK.Item.Data LHS = null )
		{
			var lhs  = LHS as NMS.PAK.MBIN.Data;
			if( lhs == null ) {
				var viewer  = Viewers.GetValueOrDefault(Header.ClassName);
				if( viewer != null ) return Activator.CreateInstance(viewer, this) as UIElement;
			}
			else {
				var differ  = Differs.GetValueOrDefault(Header.ClassName);
				if( differ != null ) return Activator.CreateInstance(differ, lhs, this) as UIElement;
			}
			return base.GetViewer(LHS);
		}

		//...........................................................

		/// <summary>
		/// Called when saving to disk.
		/// </summary>
		protected override void SaveFilePrepare( SaveFileDialog DIALOG )
		{
			DIALOG.Filter   = "EBIN|*.EBIN|" + DIALOG.Filter;
			DIALOG.FileName = System.IO.Path.ChangeExtension(DIALOG.FileName, ".EBIN");
		}

		//...........................................................

		/// <summary>
		/// Called when saving to disk.
		/// </summary>
		protected override void SaveFileTo( string PATH )
		{
			if( PATH.EndsWith(".EBIN") ) System.IO.File.WriteAllText(PATH, CreateEBIN());
			else                         base.SaveFileTo(PATH);
		}
	}
}

//=============================================================================
