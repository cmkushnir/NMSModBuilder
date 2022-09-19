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
using System.Text;

//=============================================================================

namespace cmk
{
    // Defined in EndianBinaryReader
    //public enum Endian
    //{
    //	Big,
    //	Little,
    //}

    //=========================================================================

    public class EndianBinaryWriter
	: System.IO.BinaryWriter
	{
		public static readonly Endian SystemEndian = BitConverter.IsLittleEndian ?
			cmk.Endian.Little : cmk.Endian.Big
		;

		//...........................................................

		public EndianBinaryWriter( Stream STREAM )
		: base(STREAM)
		{
			Endian = SystemEndian;
		}

		public EndianBinaryWriter( Stream STREAM, Encoding ENCODING )
		: base(STREAM, ENCODING)
		{
			Endian = SystemEndian;
		}

		public EndianBinaryWriter( Stream STREAM, Encoding ENCODING, bool LEAVE_OPEN )
		: base(STREAM, ENCODING, LEAVE_OPEN)
		{
			Endian = SystemEndian;
		}

		//...........................................................

		public EndianBinaryWriter( Stream STREAM, Endian ENDIANNESS )
		: base(STREAM)
		{
			Endian = ENDIANNESS;
		}

		public EndianBinaryWriter( Stream STREAM, Encoding ENCODING, Endian ENDIANNESS )
		: base(STREAM, ENCODING)
		{
			Endian = ENDIANNESS;
		}

		public EndianBinaryWriter( Stream STREAM, Encoding ENCODING, bool LEAVE_OPEN, Endian ENDIANNESS )
		: base(STREAM, ENCODING, LEAVE_OPEN)
		{
			Endian = ENDIANNESS;
		}

		//...........................................................

		public Endian Endian { get; set; }

		//...........................................................

		public override void Write( short VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public override void Write( ushort VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public override void Write( int VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public override void Write( uint VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public override void Write( long VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public override void Write( ulong VALUE )
		{
			if( Endian == SystemEndian ) base.Write(VALUE);
			else {
				var bytes = BitConverter.GetBytes(VALUE);
				Array.Reverse(bytes);
				base.Write(bytes);
			}
		}

		//...........................................................

		public void Write24( uint VALUE )
		{
			var bytes = BitConverter.GetBytes(VALUE);
			if( Endian != SystemEndian ) Array.Reverse(bytes);
			var offset = Endian == Endian.Big ? 1 : 0;
			base.Write(bytes, offset, 3);
		}

		//...........................................................

		public void Write40( ulong VALUE )
		{
			var bytes = BitConverter.GetBytes(VALUE);
			if( Endian != SystemEndian ) Array.Reverse(bytes);
			var offset = Endian == Endian.Big ? 3 : 0;
			base.Write(bytes, offset, 5);
		}
	}
}

//=============================================================================
