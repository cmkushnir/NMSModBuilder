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

using System.Windows.Media;
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// Map a Pfim.ImageFormat to a System.Windows.Media.PixelFormats.
		/// </summary>
		public static PixelFormat GetPixelFormat( this Pfim.Dds DDS )
		{
			switch( DDS.Format ) {
				case Pfim.ImageFormat.Rgb8:     return PixelFormats.Gray8;
				case Pfim.ImageFormat.Rgb24:    return PixelFormats.Bgr24;
				case Pfim.ImageFormat.Rgba32:   return PixelFormats.Bgra32;
				case Pfim.ImageFormat.R5g5b5:
				case Pfim.ImageFormat.R5g5b5a1: return PixelFormats.Bgr555;
				case Pfim.ImageFormat.R5g6b5:   return PixelFormats.Bgr565;
			}
			return PixelFormats.Default;
		}

		//...........................................................

		/// <summary>
		/// Build a description string for a Pfim dds image.
		/// </summary>
		public static string Description( this Pfim.Dds DDS )
		{
			if( DDS == null ) return null;

			var data   = DDS.Data;
			var width  = DDS.Width;
			var height = DDS.Height;

			var dimension = "";
			if( DDS.Header10 != null )
			switch( DDS.Header10.ResourceDimension ) {
				case Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_BUFFER:    dimension = "Buffer "; break;
				case Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE1D: dimension = "1D ";     break;
				case Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE2D: dimension = "2D ";     break;
				case Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE3D: dimension = "3D ";     break;
			}

			var depth = "";
			if( DDS.Header != null && DDS.Header.Depth > 0 ) depth = $" x {DDS.Header.Depth}";

			var    fourcc = DDS.Header.PixelFormat.FourCC;
			string format = DDS.Format.ToString();

			if( fourcc != Pfim.CompressionAlgorithm.None ) {
				var str = fourcc.ToString();
				if( str.StartsWith("D3DFMT_") ) str = str.Substring(7);
				format = $"{DDS.Format} ({str})";
			}

			return $"{dimension}{width} x {height}{depth},  {format},  {DDS.MipMaps.Length} MipMaps,  {data.Length} bytes";
		}

		//...........................................................

		/// <summary>
		/// Untransformed bitmap from Pfim dds image.
		/// </summary>
		public static BitmapSource GetBitmap( this Pfim.Dds DDS, bool FREEZE = true )
		{
			if( (DDS == null) || (DDS.Height < 1) ) return null;

			var bmp = BitmapSource.Create(
				DDS.Width, DDS.Height,
				96, 96,
				DDS.GetPixelFormat(), null,
				DDS.Data, DDS.Stride
			);
			if( FREEZE ) bmp?.Freeze();

			return bmp;
		}

		//...........................................................

		/// <summary>
		/// Untransformed bitmap from Pfim dds image.
		/// </summary>
		public static BitmapSource GetInverseBitmap( this Pfim.Dds DDS, bool FREEZE = true )
		{
			if( (DDS == null) || (DDS.Height < 1) || DDS.BytesPerPixel < 3 ) return null;

			var data = DDS.Data.Clone() as byte[];

			for( var i = 0;  i < data.Length;  i += DDS.BytesPerPixel ) {
				data[i]     = (byte)(255 - data[i]);      // R
				data[i + 1] = (byte)(255 - data[i + 1]);  // G
				data[i + 2] = (byte)(255 - data[i + 2]);  // B
				// hack: we only call this for portal symbols
				// use BitmapSource.Invert extension to get nno-hack inverse
				if( data[i + 3] < 250 ) {
					data[i + 3] = (byte)(data[i + 2] * 0.5);  // A
				}
			}

			var bmp = BitmapSource.Create(
				DDS.Width, DDS.Height,
				96, 96,
				DDS.GetPixelFormat(), null,
				data, DDS.Stride
			);
			if( FREEZE ) bmp?.Freeze();

			return bmp;
		}

		//...........................................................

		/// <summary>
		/// Scaled bitmap from Pfim dds image;
		/// </summary>
		public static BitmapSource GetBitmap( this Pfim.Dds DDS, int HEIGHT, bool FREEZE = true )
		{
			var source  = GetBitmap(DDS);
			if( source == null || source.PixelHeight < 1 ) return null;

			var scale = (double)HEIGHT / source.PixelHeight;
			var bmp   = new TransformedBitmap(source, new ScaleTransform(scale, scale));
			if( FREEZE ) bmp?.Freeze();

			return bmp;
		}

		//...........................................................

		/// <summary>
		/// Scaled bitmap from Pfim dds image;
		/// </summary>
		public static BitmapSource GetInverseBitmap( this Pfim.Dds DDS, int HEIGHT, bool FREEZE = true )
		{
			var source  = GetInverseBitmap(DDS);
			if( source == null || source.PixelHeight < 1 ) return null;

			var scale = (double)HEIGHT / source.PixelHeight;
			var bmp   = new TransformedBitmap(source, new ScaleTransform(scale, scale));
			if( FREEZE ) bmp?.Freeze();

			return bmp;
		}
	}
}

//=============================================================================
