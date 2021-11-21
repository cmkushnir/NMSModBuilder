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
using System.Linq;
using System.Reflection;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		public static bool IsCastableTo( this Type LHS, Type RHS )
		{
			return
				RHS.IsAssignableFrom(LHS) ||
				IsCastDefined(RHS, m => m.GetParameters()[0].ParameterType, _ => LHS, false) ||
				IsCastDefined(LHS, _ => RHS, m => m.ReturnType, true)
			;
		}

		static bool IsCastDefined(
			Type TYPE,
			Func<MethodInfo, Type> BASE,
			Func<MethodInfo, Type> DERIVED,
			bool LOOK_IN_BASE
		){
			var bindinFlags =
				BindingFlags.Public |
				BindingFlags.Static |
				(LOOK_IN_BASE ? BindingFlags.FlattenHierarchy : BindingFlags.DeclaredOnly)
			;
			return TYPE.GetMethods(bindinFlags).Any(
				m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
				BASE(m).IsAssignableFrom(DERIVED(m))
			);
		}

		//...........................................................

		/// <summary>
		/// "List'1" -> "List", "Single[]" -> "Single",
		/// "Boolean" -> "bool", ...
		/// </summary>
		public static string RootName( this Type TYPE )
		{
			if( TYPE == null ) return "";
			var name  = TYPE.Name;

			var index = name.IndexOf('`');
			if( index > 0 ) name = name.Remove(index);  // "List'1" -> "List"

			index = name.IndexOf('[');
			if( index > 0 ) name = name.Remove(index);  // "Single[]" -> "Single"

			switch( name ) {
				case "Boolean": return "bool";
				case "Char":    return "char";
				case "SByte":   return "sbyte";
				case "Int16":   return "short";
				case "Int32":   return "int";
				case "Int64":   return "long";
				case "IntPtr":  return "nint";
				case "Byte":    return "byte";
				case "UInt16":  return "ushort";
				case "UInt32":  return "uint";
				case "UInt64":  return "ulong";
				case "UIntPtr": return "nuint";
				case "Decimal": return "decimal";
				case "Single":  return "float";
				case "Double":  return "double";
				case "Object":  return "object";
				case "String":  return "string";
			}

			return name;
		}

		//...........................................................

		/// <summary>
		/// If TYPE is a field or class enum, return the parent class GenericName(FULL)
		/// else return "".
		/// </summary>
		public static string ParentClassName( this Type TYPE, bool FULL = false )
		{
			if( TYPE == null ) return "";
			var name  = TYPE.FullName.Remove(0, TYPE.Namespace.Length + 1);

			// class.field or class+enum
			var index = name.LastIndexOfAny(new[]{ '.', '+' });
			if( index < 0 ) return "";

			    name = TYPE.Namespace + "." + name.Substring(0, index);
			var type = TYPE.Assembly.GetType(name);

			return type?.GenericName(FULL) ?? "";
		}

		//...........................................................

		/// <summary>
		/// Convert TYPE Name or FullName to clean name
		/// e.g. "List'1{blah}" -> "List<blah>".
		/// </summary>
		public static string GenericName( this Type TYPE, bool FULL = false )
		{
			if( TYPE == null ) return "";
			string generic_name = "";

			if( FULL ) {
				generic_name = TYPE.Namespace + ".";
				var  parent_name = TYPE.ParentClassName(false);
				if( !parent_name.IsNullOrEmpty() ) {
					generic_name += parent_name + ".";
				}
			}
			generic_name += RootName(TYPE);

			if( TYPE.IsGenericType ) {
				generic_name += "<";
				var generics = TYPE.GetGenericArguments();
				if( generics.Length > 0 ) {
					generic_name += GenericName(generics[0], false);
					for( int i = 1; i < generics.Length; ++i ) {
						generic_name += "," + GenericName(generics[i], false);
					}
				}
				generic_name += ">";
			}
			if( TYPE.IsArray ) {
				for( var i = 0; i < TYPE.GetArrayRank(); ++i ) {
					generic_name += "[]";
				}
			}

			return generic_name;
		}
	}
}

//=============================================================================
