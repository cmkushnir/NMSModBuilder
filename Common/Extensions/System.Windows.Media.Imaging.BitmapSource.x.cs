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

using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		public static BitmapSource Invert( this BitmapSource SOURCE, bool FREEZE = true )
		{
			var bpp = SOURCE.Format.BitsPerPixel / 8;  // bytes per pixel
			if( bpp < 3 ) return null;

			var stride = SOURCE.PixelWidth * bpp;
			var length = stride * SOURCE.PixelHeight;
			var data   = new byte[length];

			SOURCE.CopyPixels(data, stride, 0);

			for( var i = 0;  i < length;  i += bpp ) {
				data[i]     = (byte)(255 - data[i]);      // R
				data[i + 1] = (byte)(255 - data[i + 1]);  // G
				data[i + 2] = (byte)(255 - data[i + 2]);  // B
			//	data[i + 3] = (byte)(255 - data[i + 3]);  // A
			}

			var bmp = BitmapSource.Create(
				SOURCE.PixelWidth, SOURCE.PixelHeight,
				SOURCE.DpiX, SOURCE.DpiY,
				SOURCE.Format, null,
				data, stride
			);
			if( FREEZE ) bmp?.Freeze();

			return bmp;
		}
	}
}

//=============================================================================
