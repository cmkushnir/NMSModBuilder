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
		// AddUnique is already an extension for IList
		// see: cmkNMSCommon/Extensions/System.Collections.IList.x.cs
		// so make sure we put this one in same namespace (cmk)
		// or in scripts need to make sure using cmk.NMS (prefer).
		public static bool AddUnique<STRING_T>(
			this IList<STRING_T> LIST,
			           STRING_T  STRING
		)
		where STRING_T : libMBIN.NMS.INMSString  // doesn't include VariableSizeString
		{
			if( LIST == null || STRING == null ) return false;
			foreach( var item in LIST ) {
				if( string.Equals(STRING.StringValue(), item.StringValue()) ) return false;
			}
			LIST.Add(STRING);
			return true;
		}

		//.................................................

		public static bool AddUnique(
			this IList<libMBIN.NMS.VariableSizeString> LIST,
			                                   string  STRING
		){
			if( LIST == null || STRING.IsNullOrEmpty() ) return false;
			foreach( var item in LIST ) {
				if( string.Equals(STRING, item.Value) ) return false;
			}
			LIST.Add(STRING);
			return true;
		}
	}
}

//=============================================================================

namespace cmk.NMS
{
	// partial: index all classes, enums, fields in an instance
	public partial class MBINC
	{
		public class TypeInfo
		: System.IComparable<TypeInfo>
		, System.IEquatable<TypeInfo>
		{
			public string Name     { get; protected set; }  // type
			public string WrapName { get; protected set; }  // class.type
			public string FullName { get; protected set; }  // namespace.class.type

			// NMSAttribute:
			public readonly int      AttrSize         = 0;
			public readonly bool     AttrIgnore       = false;
			public readonly object   AttrDefaultValue = null;
			public readonly string[] AttrEnumValue    = null;
			public readonly Type     AttrEnumType     = null;
			public readonly byte     AttrPadding      = 0;
			public readonly int      AttrAlignment    = 0;
			public readonly ulong    AttrGUID         = 0;
			public readonly ulong    AttrNameHash     = 0;
			public readonly bool     AttrBroken       = false;
			public readonly bool     AttrIDField      = false;

			public TypeInfo( MBINC MBINC, MemberInfo MEMBER_INFO )
			{
				if( MBINC == null || MEMBER_INFO == null ) return;

				dynamic attr = MEMBER_INFO.GetCustomAttribute(MBINC.NMSAttributeType);
				if( attr == null ) return;

				if( MBINC.HasAttrSize )         AttrSize         = attr.Size;
				if( MBINC.HasAttrIgnore )       AttrIgnore       = attr.Ignore;
				if( MBINC.HasAttrDefaultValue ) AttrDefaultValue = attr.DefaultValue;
				if( MBINC.HasAttrEnumValue )    AttrEnumValue    = attr.EnumValue;
				if( MBINC.HasAttrEnumType )     AttrEnumType     = attr.EnumType;
				if( MBINC.HasAttrPadding )      AttrPadding      = attr.Padding;
				if( MBINC.HasAttrAlignment )    AttrAlignment    = attr.Alignment;
				if( MBINC.HasAttrGUID )         AttrGUID         = attr.GUID;
				if( MBINC.HasAttrNameHash )     AttrNameHash     = attr.NameHash;
				if( MBINC.HasAttrBroken )       AttrBroken       = attr.Broken;
				if( MBINC.HasAttrIDField )      AttrIDField      = attr.IDField;

				if( AttrSize < 1 && MEMBER_INFO is System.Reflection.FieldInfo field_info &&
					field_info.FieldType.IsArray
				) {
					// would be an error, all array fields w/ enum should also have size
					var fake_enum = MBINC.NMSAttributeEnumType(field_info.FieldType.Name, attr);
					AttrSize = fake_enum?.Values.Length ?? 0;
				}
			}

			public static Predicate<object> CreateFilter( Regex REGEX )
			{
				return REGEX == null ? null : new(INFO => {
					var info  = INFO as TypeInfo;
					if( info == null ) return false;
					return REGEX.IsMatch(info.Name);
				});
			}

			public int CompareTo( TypeInfo RHS )
			{
				var            cmp = string.Compare(Name,     RHS?.Name);
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
			public readonly FakeEnum  Fake;
			public          int       Count => Fake.Values.Length;

			public readonly List<ClassInfo> Classes = new();

			public FontWeight FontWeight
			=> Fake.IsMask ? FontWeights.Bold : FontWeights.Normal;

			public EnumInfo( ClassInfo PARENT, Type TYPE )
			: base(PARENT?.Mbinc, TYPE as MemberInfo)
			{
				Parent = PARENT;
				Type   = TYPE;
				Fake   = new(Type);

				Counts.AddUnique(Count);

				Name     = Type.Name;
				WrapName = (Parent == null) ? Name          : Parent.Name     + "." + Name;
				FullName = (Parent == null) ? Type.FullName : Parent.FullName + "." + Name;
			}

			public static Predicate<object> CreateFilter( Regex REGEX, int COUNT )
			{
				return new(INFO => {
					var info  = INFO as EnumInfo;
					if( info == null ) return false;

					if( COUNT > 0 && COUNT != info.Count ) return false;
					if( REGEX == null ) return true;  // just match COUNT

					if( REGEX.IsMatch(info.Name) ) return true;
					foreach( var value in info.Fake.Values ) {
						if( REGEX.IsMatch(value.Name) ) return true;
					}

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
			public readonly List<FieldInfo> Fields   = new();
			public readonly List<ClassInfo> Classes  = new();  // parent classes
			public readonly List<string>    PakItems = new();  // mbin paths that have this as a top-level object, only set if this a game MbincVersion

			public FontWeight FontWeight
			=> PakItems.IsNullOrEmpty() ? FontWeights.Normal : FontWeights.Bold;

			public ClassInfo( MBINC MBINC, Type TYPE )
			: base(MBINC, TYPE as MemberInfo)
			{
				Mbinc = MBINC;
				Type  = TYPE;

				Name     = Type.GenericName(false);
				WrapName = Name;
				FullName = Type.GenericName(true);

				// LoadFields(); have to wait until after enum's loaded
			}

			public void LoadFields()
			{
				Fields.Clear();
				var fields = Type.GetFields();
				Fields.Capacity = fields.Length;
				foreach( var field in fields ) {
					if( field.IsPublic &&
						!field.Name.Contains("padding", StringComparison.CurrentCultureIgnoreCase)
					)	Fields.Add(new(this, field));
				}
			}

			new public static Predicate<object> CreateFilter( Regex REGEX )
			{
				return REGEX == null ? null : new(INFO => {
					var info  = INFO as ClassInfo;
					if( info == null ) return false;

					if( REGEX.IsMatch(info.Name) ) return true;
					foreach( var field in info.Fields ) {
						if( REGEX.IsMatch(field.Name) ) return true;
					}

					return false;
				});
			}

			/// <summary>
			/// Generate mappings from this ClassInfo to TO.
			/// Dictionary Key is this field name, Value to TO field name or empty.
			/// todo: base for generating .cs scripts from mod pak's.
			///       this would be mod mbinc, TO would be game mbinc.
			///       mappings would then be used to describe how to go from a given
			///       mod mbin field to the corresponding game mbin field.
			///       more than a naive field name matching, try to map fields
			///       w/ slight name changes using soundex and levenshtein values
			///       and in same order i.e. we assume fields are added, removed,
			///       renamed, but not reordered.
			/// </summary>
			public Dictionary<string, string> GenerateMappings( ClassInfo TO )
			{
				var map  = new Dictionary<string,string>();
				var from =    Type.GetFields();
				var to   = TO.Type.GetFields();

				// assume field order does not change, fields only inserted, deleted, renamed
				int fi = 0,  // current index in from
				    ti = 0,  // current index in to
					mi = 0;  // last matching ti

				for( ; fi < from.Length; ++fi ) {
					var fn = from[fi].Name;
					for( ti = mi; ti < to.Length; ++ti ) {
						var tn = to[ti].Name;
						if( string.Equals(fn, tn) || (       // names the same
							fn.Soundex() == tn.Soundex() &&  // basically the same
							fn.Levenshtein(tn) <= 2          // only a minor change
						) ) {
							mi = ++ti;
							break;
						}
					}
					if( ti < to.Length ) map.Add(fn, to[ti].Name);
					else map.Add(fn, "");
				}

				return map;
			}
		}

		//=====================================================================

		public class FieldInfo
		: cmk.NMS.MBINC.TypeInfo
		{
			public string TypeGenericName { get; protected set; }
			public string TypeAndName     { get; protected set; }
			public string TypeAndWrapName { get; protected set; }
			public string TypeAndFullName { get; protected set; }
			public readonly ClassInfo                   Parent;
			public readonly System.Reflection.FieldInfo Info;
			public readonly EnumInfo                    Enum;  // if field is enum value or array indexed by enum
			public readonly FakeEnum                    Fake;  // Enum.Fake or field specific FakeEnum 

			public FieldInfo( ClassInfo PARENT, System.Reflection.FieldInfo INFO )
			: base(PARENT.Mbinc, INFO)
			{
				Parent = PARENT;
				Info   = INFO;

				Name     = Info.Name.TrimEnd('[', ']');   // remove any trailing array "field[]"
				WrapName = Parent.Name     + "." + Name;  // class.field
				FullName = Parent.FullName + "." + Name;  // namespace.class.field

				TypeGenericName = Info.FieldType.GenericName(false);

				Enum = PARENT.Mbinc.FindEnum(AttrEnumType?.Name);
				Fake = Enum?.Fake;

				// old mbinc didn't have an actual enum type, had string[]
				if( Fake == null && !AttrEnumValue.IsNullOrEmpty() ) {
					Fake  = new(Name, AttrEnumValue);
				}

				if( AttrSize > 0 && TypeGenericName.Contains("[]") ) {
					if( Enum == null ) TypeGenericName = TypeGenericName.Replace("[]", $"[{AttrSize}]");
					else               TypeGenericName = TypeGenericName.Replace("[]", $"[{AttrSize}:{Enum.Name}]");
				}

				TypeAndName     = TypeGenericName + "  " + Name;
				TypeAndWrapName = TypeGenericName + "  " + WrapName;
				TypeAndFullName = TypeGenericName + "  " + FullName;
			}

			new public static Predicate<object> CreateFilter( Regex REGEX )
			{
				return REGEX == null ? null : new(INFO => {
					var info  = INFO as FieldInfo;
					if( info == null ) return false;
					return REGEX.IsMatch(info.Name);
				});
			}
		}

		//=====================================================================

		public readonly List<EnumInfo>  Enums   = new( 2000);  // 4.00 -    959
		public readonly List<ClassInfo> Classes = new( 3000);  // 4.00 -  2,156
		public readonly List<FieldInfo> Fields  = new(20000);  // 4.00 - 18,154

		//...........................................................

		protected void LoadTypes()
		{
			if( Fields.Count > 0 ) return;

			var path    = ".\\" + System.IO.Path.GetRelativePath(Resource.AppDirectory, Assembly.Location);
			var version = Assembly.GetName().Version.Normalize().NMSMbincString();

			Log.Default.AddInformation(
				$"Loading Types from {path} {version}"
			);

			foreach( var type in Assembly.GetExportedTypes() ) {
				if( type == NMSTemplateType ||
					type.IsSubclassOf(NMSTemplateType)
				)	Classes.Add(new(this, type));  // does not load fields
			}
			Classes.Sort();

			foreach( var type in Assembly.GetExportedTypes() ) {
				if( !type.IsEnum ) continue;
				var class_name = type.ParentClassName(false);
				var class_info = FindClass(class_name);
				Enums.Add(new(class_info, type));
			}
			Parallel.Invoke(
				() => Enums          .Sort(),
				() => EnumInfo.Counts.Sort()
			);

			Classes.ForEach(CLASS => {
				CLASS.LoadFields();  // links field to enum as required
				Fields.AddRange(CLASS.Fields);
			});
			Fields.Sort();

			LinkTypes();

			Log.Default.AddInformation(
				$"Loaded Types from {path} {version}: {Enums.Count} enum's, {Classes.Count} classes, {Fields.Count} fields"
			);
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
				var enum_info  = FindEnum(TYPE.Name);
				if( enum_info != null &&
					enum_info.Parent != PARENT
				)	enum_info.Classes.AddUnique(PARENT);
			}
			else {
				var class_info  = FindClass(TYPE.Name.Trim('[', ']'));
				if( class_info != null ) class_info.Classes.AddUnique(PARENT);
			}
		}

		//...........................................................

		public ClassInfo FindClass( string NAME )
		{
			return NAME == null ? null : Classes.Bsearch(NAME,
				(ITEM, KEY) => string.Compare(ITEM.Name, KEY)
			);
		}

		//...........................................................

		public EnumInfo FindEnum( string NAME )
		{
			return NAME == null ? null : Enums.Bsearch(NAME,
				(ITEM, KEY) => string.Compare(ITEM.Name, KEY)
			);
		}

		//...........................................................

		public IEnumerable<FieldInfo> FindField( string NAME )
		{
			return NAME == null ? null : Fields.FindAll<FieldInfo>(INFO =>
				string.Equals(INFO.Name, NAME)
			);
		}
	}
}

//=============================================================================
