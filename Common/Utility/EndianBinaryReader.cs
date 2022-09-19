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
    public enum Endian
	{
		Big,
		Little,
	}

	//=========================================================================

	public class EndianBinaryReader
	: System.IO.BinaryReader
	{
		public static readonly Endian SystemEndian = BitConverter.IsLittleEndian ?
			cmk.Endian.Little : cmk.Endian.Big
		;

		//...........................................................

		public EndianBinaryReader( Stream STREAM )
		: base(STREAM)
		{
			Endian = SystemEndian;
		}

		public EndianBinaryReader( Stream STREAM, Encoding ENCODING )
		: base(STREAM, ENCODING)
		{
			Endian = SystemEndian;
		}

		public EndianBinaryReader( Stream STREAM, Encoding ENCODING, bool LEAVE_OPEN )
		: base(STREAM, ENCODING, LEAVE_OPEN)
		{
			Endian = SystemEndian;
		}

		//...........................................................

		public EndianBinaryReader( Stream STREAM, Endian ENDIANNESS )
		: base(STREAM)
		{
			Endian = ENDIANNESS;
		}

		public EndianBinaryReader( Stream STREAM, Encoding ENCODING, Endian ENDIANNESS )
		: base(STREAM, ENCODING)
		{
			Endian = ENDIANNESS;
		}

		public EndianBinaryReader( Stream STREAM, Encoding ENCODING, bool LEAVE_OPEN, Endian ENDIANNESS )
		: base(STREAM, ENCODING, LEAVE_OPEN)
		{
			Endian = ENDIANNESS;
		}

		//...........................................................

		public Endian Endian { get; set; }

		//...........................................................

		protected byte[] EndianRead( int COUNT )
		{
			var bytes = ReadBytes(COUNT);
			if( Endian != SystemEndian ) Array.Reverse(bytes);
			return bytes;
		}

		//...........................................................

		protected byte[] EndianRead( int FROM, int TO )
		{
			var from = ReadBytes(FROM);
			var to   = new byte[TO];

			if( Endian !=  SystemEndian ) Array.Reverse(from);
			var offset  = (SystemEndian == Endian.Big) ? (TO - FROM) : 0;
			Buffer.BlockCopy(from, 0, to, offset, FROM);

			return to;
		}

		//...........................................................

		public override short ReadInt16() => BitConverter.ToInt16(EndianRead(2), 0);
		public override int   ReadInt32() => BitConverter.ToInt32(EndianRead(4), 0);
		public override long  ReadInt64() => BitConverter.ToInt64(EndianRead(8), 0);

		public override ushort ReadUInt16() => BitConverter.ToUInt16(EndianRead(2), 0);
		public override uint   ReadUInt32() => BitConverter.ToUInt32(EndianRead(4), 0);
		public override ulong  ReadUInt64() => BitConverter.ToUInt64(EndianRead(8), 0);

		//...........................................................

		public uint  ReadUInt24() => BitConverter.ToUInt32(EndianRead(3, 4), 0);
		public ulong ReadUInt40() => BitConverter.ToUInt64(EndianRead(5, 8), 0);
	}
}

//=============================================================================
