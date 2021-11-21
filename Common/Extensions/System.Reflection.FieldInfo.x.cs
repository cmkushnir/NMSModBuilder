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

using System.Reflection;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// Build field name string, supports generic's, arrays, and libMBIN.NMSAttribute.
		/// </summary>
		public static string GenericName( this FieldInfo FIELD, bool FULL = false )
		{
			var generic_name = "";
			if( FULL ) generic_name = FIELD.FieldType.Namespace + ".";

			generic_name += RootName(FIELD.FieldType);

			var nms_attr = FIELD.GetCustomAttribute<libMBIN.NMSAttribute>();
			var nms_size = nms_attr?.Size ?? 0;
			var nms_enum = nms_attr?.EnumType;

			if( FIELD.FieldType.IsGenericType ) {
				generic_name += "<";
				var generics = FIELD.FieldType.GetGenericArguments();
				if( generics.Length > 0 ) {
					generic_name += GenericName(generics[0], false);
					for( int i = 1; i < generics.Length; ++i ) {
						generic_name += "," + GenericName(generics[i], false);
					}
				}
				generic_name += ">";
			}
			if( FIELD.FieldType.IsArray ) {
				for( var i = 0; i < FIELD.FieldType.GetArrayRank(); ++i ) {
					generic_name += "[";
					if( nms_size >  0 ) generic_name += nms_size;
					if( nms_enum != null ) {
						if( nms_size < 1 ) generic_name += nms_enum.GetEnumValues().Length;
						generic_name += ":" + nms_enum.Name;
					}
					generic_name += "]";
				}
			}
			else if( nms_size > 0 ) {
				generic_name += "(" + nms_size + ")";
			}
			else if( nms_enum != null ) {
				generic_name += "(" + nms_enum.GetEnumValues().Length + ":" + nms_enum.Name + ")";
			}

			return generic_name;
		}

		//...........................................................

		/// <summary>
		/// FIELD.FieldType.GenericName(false) + "  " + FIELD.Name
		/// </summary>
		public static string TypeAndName( this FieldInfo FIELD )
		{
			return FIELD.FieldType.GenericName(false) + "  " + FIELD.Name;
		}
	}
}

//=============================================================================
