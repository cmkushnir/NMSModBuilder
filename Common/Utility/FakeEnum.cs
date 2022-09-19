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

//=============================================================================

namespace cmk
{
    public class FakeEnum
	{
		public struct ValueType
		{
			public string Name      { get; set; }
			public uint   Value     { get; set; }
			public string ValueText { get; set; }
			public override string ToString() => Name;
			public override int GetHashCode() => Name.GetHashCode();
		}

		//...........................................................

		public string      Name   { get; }
		public ValueType[] Values { get; }
		public bool        IsMask { get; }

		//...........................................................

		public FakeEnum( string NAME, string[] VALUES )
		{
			Name = NAME;
			if( VALUES.IsNullOrEmpty() ) return;

			Values = new ValueType[VALUES.Length];

			for( uint i = 0; i < Values.Length; ++i ) {
				Values[i].Name  = VALUES[i];
				Values[i].Value = i;
			}

			IsMask =
				Values[Values.Length - 1].Value != Values.Length - 1 &&
				Name.Contains("Flags",    StringComparison.OrdinalIgnoreCase) &&
				Name.Contains("Channels", StringComparison.OrdinalIgnoreCase) &&
				Name.Contains("Mask",     StringComparison.OrdinalIgnoreCase)
			;

			for( uint i = 0; i < Values.Length; ++i ) {
				Values[i].ValueText = IsMask ?
					Convert.ToString(Values[i].Value, 2).PadLeft(32, '0') :
					Values[i].Value.ToString("d")
				;
			}
		}

		//...........................................................

		public FakeEnum( Type ENUM_T )
		{
			Name = ENUM_T?.Name;
			if( Name.IsNullOrEmpty() ) return;

			var names  = Enum.GetNames (ENUM_T);
			var values = Enum.GetValues(ENUM_T) as uint [];
			if( values.IsNullOrEmpty() ) return;

			Values = new ValueType[values.Length];

			for( var i = 0; i < Values.Length; ++i ) {
				Values[i].Name  = names[i];
				Values[i].Value = values[i];
			}

			IsMask =
				Values[Values.Length - 1].Value != Values.Length - 1 &&
				Name.Contains("Flags",    StringComparison.OrdinalIgnoreCase) &&
				Name.Contains("Channels", StringComparison.OrdinalIgnoreCase) &&
				Name.Contains("Mask",     StringComparison.OrdinalIgnoreCase)
			;

			for( uint i = 0; i < Values.Length; ++i ) {
				Values[i].ValueText = IsMask ?
					Convert.ToString(Values[i].Value, 2).PadLeft(32, '0') :
					Values[i].Value.ToString("d")
				;
			}
		}

		//...........................................................

		public override string ToString() => Name;
		public override int GetHashCode() => Name.GetHashCode();
	}
}

//=============================================================================
