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

namespace cmk.NMS.PAK.MBIN
{
	public enum HeaderFormat
	{
		V0, V1, V2
	}

	//=========================================================================

	/// <summary>
	/// Little-endian.
	/// </summary>
	public class Header
	{
		public const int Size = 0x60;  // 96 bytes

		public const ulong  MagicMbinc  = 0x726576434e49424d;  // v1, "revCNIBM" ("MBINCver")
		public const uint   MagicMbin   = 0xcccccccc;          // *.MBIN
		public const uint   MagicMbinPc = 0xdddddddd;          // *.MBIN.PC (TkGeometryData and TkGeometryStreamData)
		public const ushort FormatMbin  = 2500;                // always

		public const ulong TkAnimMetadataTag     = 0xffffffffffffffff;  // v1, TkAnimMetadata
		public const ulong TkAnimMetadataVersion = 0x3cd7d2192958ba6c;  // v1, TkAnimMetadata
		public const ulong TkAnimMetadataPadding = 0xfefefefefefefefe;  // v1, TkAnimMetadata
		public const ulong TkGeometryDataTag     = 0xffffffffffffffff;  // v2, TkGeometryData and TkGeometryStreamData
		public const ulong TkGeometryDataPadding = 0xfefefefefefefefe;  // v2, TkGeometryData and TkGeometryStreamData

		// METADATA/INPUTTEST.MBIN Timestamp = 201405311442
		public static readonly DateTime InputTestDate = new(2014, 05, 31);

		//...........................................................

		public Header( Stream RAW, Log LOG = null )
		{
			if( RAW == null || RAW.Length < Size ) return;

			RAW.Position = 0;
			var reader   = new BinaryReader(RAW, Encoding.ASCII);

			RawMagic      = reader.ReadUInt32();
			RawFormatMbin = reader.ReadUInt16();
			RawFormatLib  = reader.ReadUInt16();
			RawTimestamp  = reader.ReadUInt64();
			RawGuid       = reader.ReadUInt64();
			RawName       = reader.ReadBytes(64);
			RawPadding    = reader.ReadUInt64();

			if( (RawMagic != MagicMbin && RawMagic != MagicMbinPc)
				|| RawFormatMbin != FormatMbin
			)	return;  // not a valid mbin header e.g. METADATA/INPUTTEST.MBIN

			if( RawFormatLib == 0 ) {
				if( RawTimestamp != MagicMbinc ) { // v0, game file
					m_format  = HeaderFormat.V0;
					ClassGuid = RawGuid;

					// start optimistic, first see if linked mbinc matches.
					var mbinc = NMS.MBINC.Linked;
					var guid  = mbinc.FindClass(ClassName)?.NMSAttributeGUID ?? 0;
					if( guid == RawGuid ) Version = mbinc.Version;

					#if false  // game has mbin's w/ invalid guid's
					// go back through each game release until find a mbinc w/ matching guid.
					// this would work, except they have sometimes have a mbin that
					// has an invalid RawGuid (doesn't match ANY mbinc).
					else if( !GitHub.Disabled ){
						foreach( var release in NMS.Game.Releases.List ) {
							mbinc = NMS.MBINC.LoadRelease(release);
							guid  = mbinc?.FindClass(ClassName)?.NMSAttributeGUID ?? 0;
							if( guid == RawGuid ) {
								Version = mbinc.Version;
								break;
							}
						}
					}
					#endif
					#if false  // not reliable enough
					// some game mbin's have older date but newer guid.
					// before using version based on date make sure
					// the date can get us a mbinc w/ matching guid.
					// a more reliable way would be to walk back through
					// known game releases and check the guid from each
					// mbinc until we find a match.
					var date = Date;
					if( date >= InputTestDate && date < DateTime.Now ) {
						var version = NMS.Game.Releases.FindBuilt(date).MbincVersion;
						mbinc = MBINC.LoadMbincVersion(version);
						guid  = mbinc.FindClass(ClassName)?.NMSAttributeGUID ?? 0;
						if( guid == RawGuid ) Version = version;
					}
					#endif
				}
				else { // v1, RawTimestamp == MagicMbinc, RawGuid contains MBINC version #.#.#.#
					m_format    = HeaderFormat.V1;
					var guid    = Encoding.ASCII.GetString(BitConverter.GetBytes(RawGuid));
					var version = new System.Version();
					if( System.Version.TryParse(guid, out version) ) {
						Version = version;
					}
				}
			}
			else if( RawFormatLib == 2 ) { // v2
				m_format   = HeaderFormat.V2;
				ClassGuid  = RawGuid;
				var ver_u4 = (uint)(RawTimestamp >> 32);
				Version = new(
					(int)(ver_u4 >>  0) & 0xff,
					(int)(ver_u4 >>  8) & 0xff,
					(int)(ver_u4 >> 16) & 0xff,
					(int)(ver_u4 >> 24) & 0xff
				);
				Version.Normalize();
			}

			if( Version == null ) {
				Version  = NMS.MBINC.Linked.Version;
			}		 
			if( ClassGuid == 0 ) {
				var mbinc  = MBINC.LoadMbincVersion(Version);
				ClassGuid  = mbinc.FindClass(ClassName)?.NMSAttributeGUID ?? 0;
			}
		}

