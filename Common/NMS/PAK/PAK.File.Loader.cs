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
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using zlib = ICSharpCode.SharpZipLib.Zip.Compression;

//=============================================================================

namespace cmk.NMS.PAK.File
{
	/// <summary>
	/// Read a .pak file from disk and load manifest into memory.
	/// Does not keep file open, re-opens file for each Extract request.
	/// Must specify the parent collection this .pak file belongs to.
	/// 
	/// Note: .pak files store data in big-endian format.
	/// See: 'https://www.psdevwiki.com/ps3/PlayStation_archive_(PSARC)'
	/// 
	/// Not thread-safe.  A crude attempt is made to avoid simple issues
	/// using an Instance id.  Each time a given Loader instance calls load
	/// it's Instance is incremented.  Info records hold the Loader Instance
	/// at the time of their creation.  Extract will abort if the specified
	/// info is not from the Loader or the info instance != Loader instance.
	/// </summary>
	public partial class Loader
	: cmk.IO.File
	, cmk.NMS.PAK.Item.ICollection
	, System.Collections.Specialized.INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		//...........................................................

		/// <summary>
		/// Construct a new .pak file wrapper.
		/// Both PARENT and PATH must be valid in order to access
		/// the contained .pak items, they cannot be changed after construction.
		/// </summary>
		/// <param name="PARENT">Collection this .pak file belongs to.</param>
		/// <param name="PATH">Full path to .pak file.</param>
		public Loader( NMS.PAK.Files PARENT, string PATH ) : base(PATH)
		{
			if( Exists() ) {
				Parent    = PARENT;
				InPCBANKS = Path.Directory.EndsWith("\\GAMEDATA\\PCBANKS\\");
				InMODS    = Path.Directory.EndsWith("\\GAMEDATA\\PCBANKS\\MODS\\");
				SubPath   = InPCBANKS ? "GAMEDATA\\PCBANKS\\" :
				            InMODS    ? "GAMEDATA\\PCBANKS\\MODS\\" :
							Path.Directory.Substring(Game?.Location.Path.Length ?? 0)
				;
				Load();
			}
		}

		//...........................................................

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
		public int     BlockCount { get; protected set; } = 0;     // Blocks[BlockCount]
		public long [] Blocks     { get; protected set; } = null;  // block length table, each entry contains compressed size of a block

		public NMS.PAK.Item.Info Manifest { get; protected set; } = null;

		public ulong Instance { get; protected set; } = 0;  // inc each Load, stored in info objects

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

		//...........................................................

		public readonly NMS.PAK.Files Parent;
		public NMS.Game.Data          Game { get { return Parent?.Game; } }

		//...........................................................

		// one of InPCBANKS or InMODS may be true, based on file Path 

		public readonly bool InPCBANKS = false;
		public readonly bool InMODS    = false;

		public readonly string SubPath = "";

		//...........................................................

		/// <summary>
		/// For older (mod) pak's it's not always possible to get
		/// the mbin version from the header (v0 headers).
		/// ReadMbinHeaders will set MbinVersion to the first
		/// mbin version it finds.
		/// </summary>
		public System.Version MbinVersion { get; protected set; } = new();

		//...........................................................

		/// <summary>
		/// Read first 32 bytes of .pak to get meta-data needed to parse rest of file.
		/// </summary>
		protected bool ReadHeader( Stream PAK )
		{
			PAK.Position = 0;
			var reader   = new EndianBinaryReader(PAK, Endian.Big);

			// file identifier
			var magic  = reader.ReadUInt32();
			if( magic != HeaderMagic ) return false;  // PSAR - PlayStation ARchive

			// file version
			var ver_maj  = reader.ReadUInt16();  // NMS: 1
			var ver_min  = reader.ReadUInt16();  // NMS: 4
			if( ver_maj != 1 || (InPCBANKS && ver_min != 4) ) return false;  // unexpected version, have found mods w/ 1.3

			// compression method
			CompressType = reader.ReadUInt32();
			if( CompressType != 0 &&
				CompressType != CompressTypeZLIB &&
				CompressType != CompressTypeLZMA
			)	return false;  // unexpected compression type

			// table of contents info
			TocLength     = reader.ReadInt32();
			TocEntrySize  = reader.ReadInt32();
			TocEntryCount = reader.ReadInt32() - 1;  // don't include Manifest
			BlockSize     = reader.ReadInt32();
			ArchiveFlags  = reader.ReadUInt32();

			// 30 = md5 hash (16 bytes) + index (4 bytes) + length (5 bytes) + offset (5 bytes)
			if( TocEntrySize != 30 ) return false;

			InfoList.Capacity = TocEntryCount;

			return true;
		}

