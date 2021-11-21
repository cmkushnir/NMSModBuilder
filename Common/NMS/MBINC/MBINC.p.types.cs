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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

//=============================================================================

namespace cmk.NMS
{
	public static partial class _x_
	{
		public static bool AddUnique<STRING_T>(
			this IList<STRING_T> LIST,
			STRING_T OBJECT
		)
		where STRING_T : libMBIN.NMS.INMSString  // doesn't include VariableString
		{
			if( LIST == null || OBJECT == null ) return false;
			foreach( var item in LIST ) {
				if( OBJECT.StringValue().Equals(item.StringValue()) ) return false;
			}
			LIST.Add(OBJECT);
			return true;
		}
	}

	//=========================================================================

	// partial: index all classes, enums, fields in an instance
	public partial class MBINC
	{
		public class TypeInfo
		: System.IComparable<TypeInfo>
		, System.IEquatable <TypeInfo>
		{
			public string Name     { get; protected set; }  // type
			public string WrapName { get; protected set; }  // class.type
			public string FullName { get; protected set; }  // namespace.class.type

			public static Predicate<object> CreateFilter( Regex REGEX )
			{
				return REGEX == null ? null : new(INFO => {
					var info  = INFO as TypeInfo;
					if( info == null ) return false;

					try  { return REGEX.IsMatch(info.Name); }
					catch( Exception EX ) {
						Log.Default.AddFailure(EX);
						return false;
					}
				});
			}

			public int CompareTo( TypeInfo RHS )
			{
				var cmp  = string.Compare(Name, RHS?.Name);
				if( cmp == 0 ) cmp = string.Compare(FullName, RHS?.FullName);
				return cmp;
			}
			public bool Equals( TypeInfo RHS ) => string.Equals(FullName, RHS?.FullName);

			public override bool   Equals( object RHS ) => Equals(RHS as TypeInfo);
			public override int    GetHashCode()        => FullName.GetHashCode();
			public override string ToString()           => WrapName;
		}

		//=====================================================================

		public class EnumInfo
		: cmk.NMS.MBINC.TypeInfo
		{
			public static readonly List<int> Counts = new();

			public readonly ClassInfo Parent;
			public readonly Type      Type;
			public readonly FakeEnum  Enum;
			public          int       Count { get { return Enum.Values.Length; } }

			public readonly List<ClassInfo> Classes = new();

			public EnumInfo( ClassInfo PARENT, Type TYPE )
			{
				Parent = PARENT;
				Type   = TYPE;
				Enum   = new(Type);

				Counts.AddUnique(Count);

				Name     = Type.Name + " [" + Count + "]";
				WrapName = (Parent == null) ? Name                               : Parent.Name     + "." + Name;
				FullName = (Parent == null) ? Type.FullName + " [" + Count + "]" : Parent.FullName + "." + Name;
			}

			public static Predicate<object> CreateFilter( Regex REGEX, int COUNT )
			{
				return new(INFO => {
					var info  = INFO as EnumInfo;
					if( info == null ) return false;

					if( COUNT > 0 && COUNT != info.Count ) return false;
					if( REGEX == null ) return true;  // just match COUNT

					try {
						if( REGEX.IsMatch(info.Name) ) return true;
						foreach( var name in info.Type.GetEnumNames() ) {
							if( REGEX.IsMatch(name) ) return true;
						}
					}
					catch( Exception EX ) { Log.Default.AddFailure(EX); }

					return false;
				});
			}
		}

		//=====================================================================

		public class ClassInfo
		: cmk.NMS.MBINC.TypeInfo
		{
			public readonly MBINC           Mbinc;
			public readonly Type            Type;
			public readonly ulong           NMSAttributeGUID = 0;
			public readonly List<ClassInfo> Classes  = new();
			public readonly List<string>    PakItems = new();  // only set if this a game MbincVersion
			public          FontWeight      FontWeight { get{ return PakItems.IsNullOrEmpty() ? FontWeights.Normal : FontWeights.Bold; } }

			public ClassInfo( MBINC MBINC, Type TYPE )
			{
				Mbinc = MBINC;
				Type  = TYPE;

				if( Mbinc.HasNMSAttributeGUID ) {
					dynamic attr = Type.GetCustomAttribute(Mbinc.NMSAttributeType);
					NMSAttributeGUID = (ulong)(attr?.GUID ?? 0);
				}

				Name     = Type.GenericName(false);
				WrapName = Name;
				FullName = Type.GenericName(true);
			}
		}

		//=====================================================================

