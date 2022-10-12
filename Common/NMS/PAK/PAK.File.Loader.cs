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
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using cmk.NMS.PAK.Item;

//=============================================================================

namespace cmk.NMS.PAK.File
{
	/// <summary>
	/// Read a .pak file from disk and load manifest into memory.
	/// Does not keep file open, re-opens file for each Extract request.
	/// 
	/// Note: pak files are PSARC files, store data in big-endian format.
	/// See: 'https://www.psdevwiki.com/ps3/PlayStation_archive_(PSARC)'
	/// 
	/// InfoList and InfoTree should be treated as immutable after construction
	/// in order for thread-safe reads.
	/// </summary>
	public partial class Loader
	: cmk.IO.File
	, cmk.NMS.PAK.Item.INamedCollection
	, System.IComparable<NMS.PAK.File.Loader>
	{
		protected static ulong s_instance = 0;

		//...........................................................

		public Loader()
		: base(null)
		{
		}

		/// <summary>
		/// PATH must be valid, it cannot be changed after construction.
		/// </summary>
		/// <param name="PATH">Full path of pak file.</param>
		public Loader( string PATH, Log LOG = null, CancellationToken CANCEL = default )
		: base(PATH)
		{
			// SaveFileDialog will create then delete empty file,
			// we may try to construct after deleted,
			// info.Length will throw and caller can test Length < 1.
			try {
				var info  = new System.IO.FileInfo(Path);
				Length    = (ulong)info.Length;
				LastWrite = info.LastWriteTimeUtc;
				Load(LOG, CANCEL);  // only time Load is called
			}
			catch {}
		}

		//...........................................................

		public readonly ulong    Instance = Interlocked.Increment(ref s_instance);
		public readonly ulong    Length;
		public readonly DateTime LastWrite;

		public const uint HeaderMagic      = 0x50534152;  // PSAR - PlayStation ARchive
		public const uint CompressTypeZLIB = 0x7a6c6962;  // [z,l,i,b] big-endian (NMS)
		public const uint CompressTypeLZMA = 0x6c7a6d61;  // [l,z,m,a] big-endian

		//...........................................................

		// LoadHeader
		public uint CompressType  { get; protected set; } = CompressTypeZLIB;  // "zlib" (NMS) or "lzma"
		public int  TocLength     { get; protected set; } = 0;  // includes 32 byte header + TOC list + block size list
		public int  TocEntrySize  { get; protected set; } = 0;  // default is 30 bytes (NMS) - 16 bytes MD5 hash, 4 bytes index of 1st block, 5 bytes entry length, 5 bytes offset of of 1st block in .pak
		public int  TocEntryCount { get; protected set; } = 0;  // the manifest is always included as the first entry, it has no id or path
		public int  BlockSize     { get; protected set; } = 0;  // size of uncompressed block, compressed blocks are this size or smaller, default is 65536 bytes (NMS) 
		public uint ArchiveFlags  { get; protected set; } = 0;  // 0 = relative paths (default), 1 = ignorecase (NMS), 2 = absolute

		// LoadBlocks
		public int    BlockCount { get; protected set; } = 0;     // Blocks[BlockCount]
		public long[] Blocks     { get; protected set; } = null;  // block length table, each entry contains compressed size of a block

		public NMS.PAK.Item.Info Manifest { get; protected set; } = null;

		//...........................................................

		/// <summary>
		/// List of meta-data for the contained items.
		/// We need the list because there are 2 parts to parse when reading a .pak file,
		/// the toc header which gives everything except the names of the files,
		/// then later get get the names.  We need the names to build the tree.
		/// </summary>
		public readonly List<NMS.PAK.Item.Info> InfoList = new();

		/// <summary>
		/// Tree of meta-data for the contained items.
		/// </summary>
		public readonly NMS.PAK.Item.Info.Node InfoTree = new();

		// INamedCollection:
		public string                  PakItemCollectionName => Path.Name;
		public NMS.PAK.Item.Info.Node  PakItemCollectionTree => InfoTree;

		//...........................................................

		// if InPCBANKS return "GAMEDATA\\PCBANKS\\"
		// if InMODS    return "GAMEDATA\\PCBANKS\\MODS\\"
		//              return Path.Full
		public string SubPath
		{
			get {
				if( InPCBANKS ) return NMS.Game.Location.Data.SubFolderPCBANKS;
				if( InMODS )    return NMS.Game.Location.Data.SubFolderMODS;
				return Path.Full;
			}
		}

		public cmk.IO.Path PathRelativeToApp => Path.RelativeTo(Resource.AppDirectory); 

		public bool InPCBANKS => NMS.Game.Location.Data.IsPCBANKS(Path);
		public bool InMODS    => NMS.Game.Location.Data.IsMODS(Path);

		//...........................................................

		protected bool Load( Log LOG = null, CancellationToken CANCEL = default )
		{
			if( Path.Name == "x" ) {
			}
			try {
				using( var pak = WaitOpenSharedReadOnly(default, CANCEL) ) {
					if( pak == null ) {
						LOG.AddFailure($"{Path.NameExt} - Failed to open");
						return false;
					}
					ReadHeader  (pak);
					ReadTOC     (pak);  // builds InfoList w/o paths
					ReadBlocks  (pak);
					ReadManifest(pak);  // assigns InfoList[i].Path, adds InfoList items to InfoTree
					Parallel.Invoke(
						() => BuildTree(),          // sets info.TreeNode
						() => ReadMbinHeaders(pak)  // sets info.MbinHeader
					);
					return true;
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{Path} - ");
				return false;
			}
		}

		//...........................................................

		public void ClearEbinCache()
		{
			InfoList.ForEach(INFO => {
				lock( INFO ) INFO.EbinCache = null;
			});
		}

		//...........................................................

		/// <summary>
		/// Read first 32 bytes of .pak to get meta-data needed to parse rest of file.
		/// </summary>
		protected void ReadHeader( Stream PAK )
		{
			PAK.Position = 0;
			var reader   = new EndianBinaryReader(PAK, Endian.Big);

			// file identifier
			var magic  = reader.ReadUInt32();
			if( magic != HeaderMagic ) {  // PSAR - PlayStation ARchive
				throw new FileFormatException($"{Path.NameExt} - Not a pak file, starts with 0x{magic:x8} not 0x{HeaderMagic:x8}");
			}

			// file version
			var ver_maj  = reader.ReadUInt16();  // NMS: 1
			var ver_min  = reader.ReadUInt16();  // NMS: 4
			if( ver_maj != 1 || (InPCBANKS && ver_min != 4) ) {
				// unexpected version, have found mods w/ 1.3
				throw new FileFormatException($"{Path.NameExt} - Invalid file version {ver_maj}.{ver_min}, should be 1.3 or 1.4");
			}

			// compression method
			CompressType = reader.ReadUInt32();
			if( CompressType != 0 &&
				CompressType != CompressTypeZLIB &&
				CompressType != CompressTypeLZMA
			){	// unexpected compression type
				throw new FileFormatException($"{Path.NameExt} - Unexpected compression 0x{CompressType:x8}");
			}

			// table of contents info
			TocLength     = reader.ReadInt32();
			TocEntrySize  = reader.ReadInt32();
			TocEntryCount = reader.ReadInt32() - 1;  // don't include Manifest
			BlockSize     = reader.ReadInt32();
			ArchiveFlags  = reader.ReadUInt32();

			// 30 = md5 hash (16 bytes) + index (4 bytes) + length (5 bytes) + offset (5 bytes)
			if( TocEntrySize != 30 ) {
				throw new FileFormatException($"{Path.NameExt} - Invalid TOC entry size {TocEntrySize}, expected 30");
			}

			InfoList.Capacity = TocEntryCount;
		}

		//...........................................................

		/// <summary>
		/// Read table of contents block from .pak.
		/// For each entry this contains: starting offset, block index, and uncompressed size, but not path.
		/// </summary>
		protected void ReadTOC( Stream PAK )
		{
			PAK.Position = 0x20;  // skip 32 byte header
			var reader   = new EndianBinaryReader(PAK, Endian.Big);

			{   // manifest always first item
				var hash   = reader.ReadBytes(16);  // ignore, md5 hash of item path
				var index  = reader.ReadUInt32();
				var length = reader.ReadUInt40();
				var offset = reader.ReadUInt40();
				Manifest   = new(this, 0, index, (long)offset, (long)length);
			}

			for( var entry = 0; entry++ < TocEntryCount; ) {
				var hash   = reader.ReadBytes(16);
				var index  = reader.ReadUInt32();
				var length = reader.ReadUInt40();
				var offset = reader.ReadUInt40();
				InfoList.Add(new(this, entry, index, (long)offset, (long)length));
			}

			if( InfoList.Count != TocEntryCount ) {
				throw new FileFormatException($"{Path.NameExt} - Invalid TOC entry count {InfoList.Count}, expected {TocEntryCount}");
			}
		}

		//...........................................................

		/// <summary>
		/// Read compressed-block-size list from .pak.
		/// </summary>
		protected void ReadBlocks( Stream PAK )
		{
			// // skip header and toc, 1+ as TocEntryCount doesn't include manifest
			PAK.Position = 0x20 + ((1 + TocEntryCount) * TocEntrySize);
			var reader   = new EndianBinaryReader(PAK, Endian.Big);

			// calc # of bytes needed to represent an entry block size, will be 2, 3, or 4 bytes.
			// e.g. if m_block_size is <= 65536 then only need 2 bytes to represent entry block size.
			byte block_bytes =   1;
			var  accum       = 256;
			do {
				++block_bytes;
				accum *= 256;
			}	while( accum < BlockSize );

			// m_toc_length includes header, toc and block size list.
			// if we subtract headers and toc lengths then remainder is block size list.
			BlockCount = (TocLength - (int)PAK.Position) / block_bytes;
			Blocks     = new long [BlockCount];

			switch( block_bytes ) {
				case 2: {
					for( int i = 0, offset = 0; i < BlockCount; i++, offset += block_bytes ) {
						Blocks[i] = reader.ReadUInt16();
					}
					break;
				}
				case 3: {
					for( int i = 0, offset = 0; i < BlockCount; i++, offset += block_bytes ) {
						Blocks[i] = reader.ReadUInt24();
					}
					break;
				}
				case 4: {
					for( int i = 0, offset = 0; i < BlockCount; i++, offset += block_bytes ) {
						Blocks[i] = reader.ReadUInt32();
					}
					break;
				}
			}

			if( BlockCount < 1 ) {
				throw new FileFormatException($"{Path.NameExt} - No block sizes");  
			}
		}

		//...........................................................

		/// <summary>
		/// The maifest (list of contained file paths) is always the first entry.
		/// It has no Id or path.  It is compressed like all other contained entries.
		/// The uncompressed data is a big array of characters.
		/// Each path, other than the last, is terminated by '\n'.
		/// The last path is terminated by the end of the blob.
		/// A general solution would treat these char as UTF8,
		/// for NMS we assume ASCII to simplfy the code.
		/// </summary>
		protected void ReadManifest( Stream PAK )
		{
			var manifest  = Extract(Manifest, PAK, null);
			if( manifest == null ) {
				throw new FileFormatException($"{Path.NameExt} - Unable to extract manifest");  
			}

			var chars = new char [1024];  // no path should exceed this size

			manifest.Position = 0;
			for( var entry = 0; entry < InfoList.Count; ++entry ) {
				var length = 0;
				var data   = manifest.ReadByte();  // data == -1 at end of stream
				while( data >= 0 && data != '\n' ) {
					chars[length++] = (char)data;
					data = manifest.ReadByte();
				}
				InfoList[entry].Path.Full = new(chars, 0, length);  // will Normalize
			}

			InfoList.Sort();  // PAK.Item.Info compare Path which compare Full using string.Compare
		}

		//...........................................................

		protected void BuildTree()
		{
			for( var index = 0; index < InfoList.Count; ++index ) {
				// builds sub-branches as needed in order to add m_info_list[index] leaf.
				// todo: recursive insert, could be faster if we split Path here.
				InfoList[index].TreeNode = InfoTree.Insert(InfoList[index].Path.Full, InfoList[index]);
			}
		}

		//...........................................................

		/// <summary>
		/// Decompress the first block of each mbin to get the
		/// top-level libMBIN class name.
		/// </summary>
		protected void ReadMbinHeaders( Stream PAK )
		{
			_ = Parallel.ForEach(InfoList, INFO => {
				var ext  = INFO.Path.Extension;
				if( ext != ".MBIN" &&
					ext != ".PC"   // all .PC are .MBIN.PC
				)	return;

				var uncompressed_length  = (int)Math.Min(INFO.Length, BlockSize);
				var   compressed_length  = (int)Blocks[INFO.Index];
				if(   compressed_length == 0 ) compressed_length = uncompressed_length;  // full uncompressed

				var   compressed_bytes  = new byte [  compressed_length];
				var uncompressed_bytes  = new byte [uncompressed_length];
				var uncompressed_stream = new MemoryStream(uncompressed_bytes);
				var read_length         = 0;

				lock( PAK ) {
					PAK.Position = INFO.Offset;
					read_length  = PAK.Read(compressed_bytes, 0, compressed_length);
				}
				if( read_length != compressed_length ) return;  // error, corrupt data

				var uncompressed_position = uncompressed_stream.Position;

				if( compressed_length != uncompressed_length ) {				
					try {  // think it's a compressed block, try to inflate
						var compressed_stream = new MemoryStream (compressed_bytes, 0, compressed_length);
						using( var decompressor = new ZLibStream(compressed_stream, CompressionMode.Decompress) ) {
							decompressor.CopyTo(uncompressed_stream);
						}
					}
					catch { return; }
				}
				if( uncompressed_position == uncompressed_stream.Position ) {
					// didn't inflate, just copy block assuming it's uncompressed
					uncompressed_stream.Write(compressed_bytes, 0, compressed_length);
				}

				INFO.MbinHeader = new NMS.PAK.MBIN.Header(uncompressed_stream);
			});
		}

		//...........................................................

		/// <summary>
		/// Get an uncompressed blob using the meta-data in INFO.
		/// INFO must be from this.InfoList.
		/// </summary>
		/// <returns>
		/// Null on error, else a MemoryStream or auto-delete temp FileStream.
		/// </returns>
		public Stream Extract( NMS.PAK.Item.Info INFO, Log LOG = null, CancellationToken CANCEL = default )
		{
			if( INFO == null ) return null;

			var info_file_path = INFO.FilePath.RelativeTo(Resource.AppDirectory).Full;
			if( info_file_path.IsNullOrEmpty() ) info_file_path = "memory";

			if( INFO.File != this ) {
				if( INFO.FilePath == Path ) LOG.AddFailure($"{Path.NameExt} {INFO.Path} - Can't extract, info not from same instance of {GetType().FullName}");
				else                        LOG.AddFailure($"{Path.NameExt} {INFO.Path} - Can't extract, info from {info_file_path}");
				return null;
			}

			using( var pak = WaitOpenSharedReadOnly(default, CANCEL) ) {
				if( pak == null ) {
					LOG.AddFailure($"{Path.NameExt} {INFO.Path} - Failed to open pak file");
					return null;
				}
				return Extract(INFO, pak, LOG);
			}
		}

		protected Stream Extract( NMS.PAK.Item.Info INFO, Stream PAK, Log LOG )
		{
			var compressed_offset = INFO.Offset;  // all compressed blocks for info start at INFO.Offset and are contiguous
			var compressed_bytes  = new byte [BlockSize];
			var raw               = cmk.IO.Stream.MemoryOrTempFile(INFO.Length, BlockSize);

			for( var block_index = INFO.Index; raw.Length < INFO.Length; ++block_index ) {
				var  remaining_length  = INFO.Length - raw.Length;
				var compressed_length  = (int)Blocks[block_index];
				if( compressed_length == 0 ) compressed_length = (int)Math.Min(remaining_length, BlockSize);

				var read_length = 0;
				lock( PAK ) {
					PAK.Position = compressed_offset;
					read_length  = PAK.Read(compressed_bytes, 0, compressed_length);
				}
				if( read_length != compressed_length ) {
					LOG.AddFailure($"{Path.NameExt} - Could only read {read_length} of {compressed_length} bytes for block[{block_index}]");
					return null;  // all or nothing
				}
				compressed_offset += read_length;  // next compressed block

				var raw_position = raw.Position;

				if( Blocks[block_index] != 0 &&
					compressed_length   != remaining_length
				){
					try {
						var compressed_stream  = new MemoryStream (compressed_bytes, 0, compressed_length);
						using( var decompressor = new ZLibStream(compressed_stream, CompressionMode.Decompress) ) {
							decompressor.CopyTo(raw);
						}
					}
					catch( Exception EX ) {
						LOG.AddFailure(EX, $"{Path.NameExt} - Block[{block_index}] is {compressed_length} byte compressed (?) block, starts with 0x{compressed_bytes[0]:x02}, 0x{compressed_bytes[1]:x02}, will assume uncompressed");
					}
				}
				if( raw_position == raw.Position ) {
					raw.Write(compressed_bytes, 0, compressed_length);
				}
			}

			raw.Position = 0;  // most callers will assume this
			return raw;
		}

		protected static readonly Type s_mbin_data_type = typeof(NMS.PAK.MBIN.Data);

		// used by ForEachData | ForEachMbin methods to avoid having to open|close file for each item
		protected NMS.PAK.Item.Data ExtractData( NMS.PAK.Item.Info INFO, Stream PAK, Log LOG )
		{
			// Create will use the INFO.Path extension to create an instance of a registered
			// NMS.PAK.Item.Data derived class i.e. cast return to extension specific derived Data type.
			var stream  = Extract(INFO, PAK, LOG);
			if( stream == null ) return null;

			return NMS.PAK.Item.Data.Create(INFO, stream, LOG);
		}

		//...........................................................
		// cmk.NMS.PAK.Item.Interface + info versions
		//...........................................................

		/// <summary>
		/// InfoList.Bsearch(PATH).
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);

			var key = new NMS.PAK.Item.Path(PATH);
			if( key.IsNullOrEmpty() ) return null;

			return InfoList.Bsearch(key,
				(ITEM, KEY) => ITEM.CompareTo(KEY)
			);
		}