		//...........................................................

		/// <summary>
		/// Read table of contents block from .pak.
		/// For each entry this contains: starting offset, block index, and uncompressed size, but not path.
		/// </summary>
		protected bool ReadTOC( Stream PAK )
		{
			PAK.Position = 0x20;  // skip 32 byte header
			var reader   = new EndianBinaryReader(PAK, Endian.Big);

			{   // manifest always first item
				var hash   = reader.ReadBytes(16);  // ignore, md5 hash of item path
				var index  = reader.ReadUInt32();
				var length = reader.ReadUInt40();
				var offset = reader.ReadUInt40();
				Manifest   = new(this, Instance,
					0, index, (long)offset, (long)length
				);
			}

			for( var entry = 0; entry++ < TocEntryCount; ) {
				var hash   = reader.ReadBytes(16);
				var index  = reader.ReadUInt32();
				var length = reader.ReadUInt40();
				var offset = reader.ReadUInt40();
				InfoList.Add(new(this, Instance,
					entry, index, (long)offset, (long)length
				));
			}

			return InfoList.Count == TocEntryCount;
		}

		//...........................................................

		/// <summary>
		/// Read compressed-block-size list from .pak.
		/// </summary>
		protected bool ReadBlocks( Stream PAK )
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
			Blocks     = new long[BlockCount];

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

			return BlockCount > 1;
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
		protected bool ReadManifest( Stream PAK )
		{
			var manifest  = Extract(Manifest, PAK, null);
			if( manifest == null ) return false;

			var chars = new char [1024];  // no path should exceed this size

			// manifest is a big char array, where the name of each entry is separated by '\n'.
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
			return true;
		}

		//...........................................................

		protected void BuildTree()
		{
			for( var index = 0; index < InfoList.Count; ++index ) {
				// builds sub-branches as needed in order to add m_info_list[index] leaf
				InfoList[index].TreeNode = InfoTree.Insert(InfoList[index].Path, InfoList[index]);
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
				var ext  = INFO.Path.Extension; //.ToUpper();
				if( ext != ".MBIN" &&
					ext != ".PC"   // all .PC are .MBIN.PC
				)	return;

				var   compressed_length = (int)Blocks[INFO.Index];
				var uncompressed_length = (int)Math.Min(INFO.Length, BlockSize);
				if(   compressed_length == 0 ) compressed_length = BlockSize;

				var compressed_bytes   = new byte [compressed_length];
				var uncompressed_bytes = new byte [uncompressed_length];

				lock( PAK ) {
					PAK.Position     = INFO.Offset;
					var read_length  = PAK.Read(compressed_bytes, 0, compressed_length);
					if( read_length != compressed_length ) return;
				}

				if( compressed_length != INFO.Length ) {  // compressed
					try {
						var inflater = new zlib.Inflater();
						inflater.SetInput(compressed_bytes, 0, compressed_length);
						uncompressed_length = inflater.Inflate(uncompressed_bytes, 0, uncompressed_bytes.Length);
					}
					catch { return; }
				}
				else uncompressed_length = 0;
				if( uncompressed_length == 0 ) {
					uncompressed_length  = compressed_length;
					Buffer.BlockCopy(
						compressed_bytes,   0,
						uncompressed_bytes, 0,
						compressed_length
					);
				}

				INFO.MbinHeader = new NMS.PAK.MBIN.Header(Game, this, new MemoryStream(uncompressed_bytes));
				var info_header_version = INFO.MbinHeader.Version;

				// find highest assigned mbin version
				if( info_header_version != Game.Mbinc.Version &&
					info_header_version >  MbinVersion
				)	MbinVersion = info_header_version;
			});

			// no mbin's had a version set (or any assigned were Game.Mbinc.Version)
			if( MbinVersion.IsNull() ) MbinVersion = Game.Mbinc.Version;

