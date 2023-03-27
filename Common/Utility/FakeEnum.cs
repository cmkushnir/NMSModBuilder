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
using System.Runtime.InteropServices;

//=============================================================================

namespace cmk
{
	/// <summary>
	/// Early mbinc versions used a string array attribute on an array field
	/// if the array was indexed by an enum, there were no proper enums defined.
	/// e.g. https://github.com/monkeyman192/MBINCompiler/blob/1.38.0/libMBIN/Source/Models/Structs/Globals/GcGalaxyGlobals.cs
	///    [NMS(Size = 0xA, EnumValue = new[] {
	///      "StartingLocation", "Home", "Waypoint", "Contact",
	///      "Blackhole", "AtlasStation", "Selection", "PlanetBase",
	///      "Visited", "ScanEvent"
	///    })]
	///    /* 0x200 */ public GcGalaxyMarkerSettings[] GalaxyMarkers;
	/// FakeEnum can support both the newer true enums and the older string[] ones.
	/// </summary>
	public class FakeEnum
	{
		public struct ValueType
		{
			public string Name      { get; set; }
			public ulong  Value     { get; set; }
			public string ValueText { get; set; }
			public string ValueTip  { get; set; }
			public override string ToString() => Name;
			public override int GetHashCode() => Name.GetHashCode();
		}

		//...........................................................

		public string      Name           { get; }
		public ValueType[] Values         { get; }
		public Type        UnderlyingType { get; } = typeof(uint);
		public bool        IsMask         { get; } = false;

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

			IsMask = PostConstruct();
		}

		//...........................................................

		public FakeEnum( Type ENUM_T )
		{
			Name = ENUM_T?.Name;
			if( Name.IsNullOrEmpty() ) return;

			UnderlyingType = Enum.GetUnderlyingType(ENUM_T);
			var typecode   = TypeCode.UInt64;  // Type.GetTypeCode(ENUM_T);

			var names  = Enum.GetNames (ENUM_T);
			var values = Enum.GetValues(ENUM_T);
			if( values.IsNullOrEmpty() ) return;

			Values = new ValueType[values.Length];

			// underlying type may be 1, 2, 4, 8 bytes in size
			for( var i = 0; i < Values.Length; ++i ) {
				Values[i].Name  = names[i];
				Values[i].Value = (ulong)Convert.ChangeType(values.GetValue(i), typecode);
			}

			IsMask = PostConstruct();
		}

		//...........................................................

		protected bool PostConstruct()
		{
			var is_mask = false;
			for( ulong i = 0; i < (ulong)Values.Length; ++i ) {
				if( Values[i].Value != i ) is_mask = true;
			}

			if( !is_mask ) {
				for( ulong i = 0; i < (ulong)Values.Length; ++i ) {
					Values[i].ValueText = Values[i].Value.ToString("d");
				}
			}
			else {
				var size = Marshal.SizeOf(UnderlyingType);
				var hex  = size * 2;
				var bin  = size * 8;
				for( ulong i = 0; i < (ulong)Values.Length; ++i ) {
					Values[i].ValueText = Convert.ToString((long)Values[i].Value,  2).PadLeft(bin, '0');
					Values[i].ValueTip  = "0x{0}   {1}".Format(
						Convert.ToString((long)Values[i].Value, 16).PadLeft(hex, '0'),
						Values[i].Value
					);
				}
				Array.Sort(Values, ( LHS, RHS ) => string.Compare(LHS.ValueText, RHS.ValueText));
			}

			return is_mask;
		}

		//...........................................................

		public override string ToString() => Name;
		public override int GetHashCode() => Name.GetHashCode();
	}
}

//=============================================================================
