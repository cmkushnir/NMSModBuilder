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
using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
    public static partial class _x_
	{
		// Only supports Bgr24 and Bgra32 formats.
		// A is not inverted, instead if A < ALPHA_MULT_THRESHOLD then multiply it by ALPHA_MULT.
		public static BitmapSource Invert(
			this BitmapSource SOURCE,
			byte              ALPHA_MULT_THRESHOLD = 250,  // 0 - 255
			double            ALPHA_MULT           = 1,    // >= 0
			bool              FREEZE               = true
		){
			if( SOURCE == null ) return null;

			// only support the rgb formats we map to from pfim dds
			if( SOURCE.Format != PixelFormats.Bgr24 &&
				SOURCE.Format != PixelFormats.Bgra32
			)	return null;

			var bpp    = SOURCE.Format.BitsPerPixel / 8;  // bytes per pixel (3 or 4)
			var stride = SOURCE.PixelWidth  * bpp;
			var length = SOURCE.PixelHeight * stride;
			var data   = new byte[length];

			SOURCE.CopyPixels(data, stride, 0);

			for( var i = 0; i < length; i += bpp ) {
				data[i]     = (byte)(255 - data[i]);        // R
				data[i + 1] = (byte)(255 - data[i + 1]);    // G
				data[i + 2] = (byte)(255 - data[i + 2]);    // B
				if( bpp > 3 && data[i + 3] < ALPHA_MULT_THRESHOLD ) {  // A
					data[i + 3] = (byte)Math.Clamp(data[i + 3] * ALPHA_MULT, 0, 255);
				}
			}

			try {
				var bmp = BitmapSource.Create(
					SOURCE.PixelWidth, SOURCE.PixelHeight,
					SOURCE.DpiX, SOURCE.DpiY,
					SOURCE.Format, null,
					data, stride
				);
				if( FREEZE ) bmp?.Freeze();
				return bmp;
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return null;
			}
		}
	}
}

//=============================================================================
