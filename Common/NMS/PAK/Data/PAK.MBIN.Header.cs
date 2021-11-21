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
	/// <summary>
	/// Little-endian.
	/// </summary>
	public class Header
	{
        protected const uint MagicMbin   = 0xcccccccc;  // *.MBIN
        protected const uint MagicMbinPc = 0xdddddddd;  // *.MBIN.PC (TkGeometryData and TkGeometryStreamData)

        protected const ushort MbinFormat = 2500;                // always
        protected const ulong  MBINCver   = 0x726576434e49424d;  // v1, "revCNIBM" ("MBINCver")

        protected const ulong TkAnimMetadataTag     = 0xffffffffffffffff;  // v1, TkAnimMetadata
        protected const ulong TkAnimMetadataVersion = 0x3cd7d2192958ba6c;  // v1, TkAnimMetadata
        protected const ulong TkAnimMetadataPadding = 0xfefefefefefefefe;  // v1, TkAnimMetadata
        protected const ulong TkGeometryDataTag     = 0xffffffffffffffff;  // v2, TkGeometryData and TkGeometryStreamData
        protected const ulong TkGeometryDataPadding = 0xfefefefefefefefe;  // v2, TkGeometryData and TkGeometryStreamData

		// METADATA/INPUTTEST.MBIN Timestamp = 201405311442
		protected readonly DateTime InputTestDate = new(2014, 05, 31);

		//...........................................................

		public Header( NMS.Game.Data GAME, NMS.PAK.File.Loader FILE, Stream RAW )
		{
			Parse(GAME, FILE, RAW);
		}

		//...........................................................

		public Header( NMS.Game.Data GAME, string CLASS_NAME )
		{
			Version   = GAME.Mbinc.Version;
			ClassName = CLASS_NAME;
		}

		//...........................................................
		// raw header data
		//...........................................................

		public uint    Magic      { get; protected set; } = MagicMbin;   //  4
		public ushort  FormatMbin { get; protected set; } = MbinFormat;  //  2, always 2500
		public ushort  FormatLib  { get; protected set; } = 0;           //  2, 0 for v0, v1, else v# e.g. 2 for v2
		public ulong   Timestamp  { get; protected set; } = 0;           //  8
		public ulong   ClassGuid  { get; protected set; } = 0;           //  8
		public byte [] ClassNameC { get; protected set; } = null;        // 64, top-level class name, usually w/ 'c' prepended
		public ulong   Padding    { get; protected set; } = 0;           //  8, end padding, hijack for any meta-data offset
		                                                                 // 96 bytes (0x60)
		//...........................................................
		// conversions for raw header data
		//...........................................................

		public DateTime Date { // from Timestamp, not always set
			get {
				var time_str = Timestamp.ToString();  // YYYYMMDDhhmm
				if( time_str.Length != 12 ) return DateTime.MinValue;
				var date = new DateTime(
					int.Parse(time_str.Substring( 0, 4)),  // year
					int.Parse(time_str.Substring( 4, 2)),  // month
					int.Parse(time_str.Substring( 6, 2)),  // day
					int.Parse(time_str.Substring( 8, 2)),  // hour
					int.Parse(time_str.Substring(10, 2)),  // min
					0
				);
				if( date < InputTestDate || date > DateTime.Now ) return DateTime.MinValue;
				return date;
			}
		} 

		public string ClassName {
			get => Encoding.ASCII.GetString(ClassNameC).TrimStart('c').TrimEnd('\0');
			set {
				ClassNameC = value.IsNullOrEmpty() ? null :
					Encoding.ASCII.GetBytes('c' + value.PadRight(63, '\0').Substring(0, 63))
				;
			}
		}

		public long MetaOffset { get; set; } = -1;

		public System.Version Version { // MBINC version #.#.#.#
			          get;
			protected set;
		}

		//...........................................................

		/// <summary>
		/// Read header, should work for all versions.
		/// Doesn't use Mbinc, summarized from MBINC source.
		/// </summary>
		public bool Parse( NMS.Game.Data GAME, NMS.PAK.File.Loader FILE, Stream RAW )
		{
			if( RAW == null || RAW.Length < 0x60 ) return false;

			RAW.Position = 0;
			var reader   = new BinaryReader(RAW, Encoding.ASCII);

			Magic      = reader.ReadUInt32();
			FormatMbin = reader.ReadUInt16();
			FormatLib  = reader.ReadUInt16();
			Timestamp  = reader.ReadUInt64();
			ClassGuid  = reader.ReadUInt64();
			ClassNameC = reader.ReadBytes(64);
			Padding    = reader.ReadUInt64();

			if( (Magic != MagicMbin && Magic != MagicMbinPc)
				|| FormatMbin != MbinFormat
			) {
				// METADATA/INPUTTEST.MBIN:
				// FormatMbin == 0
				// ClassNameC == "rameArray"
				// Log.Default.AddFailure("Invalid mbin header.");
				return false;  // not a valid mbin header
			}

			if( FormatLib == 0 ) {
				if( Timestamp != MBINCver ) { // v0, game file
					if( FILE?.InPCBANKS ?? false ) {  // InPCBANKS
						// for game mbin's must use linked mbinc as that is what scripts will use
						// e.g. if use older mbinc we will be able to view in PAK Item viewer
						// and script will extract and decompile correctly, but when script tries
						// to cast will cast to linked mbinc type not older mbinc type
						// and result will be null.				
						if( Version.IsNull() && GAME != null ) Version = GAME.Mbinc?.Version;  // use game mbinc version (linked mbinc if modding)
					}
					else {  // !InPCBANKS, for mod mbins try and get a more accurate version 
						if( Version.IsNull() ) {
							var date = Date;
							if( date > DateTime.MinValue && date < DateTime.Now ) {
								Version = NMS.Game.Releases.FindBuilt(date).MbincVersion;
							}
						}
						// hack: headers are loaded when NMS.PAK.File.Loader loads,
						// at this point the FILE.MbinVersion isn't set.
						// NMS.PAK.File.Loader will call SetV0NullVersion
						// once all headers loaded.
						//if( Version.IsNull() && FILE != null ) Version = FILE.MbinVersion;     // FILE has found an mbin w/ a non-game version
						//if( Version.IsNull() && GAME != null ) Version = GAME.Mbinc?.Version;  // use game mbinc version (linked mbinc if modding)
					}
				}
				else { // v1, Timestamp == MBINCver, ClassGuid contains MBINC version #.#.# (i.e. game release)
					var ver_str  = Encoding.ASCII.GetString(BitConverter.GetBytes(ClassGuid));
					var ver_game = new System.Version(ver_str);
					var release  = NMS.Game.Releases.FindGameVersion(ver_game);
					Version   = release.MbincVersion;
					ClassGuid = 0;
				}
			}
			else if( FormatLib == 2 ) { // v2
				var ver_u4 = (uint)(Timestamp >> 32);
				Version = new(
					(int)(ver_u4 >>  0) & 0xff,
					(int)(ver_u4 >>  8) & 0xff,
					(int)(ver_u4 >> 16) & 0xff,
					(int)(ver_u4 >> 24) & 0xff
				);
				Version.Normalize();
			}

			MetaOffset = (long)Padding;
			if( MetaOffset < 0x60 || MetaOffset > RAW.Length ) {
				MetaOffset = RAW.Length;
			}

			return true;
		}

		//...........................................................

		public bool Save( Data DATA, int HEADER_VERSION )
		{
			if( DATA?.Raw == null ) return false;

			switch( HEADER_VERSION ) {
				case 0: SetV0(DATA); break;
				case 1: SetV1(DATA); break;
				case 2: SetV2(DATA); break;
			}

			DATA.Raw.Position = 0;
			DATA.Raw.SetLength(0);
			var writer = new BinaryWriter(DATA.Raw, Encoding.ASCII);

			writer.Write(Magic);
			writer.Write(FormatMbin);
			writer.Write(FormatLib);
			writer.Write(Timestamp);
			writer.Write(ClassGuid);
			writer.Write(ClassNameC);
			writer.Write(Padding);

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
		protected void SetV0( Data DATA )
		{
			var name    = DATA.ClassName;
			var is_anim = name == "TkAnimMetadata";
			var is_geom = name == "TkGeometryData" || name == "TkGeometryStreamData";

			Magic = is_geom ? MagicMbinPc : MagicMbin;  // .MBIN.PC or .MBIN

			FormatLib  = 0;
			FormatMbin = MbinFormat;

			     if( is_anim ) Timestamp = TkAnimMetadataTag;
			else if( is_geom ) Timestamp = TkGeometryDataTag;
			else               Timestamp = 0;

			ClassGuid = is_anim ? TkAnimMetadataVersion : DATA.ClassGuid;
			ClassName = name;

			     if( is_anim ) Padding = TkAnimMetadataPadding;
			else if( is_geom ) Padding = TkGeometryDataPadding;
			else               Padding = 0;

			if( Padding == 0 &&
				MetaOffset > 0x60 &&
				MetaOffset < DATA.Raw.Length
			)	Padding = (ulong)MetaOffset;

			Version = DATA.Mbinc.Version;
		}

		//...........................................................

		/// <summary>
		/// Hack:
		/// If Version.IsNull and FormatLib == 0 and Timestamp != MBINCver
		/// then is a V0 mbin that isn't in PCBANKS folder.
		/// NMS.PAK.File.Loader will call this after it has loaded all
		/// mbin headers so that it can first find the highest version
		/// that may be set in all the mbin headers.  If none of the
		/// mbin headers have a version assigned it will use the Game
		/// Mbinc.Version.
		/// </summary>
		public void SetV0NullVersion( Version VERSION )
		{
			if( Version.IsNull() ) Version = VERSION;
		}

		//...........................................................

		// Old MBINC:
		// Timestamp = MBINCver
		// ClassGuid = MBINC Assembly version #.#.# (pad right w/ 0)
		protected void SetV1( Data DATA )
		{
			SetV0(DATA);
			if( Timestamp != 0 ) return;  // is_anim || is_geom

			Timestamp = MBINCver;
			ClassGuid = BitConverter.ToUInt64(
				Encoding.ASCII.GetBytes(Version.ToString(3).PadRight(8, '\0'))
			);
		}

		//...........................................................

		// Current MBINC:
		// FormatLib = 2
		// Timestamp = MBINC Assembly version #.#.#.# and supported game release #.#.#
		protected void SetV2( Data DATA )
		{
			SetV0(DATA);
			if( Timestamp != 0 ) return;  // is_anim || is_geom

			FormatLib = 2;
			Timestamp = BitConverter.ToUInt64(new byte[] {
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
