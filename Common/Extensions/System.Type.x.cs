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
using System.Collections.Generic;
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
			Type                   TYPE,
			Func<MethodInfo, Type> BASE,
			Func<MethodInfo, Type> DERIVED,
			bool                   LOOK_IN_BASE
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

		//...........................................................

		/// <summary>
		/// Get private backing field for property NAME, looks through
		/// TYPE and all its base classes.
		/// e.g. public Blah { get; private set; }
		/// type.GetPrivateFields("Blah"); will get backing field "<Blah>k__BackingField"
		/// Private get|set that are only used via reflection may be optimized away,
		/// the only way to then used them is via their backing field.
		/// </summary>
		public static FieldInfo GetBackingField( this Type TYPE,
			string NAME
		){
			var backing = $"<{NAME}>k__BackingField";
			for( var type = TYPE; type != null; type = type.BaseType ) {
				foreach( var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic) ) {
					if( string.Equals(field.Name, backing) ) return field;
				}
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Get private fields in TYPE and all its base classes.
		/// Also finds backing fields using normal field name.
		/// e.g. public Blah { get; private set; }
		/// type.GetPrivateFields("Blah"); will get backing field "<Blah>k__BackingField"
		/// Private get|set that are only used via reflection may be optimized away,
		/// the only way to then used them is via their backing field.
		/// </summary>
		public static IEnumerable<FieldInfo> GetPrivateFields( this Type TYPE,
			string NAME = null
		){
			var backing = $"<{NAME}>k__BackingField";
			for( var type = TYPE; type != null; type = type.BaseType ) {
				foreach( var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic) ) {
					if( !field.IsPrivate ) continue;
					if( NAME == null ||
						field.Name == NAME ||
						field.Name == backing
					)	yield return field;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Loop through all TYPE.GetMethods(),
		/// return first with matching NAME, has all FLAGS, and initial PARAMS
		/// where any remaining params have default values.
		/// This is technically to get around coding errors where
		/// there are ambiguous matches, but also helps protect
		/// against code churn where new param are added to methods
		/// w/ default values, the PARAMS are the ones we will use.
		/// </summary>
		public static MethodInfo FindMethod( this Type TYPE,
			string NAME, params Type [] PARAMS
		){
			foreach( var method in TYPE.GetMethods() ) {
				if( !string.Equals(method.Name, NAME)
				)	continue;

				var method_params = method.GetParameters();
				if( method_params.Length < PARAMS.Length ) continue;

				MethodInfo found = method;

				// first check that all PARAMS match
				for( var i = 0; i < PARAMS.Length; ++i ) {
					if( PARAMS[i].TypeHandle != (object)method_params[i].ParameterType.TypeHandle ) {
						found = null;
						break;
					}
				}

				// then check that any remaining params have default values
				for( var i = PARAMS.Length; i < method_params.Length; ++i ) {
					if( !method_params[i].HasDefaultValue ) {
						found = null;
						break;
					}
				}

				if( found != null ) return found;
			}
			return null;
		}
	}
}

//=============================================================================
