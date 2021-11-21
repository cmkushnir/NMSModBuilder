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
			public string Name  { get; set; }
			public int    Value { get; set; }
			public override string ToString()
			{
				return Name;
			}
			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}
		}

		//...........................................................

		public string      Name   { get; }
		public ValueType[] Values { get; }

		//...........................................................

		public FakeEnum( string NAME, string [] VALUES )
		{
			Name = NAME;
			if( VALUES.IsNullOrEmpty() ) return;

			Values = new ValueType[VALUES.Length];

			for( var i = 0; i < Values.Length; ++i ) {
				Values[i].Name  = VALUES[i];
				Values[i].Value = i;
			}
		}

		//...........................................................

		public FakeEnum( Type ENUM_T )
		{
			Name = ENUM_T?.Name;
			if( Name.IsNullOrEmpty() ) return;

			var names  = Enum.GetNames (ENUM_T);
			var values = Enum.GetValues(ENUM_T) as int [];
			if( values.IsNullOrEmpty() ) return;

			Values = new ValueType[values.Length];

			for( var i = 0; i < Values.Length; ++i ) {
				Values[i].Name  = names [i];
				Values[i].Value = values[i];
			}
		}

		//...........................................................

		public override string ToString()
		{
			return Name;
		}
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}

//=============================================================================
