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
using System.Threading;

//=============================================================================

namespace cmk.IO
{
	/// <summary>
	/// Base class for File and Directory wrapper classes.
	/// </summary>
	public abstract class Item
	: System.IComparable
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

		public Path Path { get; set; }

		//...........................................................

		public virtual bool Exists()
		{
			return false;
		}

		//...........................................................

		public virtual bool EnsureExists()
		{
			return false;
		}

		//...........................................................

		public override bool Equals( object RHS ) => CompareTo(RHS) == 0;

		//...........................................................

		public int CompareTo( object RHS )
		{
			if( RHS is Item rhs ) return Path.CompareTo(rhs?.Path);
			                      return Path.CompareTo(RHS);
		}

		//...........................................................

		public override int    GetHashCode() => Path.GetHashCode();
		public override string ToString()    => Path;
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

		public Directory( string DIRECTORY, string NAME, string EXTENSION = null )
		: base(DIRECTORY, NAME, EXTENSION)
		{
		}

		//...........................................................

		public override bool Exists()
		{
			return System.IO.Directory.Exists(Path);
		}

		//...........................................................

		public override bool EnsureExists()
		{
			if( !Exists() && !Path.IsNullOrEmpty() ) {
				try  { System.IO.Directory.CreateDirectory(Path); }
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
			}
			return Exists();
		}

		//...........................................................

		public IEnumerable<FileInfo> Files( string PATTERN = null )
		{
			if( Exists() ) try {
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
			if( Exists() ) try {
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

		public override bool Exists()
		{
			return System.IO.File.Exists(Path);
		}

		//...........................................................

		public override bool EnsureExists()
		{
			if( !Exists() && !Path.IsNullOrEmpty() ) {
				try  { System.IO.File.Create(Path); }
				catch( Exception EX ) { Log.Default.AddFailure(EX); }
			}
			return Exists();
		}

		//...........................................................

		/// <summary>
		/// Open existing or create new for exclusive read|write.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Ticks.</param>
		public FileStream WaitOpen(
			FileMode          MODE,
			FileAccess        ACCESS,
			FileShare         SHARE,
			int               TIMEOUT = Int32.MaxValue,
			CancellationToken CANCEL = default
		){
			var end = DateTime.Now.Ticks + TIMEOUT;

			do {
				FileStream fs = null;
				try {
					fs = new(Path.Full, MODE, ACCESS, SHARE);
					if( fs != null ) return fs;
				}
				catch( IOException ) {  // assume someone else using
					if( fs != null ) {
						fs.Dispose();
						fs = null;
					}
					Thread.Sleep(1);
				}
				catch( Exception EX ) {
					Log.Default.AddFailure(EX);
					break;
				}
			}	while(
				DateTime.Now.Ticks < end &&
				!CANCEL.IsCancellationRequested
			);

			return null;
		}

		//...........................................................

		/// <summary>
		/// Open existing or create new for exclusive read|write.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Ticks.</param>
		public FileStream WaitOpenExclusiveReadWrite( int TIMEOUT = Int32.MaxValue, CancellationToken CANCEL = default )
		{
			return WaitOpen(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, TIMEOUT, CANCEL);
		}

		/// <summary>
		/// Open existing for shared read.
		/// If file currently locked will keep trying until success or TIMEOUT.
		/// </summary>
		/// <param name="TIMEOUT">Ticks.</param>
		protected FileStream WaitOpenSharedReadOnly( int TIMEOUT = int.MaxValue, CancellationToken CANCEL = default )
		{
			return WaitOpen(FileMode.Open, FileAccess.Read, FileShare.Read, TIMEOUT, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Get the embedded link date from the PE header in the PATH exe.
		/// </summary>
		/// <param name="PATH">Full path to an exe.</param>
		/// <param name="TZ">Optional timezone for returned DateTime, default is UTC.</param>
		/// <returns></returns>
		public static DateTime PEBuildDate( string PATH, TimeZoneInfo TZ = null )
		{
			PATH = PATH?.Replace('/', '\\');  // steam: "g:/steam\dir1\dir2\name.ext"
			if( PATH.IsNullOrEmpty() || !System.IO.File.Exists(PATH) ) return DateTime.MinValue;

			var buffer = new byte[2048];
			int length = 0;

			using( var stream = new FileStream(PATH, FileMode.Open, FileAccess.Read) ) {
				length = stream.Read(buffer, 0, buffer.Length);
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
	}
}

//=============================================================================
