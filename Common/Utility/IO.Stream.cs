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

//=============================================================================

namespace cmk.IO
{
	public static class Stream
	{
		/// <summary>
		/// If CAPACITY < Int32.MaxValue return a new MemoryStream constructed
		/// with the specified CAPACITY, else return a FileStream to a temp
		/// DeleteOnClose file with the specified BUFFER_SIZE.
		/// CAPACITY should be treated as the maximum size this stream will get
		/// otherwise may get a MemoryStream only to throw when you write > Int32.MaxValue bytes to it.
		/// </summary>
		public static System.IO.Stream MemoryOrTempFile( long CAPACITY, int BUFFER_SIZE = 65536 )
		{
			System.IO.Stream stream;
			if( CAPACITY < Int32.MaxValue ) {
				stream = new MemoryStream((int)CAPACITY);
			}
			else {
				var temp = System.IO.Path.GetTempFileName();
				stream   = new FileStream(temp,
					FileMode.Open,
					FileAccess.ReadWrite,
					FileShare.None,
					BUFFER_SIZE,
					FileOptions.DeleteOnClose
				);
			}
			stream.Position = 0;
			return stream;
		}
	}
}

//=============================================================================
