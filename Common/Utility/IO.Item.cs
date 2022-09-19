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
using System.Threading;

//=============================================================================

namespace cmk.IO
{
    /// <summary>
    /// Base class for File and Directory wrapper classes.
    /// </summary>
    public abstract class Item
	: System.IComparable<Item>
	, System.IComparable<Path>
	, System.IComparable<string>
	{
		public Item( string FULL_PATH = null )
		{
			Path = new Path(FULL_PATH);
		}

		//...........................................................

		public Item( string DIRECTORY, string NAME, string EXTENSION = null )
		{
			Path = new Path(DIRECTORY, NAME, EXTENSION);
		}

		//...........................................................

		public cmk.IO.Path Path { get; protected set; }  // never null

		//...........................................................

		public virtual bool Exists => false;

		//...........................................................

		public virtual bool EnsureExists()
		{
			return false;
		}

		//...........................................................

		public static int Compare( Item LHS, Item RHS )
		=> Path.Compare(LHS?.Path, RHS?.Path);

		public static int Compare( Item LHS, Path RHS ) => Path.Compare(LHS?.Path, RHS);
		public static int Compare( Path LHS, Item RHS ) => Path.Compare(LHS,       RHS?.Path);

		public int CompareTo( Item   RHS ) => Path.Compare(Path, RHS?.Path);
		public int CompareTo( Path   RHS ) => Path.Compare(Path, RHS);
		public int CompareTo( string RHS ) => Path.Compare(Path, RHS);

		//...........................................................

		public static bool Equals( Item LHS, Item RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( Item LHS, Path RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( Path LHS, Item RHS ) => Compare(LHS, RHS) == 0;

		public override bool Equals( object RHS )
		{
			if( RHS is Item rhs_item ) return Path.Equals(Path, rhs_item?.Path);
			return Path.Equals(RHS);
		}

		//...........................................................

		public override int GetHashCode() => Path.GetHashCode();
		public override string ToString() => Path;
	}

	//=========================================================================

	/// <summary>
	/// Wrap a directory path.
	/// </summary>
	public class Directory
	: cmk.IO.Item
	{
		public Directory( string FULL_PATH = null )
		: base(FULL_PATH)
		{
		}

		//...........................................................

		public override bool Exists => System.IO.Directory.Exists(Path);

		//...........................................................

		public override bool EnsureExists()
		{
			if( !Exists && !Path.IsNullOrEmpty() ) {
				try  { System.IO.Directory.CreateDirectory(Path); }
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
			}
			return Exists;
		}

		//...........................................................

		public IEnumerable<FileInfo> Files( string PATTERN = null )
		{
			if( Exists ) try {
				var    dir = new System.IO.DirectoryInfo(Path);
				return PATTERN.IsNullOrEmpty() ?
					dir.EnumerateFiles() :
					dir.EnumerateFiles(PATTERN)
				;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
			return new List<FileInfo>();
		}

		//...........................................................

		public IEnumerable<DirectoryInfo> Directories( string PATTERN = null )
		{
			if( Exists ) try {
				var    dir = new System.IO.DirectoryInfo(Path);
				return PATTERN.IsNullOrEmpty() ?
					dir.EnumerateDirectories() :
					dir.EnumerateDirectories(PATTERN)
				;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
			return new List<DirectoryInfo>();
		}
	}

	//=========================================================================

	/// <summary>
	/// Wrap a file path.
	/// </summary>
	public class File
	: cmk.IO.Item
	{
		public File( string FULL_PATH = null )
		: base(FULL_PATH)
		{
		}

		//...........................................................

		public File( string DIRECTORY, string NAME, string EXTENSION = null )
		: base(DIRECTORY, NAME, EXTENSION)
		{
		}

		//...........................................................

		public override bool Exists => System.IO.File.Exists(Path);

		//...........................................................

		public override bool EnsureExists()
		{
			if( !Exists && !Path.IsNullOrEmpty() ) {
				try  { System.IO.File.Create(Path); }
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
			}
			return Exists;
		}

		//...........................................................

		/// <summary>
		/// Open existing or create new for exclusive read|write.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Milliseconds.</param>
		public FileStream WaitOpen(
			FileMode          MODE,
			FileAccess        ACCESS,
			FileShare         SHARE,
			TimeSpan          TIMEOUT = default,
			CancellationToken CANCEL  = default
		){
			if( TIMEOUT == default ) TIMEOUT = Resource.DefaultTimeout;
			var end = Environment.TickCount64 +TIMEOUT.Ticks;

			do {
				FileStream fs = null;
				try {
					switch( MODE ) {
						case FileMode.CreateNew:
						case FileMode.Create:
						case FileMode.OpenOrCreate:
							var dir = new cmk.IO.Directory(Path.Directory);
							if( !dir.EnsureExists() ) {
								throw new Exception($"Could not find or create directory '{dir.Path}'");
							}
							break;
					}
					fs = new(Path.Full, MODE, ACCESS, SHARE);
					if( fs != null ) return fs;
				}
				catch( IOException ) {
					if( fs != null ) {
						fs.Dispose();
						fs = null;
					}
					// System.IO.FileNotFoundException is a type of IOException,
					// Exists will fail if no such file, or if caller doesn't have read permission.
					if( !System.IO.File.Exists(Path.Full) ) break;
					Thread.Sleep(8);  // assume open by something else
				}
				catch( Exception EX ) {
					Log.Default.AddFailure(EX);
					break;
				}
			}	while(
				Environment.TickCount64 < end &&
				!CANCEL.IsCancellationRequested
			);

			return null;
		}

		//...........................................................

		/// <summary>
		/// Open existing for shared read.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Milliseconds.</param>
		protected FileStream WaitOpenSharedReadOnly( TimeSpan TIMEOUT = default, CancellationToken CANCEL = default )
		{
			return WaitOpen(FileMode.Open, FileAccess.Read, FileShare.Read, TIMEOUT, CANCEL);
		}

		/// <summary>
		/// Open existing or create new for exclusive read|write.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Milliseconds.</param>
		public FileStream WaitOpenExclusiveReadWrite( TimeSpan TIMEOUT = default, CancellationToken CANCEL = default )
		{
			return WaitOpen(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, TIMEOUT, CANCEL);
		}

		/// <summary>
		/// Truncate existing or create new for exclusive read|write.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Milliseconds.</param>
		public FileStream WaitOpenExclusiveReadWriteTruncate( TimeSpan TIMEOUT = default, CancellationToken CANCEL = default )
		{
			return WaitOpen(FileMode.Create, FileAccess.ReadWrite, FileShare.None, TIMEOUT, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Get the embedded link date from the PE header in the PATH exe.
		/// </summary>
		/// <param name="PATH">Full path to an exe.</param>
		/// <param name="TZ">Optional timezone for returned DateTime, default is UTC.</param>
		/// <returns></returns>
		public static DateTime PEBuildDate( cmk.IO.Path PATH, TimeZoneInfo TZ = null, Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				var  file = new cmk.IO.File(PATH);
				if( !file.Exists ) return DateTime.MinValue;

				var buffer = new byte[2048];
				int length = 0;

				using( var stream = file.WaitOpenSharedReadOnly(default, CANCEL) ) {
					length = stream?.Read(buffer, 0, buffer.Length) ?? 0;
				}

				const int pe_header_offset      = 60;
				const int link_timestamp_offset =  8;

				if( length < (pe_header_offset + 4) ) return DateTime.MinValue;
				var offset = BitConverter.ToInt32(buffer, pe_header_offset) + link_timestamp_offset;

				if( length  < (offset + 4) ) return DateTime.MinValue;
				var seconds = BitConverter.ToInt32(buffer, offset);

				var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var utc   = epoch.AddSeconds(seconds);
				var tz    = TZ ?? TimeZoneInfo.Local;
				var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

				return local;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{PATH.Full}:\r\n");
				return DateTime.MinValue;
			}
		}

		//...........................................................

		public static MemoryStream ReadAllStream( cmk.IO.Path PATH, Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				var  file = new cmk.IO.File(PATH);
				if( !file.Exists ) return null;
				using( var stream = file.WaitOpenSharedReadOnly(default, CANCEL) ) {
					if( stream == null ) {
						LOG.AddFailure($"File could not be opened for loading: {file.Path.Full}");
						return null;
					}
					var memory = new MemoryStream();
					stream.Position = 0;
					stream.CopyTo(memory);
					memory.Position = 0;
					return memory;
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{PATH.Full}:\r\n");
				return null;
			}
		}

		//...........................................................

		public static string ReadAllText( cmk.IO.Path PATH, Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				var  file = new cmk.IO.File(PATH);
				if( !file.Exists ) return null;
				// return System.IO.File.ReadAllText(PATH.Full, Encoding.UTF8);  // fails if already open exclusive
				using( var stream = file.WaitOpenSharedReadOnly(default, CANCEL) ) {
					if( stream == null ) {
						LOG.AddFailure($"File could not be opened for loading: {file.Path.Full}");
						return null;
					}
					using( var reader = new StreamReader(stream, Encoding.UTF8) ) {
						return reader.ReadToEnd();
					}
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{PATH.Full}:\r\n");
				return null;
			}
		}

		//...........................................................

		public static bool WriteAllStream( cmk.IO.Path PATH, System.IO.Stream STREAM, Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				var file = new cmk.IO.File(PATH);
				if( file.Exists && STREAM == null ) {
					System.IO.File.Delete(PATH.Full);
					return true;
				}
				// System.IO.File.WriteAll*(PATH.Full, TEXT, Encoding.UTF8);  // fails if already open exclusive
				// return true;
				using( var stream = file.WaitOpenExclusiveReadWriteTruncate(default, CANCEL) ) {
					if( stream == null ) {
						LOG.AddFailure($"File could not be created|truncated for writing: {file.Path.Full}");
						return false;
					}
					STREAM.Position = 0;
					STREAM.CopyTo(stream);
					return true;
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{PATH.Full}:\r\n");
				return false;
			}
		}

		//...........................................................

		public static bool WriteAllText( cmk.IO.Path PATH, string TEXT, Log LOG = null, CancellationToken CANCEL = default )
		{
			try {
				var file = new cmk.IO.File(PATH);
				if( file.Exists && TEXT == null ) {
					System.IO.File.Delete(PATH.Full);
					return true;
				}
				// System.IO.File.WriteAllText(PATH.Full, TEXT, Encoding.UTF8);  // fails if already open exclusive
				// return true;
				using( var stream = file.WaitOpenExclusiveReadWriteTruncate(default, CANCEL) ) {
					if( stream == null ) {
						LOG.AddFailure($"File could not be created|truncated for writing: {file.Path.Full}");
						return false;
					}
					using( var writer = new StreamWriter(stream, Encoding.UTF8) ) {
						writer.Write(TEXT);
						return true;
					}
				}
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{PATH.Full}:\r\n");
				return false;
			}
		}
	}
}

//=============================================================================