			// set version for any v0 mbin headers to highest assigned mbin version
			_ = Parallel.ForEach(InfoList, INFO =>
				INFO.MbinHeader?.SetV0NullVersion(MbinVersion)
			);
		}

		//...........................................................

		/// <summary>
		/// Called when Loader is first created i.e. when first scan a pak folder,
		/// and if a pak is modified (replaced) e.g. new version of mod is created
		/// overwrites old version.
		/// </summary>
		public bool Load( Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				InfoTree.ItemsClear();
				InfoList.Clear();
				Manifest = null;

				++Instance;
				CompressType  = 0;
				TocLength     = 0;
				TocEntrySize  = 0;
				TocEntryCount = 0;
				BlockSize     = 0;
				ArchiveFlags  = 0;
				BlockCount    = 0;
				Blocks        = null;

				var ok = false;
				using( var pak = WaitOpenSharedReadOnly(int.MaxValue, CANCEL) ) {
					if( pak == null ) {
						LOG.AddFailure($"Failed to open {SubPath}");
						return false;
					}
					ok =
						ReadHeader  (pak) &&
						ReadTOC     (pak) &&  // builds InfoList w/o paths
						ReadBlocks  (pak) &&
						ReadManifest(pak)     // assigns InfoList[i].Path, adds InfoList items to InfoTree
					;
					Parallel.Invoke(
						() => BuildTree(),
						() => ReadMbinHeaders(pak)
					);
				}
				CollectionChanged?.Invoke(this,
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Reset
					)
				);
				return ok;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{SubPath}\n");
				return false;
			}
		}

		//...........................................................

		/// <summary>
		/// Get an uncompressed blob using the meta-data in ENTRY.
		/// </summary>
		/// <returns>A MemoryStream if possible, otherwise a FileStream to an auto-delete temp file.</returns>
		public Stream Extract( NMS.PAK.Item.Info INFO, Log LOG )
		{
			using( var pak = WaitOpenSharedReadOnly() ) {
				return Extract(INFO, pak, LOG);
			}
		}

		protected Stream Extract( NMS.PAK.Item.Info INFO, Stream PAK, Log LOG )
		{
			if( INFO == null || PAK == null ) return null;

			if( INFO.File != this ) {
				LOG.AddFailure($"{INFO.Path} - info not from {SubPath}");
				return null;
			}
			if( INFO.Instance != Instance ) {
				LOG.AddFailure($"{INFO.Path} - info from Instance {INFO.Instance}, now on Instance {Instance}");
				return null;
			}

			var raw = cmk.IO.Stream.MemoryOrTempFile(INFO.Length, BlockSize);

			var inflater           = new zlib.Inflater();
			var compressed_bytes   = new byte [BlockSize];
			var uncompressed_bytes = new byte [BlockSize];

			lock( PAK ) {  // lock per item not per block
				PAK.Position = INFO.Offset;

				for( var index = INFO.Index; raw.Length < INFO.Length; ++index ) {
					var remaining = INFO.Length - raw.Length;

					var compressed_length  = (int)Blocks[index];
					if( compressed_length == 0 ) compressed_length = (int)Math.Min(remaining, BlockSize);

					var read_length  = PAK.Read(compressed_bytes, 0, compressed_length);
					if( read_length != compressed_length ) {
						LOG.AddFailure($"Extract {INFO.Path} - could only read {read_length} of {compressed_length} bytes for block[{index}]");
						return null;
					}

					// check if uncompressed block
					if( Blocks[index]     == 0 ||       // full
						compressed_length == remaining  // partial (last)
					){
						// just copy uncompressed data
						raw.Write(compressed_bytes, 0, compressed_length);
					}		
					else {  // think it's a compressed block, try to inflate
						var uncompressed_length = 0;
						try {
							inflater.SetInput(compressed_bytes, 0, compressed_length);
							uncompressed_length = inflater.Inflate(uncompressed_bytes, 0, BlockSize);
							inflater.Reset();
						}
						catch( Exception EX ) {
							LOG.AddFailure(EX, $"Extract {INFO.Path} - block[{index}] is {compressed_length} byte compressed (?) block, starts with 0x{compressed_bytes[0]:x02}, 0x{compressed_bytes[1]:x02}, will assume uncompressed.\n");
						}
						if( uncompressed_length == 0 ) {
							// unable to inflate, just copy block assuming it's uncompressed
							raw.Write(compressed_bytes, 0, compressed_length);
						}
						else {
							raw.Write(uncompressed_bytes, 0, uncompressed_length);
						}
					}
				}
			}

			raw.Position = 0;  // most callers will assume this
			return raw;
		}

		//...........................................................
		// cmk.NMS.PAK.Item.Interface
		//...........................................................

		/// <summary>
		/// InfoList.Find(MATCH) - forward scan.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( Predicate<NMS.PAK.Item.Info> MATCH )
		{
			return InfoList.Find(INFO => MATCH(INFO));
		}

		//...........................................................

		/// <summary>
		/// InfoList.Find(PATH) - bsearch.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);
			return InfoList.Find(PATH, ( INFO, PATH ) =>
				INFO.Path.CompareTo(PATH)
			);
		}

		//...........................................................

		/// <summary>
		/// Scan InfoList for matching info.Path.Full.StartsWith(PATTERN).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN )
		{
			foreach( var info in InfoList ) {
				if( info.Path.Full.StartsWith(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Scan InfoList for matching info.Path.Full.Contains(PATTERN).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoContains( string PATTERN )
		{
			foreach( var info in InfoList ) {
				if( info.Path.Full.Contains(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Scan InfoList for matching info.Path.Full.EndsWith(PATTERN).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN )
		{
			foreach( var info in InfoList ) {
				if( info.Path.Full.EndsWith(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Scan InfoList for REGEX.IsMatch(info.Path).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoRegex( Regex REGEX )
		{
			if( REGEX != null ) {
				foreach( var info in InfoList ) {
					var    is_match = false;
					try  { is_match = REGEX.IsMatch(info.Path); }
					catch( Exception EX ) { Log.Default.AddFailure(EX, $"{SubPath}:\n"); }
					if( is_match ) yield return info;
				}
			}
		}

		//...........................................................

		protected NMS.PAK.Item.Data ExtractData( NMS.PAK.Item.Info INFO, Stream PAK, Log LOG )
		{
			return NMS.PAK.Item.Data.Create(INFO, Extract(INFO, PAK, LOG), LOG);
		}

		/// <summary>
		/// Extract the INFO item and wrap in a type specific PAK.Item.Data derived object.
		/// </summary>
		/// <returns>Wraper around MemoryStrem if possible, else delete-on-close FileStream.</returns>
		public NMS.PAK.Item.Data ExtractData( NMS.PAK.Item.Info INFO, Log LOG = null )
		{
			using( var pak = WaitOpenSharedReadOnly() ) {
				return ExtractData(INFO, pak, LOG);
			}
		}

		public AS_T ExtractData<AS_T>( NMS.PAK.Item.Info INFO, Log LOG = null )
		where  AS_T : NMS.PAK.Item.Data
		{
			if( INFO == null ) return null;

			var data  = ExtractData(INFO, LOG);  // get generic NMS.PAK.Item.Data
			if( data == null ) return null;

			var data_as  = data as AS_T;
			if( data_as == null ) {
				LOG.AddFailure($"{data.Path} - is a {data.GetType().FullName} not {typeof(AS_T).FullName}");
				return null;
			}

			if( InPCBANKS ) LOG.AddSuccess($"Extracted - {data.Path}");
			else            LOG.AddSuccess($"Extracted - {data.Path} from {SubPath}");

			return data_as;
		}

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null )
		{
			return ExtractData<NMS.PAK.Item.Data>(PATH, NORMALIZE, LOG);
		}

		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : NMS.PAK.Item.Data
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in {SubPath}");
				return null;
			}
			return ExtractData<AS_T>(info, LOG);
		}

		//...........................................................

		/// <summary>
		/// Extract DDS item and convert to a BitmapSource.
		/// Discards PAK.DDS.Data wrapper after conversion.
		/// </summary>
		public BitmapSource ExtractDdsBitmapSource( NMS.PAK.Item.Info INFO, int HEIGHT = 32, Log LOG = null )
		{
			if( INFO.Path.Extension.ToUpper() != ".DDS" ) {
				LOG.AddFailure($"{INFO.Path} - not a *.DDS file");
				return null;
			}

			var data  = ExtractData<NMS.PAK.DDS.Data>(INFO, LOG);
			if( data == null ) return null;

			var bitmap  = data.Dds?.GetBitmap(HEIGHT < 16 ? 256 : HEIGHT, true);
			if( bitmap == null ) {
				LOG.AddFailure($"{data.Path} - unable to convert dds to bitmap");
				return null;
			}

			return bitmap;
		}

		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null )
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in {SubPath}");
				return null;
			}
			return ExtractDdsBitmapSource(info, HEIGHT, LOG);
		}

		//...........................................................

		/// <summary>
		/// Extract MBIN or MBIN.PC item then decompile NMSTemplate based object.
		/// Discards PAK.MBIN.Data wrapper after decompiling.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( NMS.PAK.Item.Info INFO, Log LOG = null )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			var class_name  = INFO.MbinHeader.ClassName;
			if( class_name != typeof(AS_T).Name ) {
				LOG.AddFailure($"{INFO.Path} - is a {class_name} not {typeof(AS_T).Name}");
				return null;
			}

			var data  = ExtractData<NMS.PAK.MBIN.Data>(INFO, LOG);
			if( data == null ) return null;

			// since we are going to throw away the data wrapper we just extracted
			// we may as well get a new NMSTemplate based object.
			var template  = data.ExtractObject();  
			if( template == null ) {
				LOG.AddFailure($"{data.Path} - unable to decompile");
				return null;
			}

			var cast  = template as AS_T;
			if( cast == null ) {
				LOG.AddFailure($"{data.Path} - is a {template.GetType().FullName} not {typeof(AS_T).FullName}");
			}

			return cast;
		}

		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : class  // libMBIN.NMSTemplate
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in {SubPath}");
				return null;
			}
			return ExtractMbin<AS_T>(info, LOG);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach info in InfoList call HANDLER(INFO, CANCEL, LOG).
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		){
			_ = Parallel.ForEach(InfoList,
				new() {
					CancellationToken      = CANCEL,
					MaxDegreeOfParallelism = System.Environment.ProcessorCount,
				},
				INFO => {
					var priority = Thread.CurrentThread.Priority;
					Thread.CurrentThread.Priority = ThreadPriority.Lowest;
					try     { HANDLER(INFO, CANCEL, LOG); }
					catch   ( Exception EX ) { LOG.AddFailure(EX, $"{INFO.Path}:\n"); }
					finally { Thread.CurrentThread.Priority = priority; }
				}
			);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach data extracted from info in InfoList call HANDLER(data, CANCEL, LOG).
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		){
			using( var pak = WaitOpenSharedReadOnly() ) {
				_ = Parallel.ForEach(InfoList,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					INFO => {
						var priority = Thread.CurrentThread.Priority;
						Thread.CurrentThread.Priority = ThreadPriority.Lowest;
						try {
							var data  = ExtractData(INFO, pak, LOG);
							if( data != null ) HANDLER(data, CANCEL, LOG);
						}
						catch   ( Exception EX ) { LOG.AddFailure(EX, $"{INFO.Path}:\n"); }
						finally { Thread.CurrentThread.Priority = priority; }
					}
				);
			}
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach mbin data extracted from info in InfoList call HANDLER(mbin, CANCEL, LOG).
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		){
			using( var pak = WaitOpenSharedReadOnly() ) {
				_ = Parallel.ForEach(InfoList,
					new() {
						CancellationToken      = CANCEL,
						MaxDegreeOfParallelism = System.Environment.ProcessorCount,
					},
					INFO => {
						if( INFO.MbinHeader == null ) return;  // not mbin
						var priority = Thread.CurrentThread.Priority;
						Thread.CurrentThread.Priority = ThreadPriority.Lowest;
						try {
							var data  = ExtractData(INFO, pak, LOG) as NMS.PAK.MBIN.Data;
							if( data != null ) HANDLER(data, CANCEL, LOG);
						}
						catch   ( Exception EX ) { LOG.AddFailure(EX, $"{INFO.Path}:\n"); }
						finally { Thread.CurrentThread.Priority = priority; }
					}
				);
			}
		}
	}
}

//=============================================================================