		//...........................................................

		/// <summary>
		/// Scan InfoList for all MATCHing Info.
		/// </summary>
		public List<NMS.PAK.Item.Info> FindInfo( Predicate<NMS.PAK.Item.Info> MATCH, bool SORT = true )
		{
			var list = new List<NMS.PAK.Item.Info>();
			foreach( var info in InfoList ) {
				if( MATCH(info) ) list.Add(info);
			}
			//if( SORT ) list.Sort();  - InfoList already sorted
			return list;
		}

		//...........................................................

		public List<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoStartsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoContains( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoContains(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoEndsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoRegex( string PATTERN, bool SORT = true, bool WHOLE_WORDS = false, bool CASE_SENS = true, bool PATTERN_IS_REGEX = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoRegex(PATTERN, SORT, WHOLE_WORDS, CASE_SENS, PATTERN_IS_REGEX);

		public List<NMS.PAK.Item.Info> FindInfo( Regex REGEX, bool SORT = true, bool WHOLE_WORDS = false )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfo(REGEX, SORT, WHOLE_WORDS);

		//...........................................................

		/// <summary>
		/// Extract the INFO item and wrap in an extension specific PAK.Item.Data derived object.
		/// </summary>
		/// <returns>Wraper around MemoryStrem if possible, else delete-on-close FileStream.</returns>
		public NMS.PAK.Item.Data ExtractData( NMS.PAK.Item.Info INFO, Log LOG = null, CancellationToken CANCEL = default )
		{
			if( INFO == null ) return null;

			var stream  = Extract(INFO, LOG, CANCEL);
			if( stream == null ) return null;

			return NMS.PAK.Item.Data.Create(INFO, stream, LOG);
		}

		public AS_T ExtractData<AS_T>( NMS.PAK.Item.Info INFO, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data
		{
			var data  = ExtractData(INFO, LOG, CANCEL);  // get generic NMS.PAK.Item.Data
			if( data == null ) return null;

			var data_as  = data as AS_T;
			if( data_as == null ) {
				LOG.AddFailure($"{data.Path} - Is a {data.GetType().FullName} not {typeof(AS_T).FullName}");
				return null;
			}

			if( InPCBANKS ) LOG.AddSuccess($"Extracted - {data.Path}");
			else            LOG.AddSuccess($"Extracted - {data.Path} from {Path.NameExt}");

			return data_as;
		}

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		=> ((NMS.PAK.Item.ICollection)this).DefaultExtractData(PATH, NORMALIZE, LOG, CANCEL);

		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{Path.NameExt} {PATH} - Unable to find info");
				return null;
			}
			return ExtractData<AS_T>(info, LOG, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Extract MBIN or MBIN.PC item then decompile NMSTemplate based object.
		/// Discards PAK.MBIN.Data wrapper after decompiling.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( NMS.PAK.Item.Info INFO, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			var class_name  = INFO?.MbinHeader?.ClassName ?? "null";
			if( class_name != typeof(AS_T).Name ) {
				LOG.AddFailure($"{Path.NameExt} {INFO.Path} - Is a {class_name} not {typeof(AS_T).Name}");
				return null;
			}

			var data  = ExtractData<NMS.PAK.MBIN.Data>(INFO, LOG, CANCEL);
			if( data == null ) return null;

			// data may be cached, so object may be as well
			var template  = data.ModObject();
			if( template == null ) {
				LOG.AddFailure($"{Path.NameExt} {data.Path} - Unable to decompile");
				return null;
			}

			var cast  = template as AS_T;
			if( cast == null ) {
				LOG.AddFailure($"{Path.NameExt} {data.Path} - Is a {template.GetType().FullName} not {typeof(AS_T).FullName}");
			}

			return cast;
		}

		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{Path.NameExt} {PATH} - Unable to find info");
				return null;
			}
			return ExtractMbin<AS_T>(info, LOG, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach info in InfoList call HANDLER(INFO, LOG, CANCEL).
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			_ = Parallel.ForEach(InfoList,
				new() {
					CancellationToken      = CANCEL,
					MaxDegreeOfParallelism = System.Environment.ProcessorCount,
				},
				INFO => {
					try   { HANDLER(INFO, LOG, CANCEL); }
					catch ( Exception EX ) { LOG.AddFailure(EX, $"{Path.NameExt} {INFO.Path}:\n"); }
				}
			);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach data extracted from info in InfoList call HANDLER(data, LOG, CANCEL).
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			using( var pak = WaitOpenSharedReadOnly(default, CANCEL) ) {
				_ = Parallel.ForEach(InfoList,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					INFO => {
						try {
							var data  = ExtractData(INFO, pak, LOG);
							if( data != null ) HANDLER(data, LOG, CANCEL);
						}
						catch( Exception EX ) { LOG.AddFailure(EX, $"{Path.NameExt} {INFO.Path}:\n"); }
					}
				);
			}
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach mbin data extracted from info in InfoList call HANDLER(mbin, LOG, CANCEL).
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			using( var pak = WaitOpenSharedReadOnly(default, CANCEL) ) {
				_ = Parallel.ForEach(InfoList,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					INFO => {
						if( INFO.MbinHeader == null ) return;  // not mbin
						try {
							var data = ExtractData(INFO, pak, LOG) as NMS.PAK.MBIN.Data;
							if( data?.Header != null ) HANDLER(data, LOG, CANCEL);
						}
						catch( Exception EX ) { LOG.AddFailure(EX, $"{Path.NameExt} {INFO.Path}:\n"); }
					}
				);
			}
		}

		//...........................................................

		public static int Compare( Loader LHS, Loader RHS )
		{
			if( object.ReferenceEquals(LHS, RHS) ) return 0;
			if( LHS == null ) return -1;
			if( RHS == null ) return  1;

			var c  = cmk.IO.Path.Compare(LHS.Path, RHS.Path);
			if( c != 0 ) return c;

			c = DateTime.Compare(LHS.LastWrite, RHS.LastWrite);
			if( c != 0 ) return c;

			if( LHS.Length < RHS.Length ) return -1;
			if( LHS.Length > RHS.Length ) return  1;

			return 0;
		}

		public int CompareTo( Loader RHS ) => Compare(this, RHS);

		//...........................................................

		public static bool Equals( Loader LHS, Loader RHS ) => Compare(LHS, RHS) == 0;

		public override bool Equals( object RHS )
		{
			if( RHS is Loader rhs_loader ) return Equals(this, rhs_loader);
			return Path.Equals(RHS);
		}

		//...........................................................

		public override int    GetHashCode() => base.GetHashCode();
		public override string ToString()    => Path;
	}
}

//=============================================================================