		public class FieldInfo
		: cmk.NMS.MBINC.TypeInfo
		{
			public readonly ClassInfo                   Parent;
			public readonly string                      TypeGenericName;
			public readonly string                      TypeAndName;
			public readonly System.Reflection.FieldInfo Info;

			public FieldInfo( ClassInfo PARENT, System.Reflection.FieldInfo INFO )
			{
				Parent = PARENT;
				Info   = INFO;

				Name     = Info.Name;
				WrapName = Parent.Type.GenericName(false) + "." + Name;
				FullName = Parent.Type.GenericName(true)  + "." + Name;

				TypeGenericName = Info.FieldType.GenericName(false);
				TypeAndName     = TypeGenericName + "  " + Info.Name;
			}

			new public static Predicate<object> CreateFilter( Regex REGEX )
			{
				return REGEX == null ? null : new(INFO => {
					var info  = INFO as FieldInfo;
					if( info == null ) return false;

					try  { return REGEX.IsMatch(info.TypeAndName); }
					catch( Exception EX ) {
						Log.Default.AddFailure(EX);
						return false;
					}
				});
			}
		}

		//=====================================================================

		public readonly List<EnumInfo>  Enums   = new(  500);
		public readonly List<ClassInfo> Classes = new( 2000);
		public readonly List<FieldInfo> Fields  = new(20000);

		//...........................................................

		protected void LoadTypes()
		{
			if( Fields.Count > 0 ) return;

			Log.Default.AddInformation(
				$"Loading Types from {Assembly.Location}"
			);

			foreach( var type in Assembly.GetExportedTypes() ) {
				if( type.IsSubclassOf(NMSTemplateType) ) {
					var class_info = new ClassInfo(this, type);
					Classes.Add(class_info);

					var fields = type.GetFields();
					Fields.Capacity += fields.Length;

					foreach( var field in fields ) {
						if( field.IsPublic &&
							!field.Name.Contains("padding", StringComparison.CurrentCultureIgnoreCase)
						)	Fields.Add(new(class_info, field));
					}
				}
			}

			Parallel.Invoke(
				() => Classes.Sort(),
				() => Fields .Sort()
			);

			LoadEnums();  // need sorted Classes to get Parent

			Parallel.Invoke(
				() => Enums.Sort(),
				() => EnumInfo.Counts.Sort(),
				() => LinkTypes()
			);

			Log.Default.AddInformation(
				$"Loaded Types from {Assembly.Location}"
			);
		}

		//...........................................................

		private void LoadEnums()
		{
			foreach( var type in Assembly.GetExportedTypes() ) {
				if( !type.IsEnum ) continue;
				var class_name = type.ParentClassName(false);
				var class_info = Classes.Find(class_name,
					(LHS, RHS) => string.Compare(LHS.Type.Name, RHS)
				);
				Enums.Add(new(class_info, type));
			}
		}

		//...........................................................

		private void LinkTypes()
		{
			foreach( var parent_info in Classes ) {
				foreach( var field in parent_info.Type.GetFields() ) {
					if( field.FieldType.IsGenericType ) {
						// e.g. List<GcBlah>, link GcBlah to parent_info
						foreach( var generic_type in field.FieldType.GenericTypeArguments ) {
							LinkField(parent_info, generic_type);
						}
					}
					else {
						LinkField(parent_info, field.FieldType);
					}
				}
			}
		}

		//...........................................................

		private void LinkField( ClassInfo PARENT, Type TYPE )
		{
			if( TYPE.IsEnum ) {
				var enum_info = Enums.Find(TYPE.Name,
					(LHS, RHS) => string.Compare(LHS.Type.Name, RHS)
				);
				if( enum_info != null &&
					enum_info.Parent != PARENT
				){
					enum_info.Classes.AddUnique(PARENT);
				}
			}
			else {
				var class_info = Classes.Find(TYPE.Name,
					(LHS, RHS) => string.Compare(LHS.Type.Name, RHS)
				);
				if( class_info != null ) {
					class_info.Classes.AddUnique(PARENT);
				}
			}
		}

		//...........................................................

		public ClassInfo FindClass( string NAME )
		{
			return Classes.Find(NAME, (LHS, RHS) => string.Compare(LHS.Type.Name, RHS));
		}

		//...........................................................

		public EnumInfo FindEnum( string NAME )
		{
			return Enums.Find(NAME, (LHS, RHS) => string.Compare(LHS.Type.Name, RHS));
		}

		//...........................................................

		public IEnumerable<FieldInfo> FindField( string NAME )
		{
			return Fields.FindAll<FieldInfo>(INFO => string.Compare(INFO.Name, NAME) == 0);
		}
	}
}

//=============================================================================
