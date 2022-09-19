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
    /// <summary>
    /// PixelFormats.Bgra32 bitmap builder.
    /// </summary>
    public class BitmapBuilder
	{
		public static readonly PixelFormat PixelFormat = PixelFormats.Bgra32;
		public static readonly int         Bpp         = 4;  // bytes per pixel

		//...........................................................

		public BitmapBuilder( int WIDTH, int HEIGHT )
		{
			Width  = WIDTH;
			Height = HEIGHT;
			Stride = Bpp * Width;
			Raw    = new byte[Height * Stride];
		}

		//...........................................................

		public int Width  { get; }
		public int Height { get; }
		public int Stride { get; }
		public byte[] Raw { get; }

		//...........................................................

		public Color GetPixel( int X, int Y )
		{
			Y *= Stride;
			X *= Bpp;
			X += Y;
			return new Color {
				B = Raw[X++],
				G = Raw[X++],
				R = Raw[X++],
				A = Raw[X++],
			};
		}

		//...........................................................

		public void SetPixel( int X, int Y, Color COLOR )
		{
			Y *= Stride;
			X *= Bpp;
			X += Y;
			Raw[X++] = COLOR.B;
			Raw[X++] = COLOR.G;
			Raw[X++] = COLOR.R;
			Raw[X++] = COLOR.A;
		}

		//...........................................................

		public void Clear( Color COLOR )
		{
			for( var y = 0; y < Height; ++y ) HLine(y, COLOR);
		}

		//...........................................................

		public void HLine( int Y, Color COLOR, int X0 = 0, int X1 = Int32.MaxValue )
		{
			Y *= Stride;
			X1 = Math.Min(Width, X1);
			for( int x = X0, x0 = Y + (X0 * Bpp); x < X1; ++x ) {
				Raw[x0++] = COLOR.B;
				Raw[x0++] = COLOR.G;
				Raw[x0++] = COLOR.R;
				Raw[x0++] = COLOR.A;
			}
		}

		//...........................................................

		public void VLine( int X, Color COLOR, int Y0 = 0, int Y1 = Int32.MaxValue )
		{
			Y1 = Math.Min(Height, Y1);
			X *= Bpp;
			for( int y = Y0, y0 = Y0; y < Y1; ++y, y0 += Stride ) {
				Raw[y0 + X]     = COLOR.B;
				Raw[y0 + X + 1] = COLOR.G;
				Raw[y0 + X + 2] = COLOR.R;
				Raw[y0 + X + 3] = COLOR.A;
			}
		}

		//...........................................................

		public BitmapSource CreateBitmap( int DPI_X = 96, int DPI_Y = 96 )
		{
			return BitmapSource.Create(
				Width, Height,
				DPI_X, DPI_Y,
				PixelFormat, null,
				Raw, Stride
			);
		}
	}
}

//=============================================================================