		//...........................................................

		public Header( string CLASS_NAME, Version MBINC = null, HeaderFormat FORMAT = HeaderFormat.V2, Log LOG = null )
		{
			Version   = MBINC.IsNullOrZero() ? NMS.MBINC.Linked.Version : MBINC.Normalize();
			ClassName = CLASS_NAME;
			Format    = FORMAT;
		}

		//...........................................................
		// raw header data, readonly as interpretation depend on header Format
		//...........................................................

		public uint   RawMagic      { get; protected set; } = MagicMbin;     //  4
		public ushort RawFormatMbin { get; protected set; } = FormatMbin;    //  2, always 2500
		public ushort RawFormatLib  { get; protected set; } = 0;             //  2, 0 for v0, v1, else v# e.g. 2 for v2
		public ulong  RawTimestamp  { get; protected set; } = 0;             //  8
		public ulong  RawGuid       { get; protected set; } = 0;             //  8
		public byte[] RawName       { get; protected set; } = new byte[64];  // 64, top-level class name, usually w/ 'c' prepended
		public ulong  RawPadding    { get; protected set; } = 0;             //  8, end padding, hijack for any meta-data offset
																	      // 96 bytes (0x60)
		//...........................................................
		// conversions for raw header data
		//...........................................................

		// V0 (game), V1 (old mbinc), V2 (new mbinc)
		// Relies on ClassName, possibly ClassGuid (set by ClassName) being set.
		protected HeaderFormat m_format;
		public    HeaderFormat   Format {
			get { return m_format; }
			set {
				if( m_format == value ) return;
				m_format = value;
				switch( m_format ) {
					case HeaderFormat.V0: SetV0(); break;
					case HeaderFormat.V1: SetV1(); break;
					case HeaderFormat.V2: SetV2(); break;
				}
			}
		}

		// MBINC version #.#.#.#
		public System.Version Version { get; protected set; }

		//...........................................................

		public DateTime Date { // from Timestamp, not always set
			get {
				var time_str = RawTimestamp.ToString();  // YYYYMMDDhhmm
				if( time_str.Length != 12 ) return DateTime.MinValue;
				var date = new DateTime(
					int.Parse(time_str.Substring( 0, 4)),  // year
					int.Parse(time_str.Substring( 4, 2)),  // month
					int.Parse(time_str.Substring( 6, 2)),  // day
					int.Parse(time_str.Substring( 8, 2)),  // hour
					int.Parse(time_str.Substring(10, 2)),  // min
					0
				);
				if( date < InputTestDate || date >= DateTime.Now ) return DateTime.MinValue;
				return date;
			}
		}

