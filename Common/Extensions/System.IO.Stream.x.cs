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
using System.Threading;
using System.Threading.Tasks;

//=============================================================================

namespace cmk
{
    public static partial class _x_
	{
		/// <summary>
		/// Create new stream via cmk.IO.Stream.MemoryOrTempFile(SOURCE.Length, BUFFER_SIZE),
		/// then copy all of SOURCE to new stream.
		/// Returns null if SOURCE == null;
		/// </summary>
		public static Stream Clone(
			this Stream SOURCE,
			     int    BUFFER_SIZE = 65536
		){
			if( SOURCE == null ) return null;

			var clone = cmk.IO.Stream.MemoryOrTempFile(SOURCE.Length, BUFFER_SIZE);

			var source_pos = SOURCE.Position;
			SOURCE.Position = 0;

			SOURCE.CopyTo(clone);

			SOURCE.Position = source_pos;
			clone .Position = 0;

			return clone;
		}

		//...........................................................

		/// <summary>
		/// PROGRESS.Report receives total bytes copied upto that point.
		/// Caller will need to convert that to a % if needed,
		/// since we don't check if SOURCE implements Length here.
		/// </summary>
		public static async Task CopyToAsync(
			this Stream            SOURCE,
			     Stream            TARGET,
			     IProgress<long>   PROGRESS    = null,
			     CancellationToken CANCEL      = default,
			     int               BUFFER_SIZE = 65536
		){
			if( SOURCE == null ) throw new ArgumentNullException(nameof(SOURCE));
			if( TARGET == null ) throw new ArgumentNullException(nameof(TARGET));

			if( !SOURCE.CanRead  ) throw new ArgumentException("Can't read from source.", nameof(SOURCE));
			if( !TARGET.CanWrite ) throw new ArgumentException("Can't write to target.",  nameof(TARGET));

			if( BUFFER_SIZE < 4096 ) BUFFER_SIZE = 4096;
			var buffer = new byte[BUFFER_SIZE];

			int  read  = 0;
			long total = 0;

			while( (read = await SOURCE.ReadAsync(buffer, 0, buffer.Length, CANCEL).ConfigureAwait(false)) != 0 ) {
				await TARGET.WriteAsync(buffer, 0, read, CANCEL).ConfigureAwait(false);
				total += read;
				PROGRESS?.Report(total);
			}
		}
	}
}

//=============================================================================
