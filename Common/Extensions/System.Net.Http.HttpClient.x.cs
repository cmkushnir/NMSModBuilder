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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// Use HTTP to request URI content and copy to TARGET.
		/// Report % progress (0-100) to PROGRESS_PERCENT.
		/// </summary>
		public static async Task RecvAsync(
			this HttpClient        HTTP,
			     Uri               URI,
			     Stream            TARGET,
			     IProgress<byte>   PROGRESS_PERCENT = null,  // 0 - 100
			     CancellationToken CANCEL           = default,
			     int               BUFFER_SIZE      = 65536
		){
			if( HTTP   == null ) throw new ArgumentNullException(nameof(HTTP));
			if( URI    == null ) throw new ArgumentNullException(nameof(URI));
			if( TARGET == null ) throw new ArgumentNullException(nameof(TARGET));

			if( !TARGET.CanWrite ) throw new ArgumentException("Can't write to target.", nameof(TARGET));

			using( var response = await HTTP.GetAsync(URI, HttpCompletionOption.ResponseHeadersRead, CANCEL).ConfigureAwait(false) ) {
				if( !response.IsSuccessStatusCode ) throw new HttpRequestException(
					response.ReasonPhrase, null, response.StatusCode
				);
				var length = response.Content.Headers.ContentLength.GetValueOrDefault(0);
				using( var source = await response.Content.ReadAsStreamAsync(CANCEL).ConfigureAwait(false) ) {
					if( PROGRESS_PERCENT == null || length < 1 ) {
						await source.CopyToAsync(TARGET, CANCEL).ConfigureAwait(false);
						return;
					}
					var progress = new Progress<long>(
						// forward progress to PROGRESS_PERCENT.
						// convert CopyToAsync total-bytes-copied to a % (0 - 100).
						TOTAL => PROGRESS_PERCENT.Report((byte)(100 * TOTAL / length))
					);
					await source.CopyToAsync(TARGET, progress, CANCEL, BUFFER_SIZE).ConfigureAwait(false);
					PROGRESS_PERCENT.Report(100);
				}
			}
		}
	}
}

//=============================================================================