		//...........................................................

		public string ClassName {
			get => Encoding.ASCII.GetString(RawName).TrimStart('c').TrimEnd('\0');
			set {
				RawName = value.IsNullOrEmpty() ? null :
					Encoding.ASCII.GetBytes('c' + value.PadRight(63, '\0').Substring(0, 63))
				;
				if( value == "TkAnimMetadata" ) {
					ClassGuid = TkAnimMetadataVersion;
				}
				else {
					var mbinc  = MBINC.LoadMbincVersion(Version);
					if( mbinc != null ) ClassGuid = mbinc.FindClass(value)?.NMSAttributeGUID ?? 0;
				}
			}
		}

		public ulong ClassGuid { get; protected set; } = 0;

		//...........................................................

		/// <summary>
		/// Write 0x60 header bytes to RAW starting at current position
		/// using current header values.  RAW.Postition updated to end of header.
		/// </summary>
		public bool SaveTo( Stream RAW, Log LOG = null )
		{
			if( RAW == null ) return false;

			var writer = new BinaryWriter(RAW, Encoding.ASCII);
			writer.Write(RawMagic);
			writer.Write(RawFormatMbin);
			writer.Write(RawFormatLib);
			writer.Write(RawTimestamp);
			writer.Write(RawGuid);
			writer.Write(RawName);
			writer.Write(RawPadding);

			return true;
		}

		//...........................................................

		// Game:
		// Magic      = MagicMbin | MagicMbinPc
		// FormatLib  = 0
		// FormatMbin = MbinFormat (2500)
		// Timestamp  = 0, TkAnimMetadataTag, or TkGeometryDataTag
		// ClassGuid  = class NMSAttribute.GUID, or TkAnimMetadataVersion
		// ClassNameC = 'c' + class name w/o namespace
		// Padding    = 0, TkAnimMetadataPadding, or TkGeometryDataPadding, or MetaOffset (mod only)
		protected void SetV0()
		{
			var name    = ClassName;
			var is_anim = name == "TkAnimMetadata";
			var is_geom = name == "TkGeometryData" || name == "TkGeometryStreamData";

			RawMagic = is_geom ? MagicMbinPc : MagicMbin;  // .MBIN.PC or .MBIN

			RawFormatLib  = 0;
			RawFormatMbin = FormatMbin;
			RawGuid       = ClassGuid;

			if( is_anim ) {
				RawTimestamp = TkAnimMetadataTag;
				RawPadding   = TkAnimMetadataPadding;
			}
			else if( is_geom ) {
				RawTimestamp = TkGeometryDataTag;
				RawPadding   = TkGeometryDataPadding;
			}
			else {
				RawTimestamp = 0;
				RawPadding   = 0;
			}
		}

		//...........................................................

		// Old MBINC:
		// Timestamp = MBINCver
		// ClassGuid = MBINC Assembly version #.#.# (pad right w/ 0)
		protected void SetV1()
		{
			SetV0();
			RawTimestamp = MagicMbinc;
			RawGuid      = BitConverter.ToUInt64(
				Encoding.ASCII.GetBytes(Version.ToString(3).PadRight(8, '\0'))
			);
		}

		//...........................................................

		// Current MBINC:
		// FormatLib = 2
		// Timestamp = MBINC Assembly version #.#.#.# and supported game release #.#.#
		protected void SetV2()
		{
			SetV0();
			RawFormatLib = 2;
			RawTimestamp = BitConverter.ToUInt64(new byte[] {
				// little-endian
				(byte)Version.Major,  // lsb
				(byte)Version.Minor,
				(byte)Version.Build,
				0,
				(byte)Version.Major,
				(byte)Version.Minor,
				(byte)Version.Build,
				(byte)Version.Revision,  // msb
			});
		}
	}
}

//=============================================================================
