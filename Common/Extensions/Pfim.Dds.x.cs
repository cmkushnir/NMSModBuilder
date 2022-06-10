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
	public struct Rgba16
	{
		ushort Raw;

		public Rgba16( ushort RAW = 0 )         { Raw = RAW; }
		public Rgba16( byte BYTE0, byte BYTE1 ) { Raw = (ushort)((BYTE0 << 8) | BYTE1); }

		public void Set( ushort RAW )             { Raw = RAW; }
		public void Set( byte BYTE0, byte BYTE1 ) { Raw = (ushort)((BYTE0 << 8) | BYTE1); }

		public byte R {
			get { return (byte)((ushort)(Raw & 0xf000) >> 12); }
			set { Raw = (ushort)((Raw & 0x0fff) | (Math.Clamp(value, (ushort)0, (ushort)15) << 12)); }
		}

		public byte G {
			get { return (byte)((ushort)(Raw & 0x0f00) >> 8); }
			set { Raw = (ushort)((Raw & 0xf0ff) | (Math.Clamp(value, (ushort)0, (ushort)15) << 8)); }
		}

		public byte B {
			get { return (byte)((ushort)(Raw & 0x00f0) >> 4); }
			set { Raw = (ushort)((Raw & 0xff0f) | (Math.Clamp(value, (ushort)0, (ushort)15) << 4)); }
		}

		public byte A {
			get { return (byte)((ushort)(Raw & 0x000f) >> 0); }
			set { Raw = (ushort)((Raw & 0xfff0) | (Math.Clamp(value, (ushort)0, (ushort)15) << 0)); }
		}
	}

	public struct R5g5b5a1
	{
		ushort Raw;

		public R5g5b5a1( ushort RAW = 0 )         { Raw = RAW; }
		public R5g5b5a1( byte BYTE0, byte BYTE1 ) { Raw = (ushort)((BYTE0 << 8) | BYTE1); }

		public void Set( ushort RAW )             { Raw = RAW; }
		public void Set( byte BYTE0, byte BYTE1 ) { Raw = (ushort)((BYTE0 << 8) | BYTE1); }

		public byte R {
			get { return (byte)((ushort)(Raw & 0b1111100000000000) >> 11); }
			set { Raw = (ushort)((Raw & 0b0000011111111111) | (Math.Clamp(value, (ushort)0, (ushort)31) << 11)); }
		}

		public byte G {
			get { return (byte)((ushort)(Raw & 0b0000011111000000) >> 6); }
			set { Raw = (ushort)((Raw & 0b1111100000111111) | (Math.Clamp(value, (ushort)0, (ushort)31) << 6)); }
		}

		public byte B {
			get { return (byte)((ushort)(Raw & 0b0000000000111110) >> 1); }
			set { Raw = (ushort)((Raw & 0b1111111111000001) | (Math.Clamp(value, (ushort)0, (ushort)31) << 1)); }
		}

		public byte A {
			get { return (byte)((ushort)(Raw & 0b0000000000000001) >> 0); }
			set { Raw = (ushort)((Raw & 0b1111111111111110) | (Math.Clamp(value, (ushort)0, (ushort)1) << 0)); }
		}
	}

	//=========================================================================

	public static partial class _x_
	{
		/// <summary>
		/// !Data.IsNullOrEmpty && Data.Length >= Width * Height * BytesPerPixel;
		/// </summary>
		public static bool IsValid( this Pfim.Dds DDS )
		{
			return DDS != null &&
				DDS.Data != null &&
				DDS.Data.Length >= (DDS.Width * DDS.Height * DDS.BytesPerPixel)
			;
		}

		//...........................................................

		/// <summary>
		/// Map a Pfim.ImageFormat to a System.Windows.Media.PixelFormats.
		/// </summary>
		public static PixelFormat PixelFormat( this Pfim.Dds DDS )
		{
			if( DDS != null )
			switch( DDS.Format ) {
				case Pfim.ImageFormat.Rgb8:   return PixelFormats.Gray8;
				case Pfim.ImageFormat.R5g5b5: return PixelFormats.Bgr555;
				case Pfim.ImageFormat.R5g6b5: return PixelFormats.Bgr565;
				case Pfim.ImageFormat.Rgb24:  return PixelFormats.Bgr24;
				case Pfim.ImageFormat.Rgba32: return PixelFormats.Bgra32;
			}
			return PixelFormats.Default;
		}

		//...........................................................

		/// <summary>
		/// Build a description string for a Pfim dds image.
		/// </summary>
		public static string Description( this Pfim.Dds DDS )
		{
			if( !IsValid(DDS) ) return null;

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
			if( !IsValid(DDS) ) return null;

			var fmt    = PixelFormat(DDS);
			var stride = DDS.Stride;
			var data   = DDS.Data;

			// transform formats not supported by BitmapSource.Create to Bgra32
			switch( DDS.Format ) {
				case Pfim.ImageFormat.Rgba16: {  // 4 bits per channel
					Rgba16 pix = new();
					var dds_i = DDS.Data;
					var dds_o = new byte[DDS.Height * DDS.Width * 4];  // rgba32
					for( int r = 0; r < DDS.Height; ++r ) {
						var roi = r * DDS.Stride;     // input row offset
						var roo = r * DDS.Width * 4;  // output row offset
						for( int c = 0; c < DDS.Width; ++c ) {
							var coi = roi + (c * DDS.BytesPerPixel);
							var coo = roo + (c * 4);
							pix.Set(dds_i[coi + 0], dds_i[coi + 1]);
							dds_o[coo + 0] = (byte)((pix.R * 255) / 15);
							dds_o[coo + 1] = (byte)((pix.G * 255) / 15);
							dds_o[coo + 2] = (byte)((pix.B * 255) / 15);
							dds_o[coo + 3] = (byte)((pix.A * 255) / 15);
						}
					}
					fmt    = PixelFormats.Bgra32;
					stride = DDS.Width * 4;
					data   = dds_o;
					break;
				}
				case Pfim.ImageFormat.R5g5b5a1: {
					R5g5b5a1 pix = new();
					var dds_i = DDS.Data;
					var dds_o = new byte[DDS.Height * DDS.Width * 4];
					for( int r = 0; r < DDS.Height; ++r ) {
						var roi = r * DDS.Stride;
						var roo = r * DDS.Width * 4;
						for( int c = 0; c < DDS.Width; ++c ) {
							var coi = roi + (c * DDS.BytesPerPixel);
							var coo = roo + (c * 4);
							pix.Set(dds_i[coi + 0], dds_i[coi + 1]);
							dds_o[coo + 0] = (byte)((pix.R * 255) / 31);
							dds_o[coo + 1] = (byte)((pix.G * 255) / 31);
							dds_o[coo + 2] = (byte)((pix.B * 255) / 31);
							dds_o[coo + 3] = (byte)((pix.A * 255) / 1);
						}
					}
					fmt    = PixelFormats.Bgra32;
					stride = DDS.Width * 4;
					data   = dds_o;
					break;
				}
			}
			if( fmt == PixelFormats.Default ) return null;

			try {
				var bmp = BitmapSource.Create(
					DDS.Width, DDS.Height,
					96, 96,
					fmt,  null,
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

		//...........................................................

		/// <summary>
		/// Scaled bitmap from Pfim dds image;
		/// </summary>
		public static BitmapSource GetBitmap( this Pfim.Dds DDS, int HEIGHT, bool FREEZE = true )
		{
			var source  = GetBitmap(DDS);
			if( source == null || source.PixelHeight < 1 ) return null;
			try {
				var scale = (double)HEIGHT / source.PixelHeight;
				var bmp   = new TransformedBitmap(source, new ScaleTransform(scale, scale));
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
