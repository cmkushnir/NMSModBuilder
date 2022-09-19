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
using System.Collections;
using System.Reflection;
using System.Text;

//=============================================================================

namespace cmk.NMS
{
    // partial: convert from dom to ebin
    public partial class MBINC
	{
		/// <summary>
		/// Convert a .pak .mbin item to a .ebin string for viewing or saving to disk.
		/// </summary>
		/// <param name="PATH">.pak item Path to the .mbin we are converting from.</param>
		/// <param name="MBIN">Top-level object.</param>
		/// <returns>Similar to EXML, a string representation of the MBIN at PATH.</returns>
		public string MbinToEbin( string PATH, object MBIN, Log LOG = null )
		{
			if( MBIN == null ) return null;

			var builder = new StringBuilder(600 * 1024);

			builder.AppendLine($"EBIN:{Version}");
			builder.AppendLine(PATH ?? "<unknown path>");

			// kick-off recusrive conversion
			MbinToEbin("", "", MBIN.GetType(), MBIN, null, builder, "", LOG);

			return builder.ToString();
		}

		//...........................................................

		/// <summary>
		/// Recursively convert and emit .mbin data to string BUILDER.
		/// In general,
		/// - if VALUE is an Array|List element then show "[...] VALUE", else show "NAME = VALUE"
		/// - if VALUE is an Array|List show "NAME TYPE[size]" or "NAME List<TYPE>[size]"
		/// - If VALUE is associated w/ an Enum then make sure the Enum value is written.
		/// </summary>
		/// <param name="INDEX">If VALUE is an Array or List item this is the "[...]" to display at the start of the line.</param>
		/// <param name="NAME">Name of the field.</param>
		/// <param name="TYPE">FieldInfo Type from parent class, or element type from parent Array or List.</param>
		/// <param name="VALUE"></param>
		/// <param name="ENUM">
		/// Parent checks if field has NMSAttribute, if so it passes NMSAttribute.EnumType with field VALUE.
		/// Only set by NMSTemplate VALUEs, mainly used by Array VALUEs;
		/// that is, the NMSTemplate found an Array field with an NMSAttribute associated Enum.
		/// </param>
		/// <param name="BUILDER">Keep adding strings to this.</param>
		/// <param name="INDENT">Indentation string for current recursion depth e.g. "\t\t\t".</param>
		protected void MbinToEbin( string INDEX, string NAME, Type TYPE, object VALUE, FakeEnum ENUM, StringBuilder BUILDER, string INDENT, Log LOG )
		{
			if( VALUE != null ) TYPE = VALUE.GetType();  // TYPE may be NMSTemplate, get real type

			if( TYPE.GenericTypeArguments.Length > 0 ) {  // List<?>, get ?
				TYPE = VALUE.GetType().GenericTypeArguments[0];
			}
			if( TYPE.HasElementType ) {  // Array[?], get ?
				TYPE = TYPE.GetElementType();
			}

			var index = !string.IsNullOrWhiteSpace(INDEX);  // is VALUE an Array | List item
			var head  = INDENT + INDEX;

			// rarely there will be null NMS field values
			if( VALUE == null ) {
				if( index ) BUILDER.AppendLine($"{head} <null>");
				else        BUILDER.AppendLine($"{head}{NAME} = <null> {TYPE.Name}");
				return;
			}

			// try to convert NMSString* and VariableSizeString to normal string and process.
			if( TryMbinStringToEbin(index, head, NAME, VALUE, BUILDER) ) return;

			// try to handle NMSTemplate derived classes (other than strings).
			if( TryMbinNMSTemplateToEbin(index, head, NAME, VALUE, BUILDER, INDENT, LOG) ) return;

			var indent    = INDENT + '\t';    // for children
			var type_name = TYPE.RootName();  // for type_name[#] and List<type_name>[#]

			switch( VALUE ) {
				case byte[] bytes_v:
					BUILDER.AppendLine($"{head}{NAME} = [[{Convert.ToBase64String(bytes_v)}]] byte[{bytes_v.Length}]");
					break;

				case Array array_v:
					if( ENUM != null ) {
						BUILDER.AppendLine($"{head}{NAME} {type_name}[{array_v.GetLength(0)}:{ENUM.Name}]");
						for( var i = array_v.GetLowerBound(0); i <= array_v.GetUpperBound(0); ++i ) {
							MbinToEbin($"[{i}:{ENUM.Values[i].Name}] ", "", TYPE, array_v.GetValue(i), null, BUILDER, indent, LOG);
						}
					}
					else {
						BUILDER.AppendLine($"{head}{NAME} {type_name}[{array_v.GetLength(0)}]");
						for( var i = array_v.GetLowerBound(0); i <= array_v.GetUpperBound(0); ++i ) {
							MbinToEbin($"[{i}] ", "", TYPE, array_v.GetValue(i), null, BUILDER, indent, LOG);
						}
					}
					break;

				case IList list_v:
					// there are Lists that use enum indexes, but the enum isn't linked to the List in MbincVersion.
					BUILDER.AppendLine($"{head}{NAME} List<{type_name}>[{list_v.Count}]");
					for( var i = 0; i < list_v.Count; ++i ) {
						MbinToEbin($"[{i}] ", "", TYPE, list_v[i], null, BUILDER, indent, LOG);
					}
					break;

				default:  // not nms type, not array|list type e.g. pod types
					if( index ) BUILDER.AppendLine($"{head}{VALUE}");
					else {
						if( TYPE.IsEnum ) BUILDER.AppendLine($"{head}{NAME} = {type_name}:{VALUE}");
						else BUILDER.AppendLine($"{head}{NAME} = {VALUE}");
					}
					break;
			}
		}

		//...........................................................

		/// <summary>
		/// Emit .NET or .mbin string type to string BUILDER.
		/// </summary>
		protected bool TryMbinStringToEbin( bool INDEX, string HEAD, string NAME, object VALUE, StringBuilder BUILDER )
		{
			string nms_string = IsString(VALUE);  // return null if not string, "" if is string
			if( nms_string == null ) return false;

			// handles indentation issue, but now can't select entire string w/o getting artifical embedded tabs
			//nms_string = nms_string.Replace("\r\n", "\n").Replace("\n", $"\n{HEAD}");

			if( INDEX ) BUILDER.AppendLine($"{HEAD}\"{nms_string}\"");
			else BUILDER.AppendLine($"{HEAD}{NAME} = \"{nms_string}\"");

			return true;
		}

		//...........................................................

		/// <summary>
		/// Emit NMSTemplate type to string BUILDER.
		/// </summary>
		protected bool TryMbinNMSTemplateToEbin( bool INDEX, string HEAD, string NAME, object VALUE, StringBuilder BUILDER, string INDENT, Log LOG )
		{
			var type      = VALUE.GetType();
			var type_name = type.Name;

			// some fields that are obviously colors use Vector4 type
			if( type_name == "Vector4f" && NAME.Contains("Colour") ) {
				type_name  = "Colour";
			}

			var head = HEAD;
			if( !INDEX ) head = HEAD + NAME + " = ";

			// flatten simple|vector types
			switch( type_name ) {
				case "GcSeed": {
					dynamic seed = VALUE;
					BUILDER.AppendLine($"{head}0x{seed.Seed:x16} {seed.UseSeedValue}");
					return true;
				}
				case "Colour": {  // wrap with (...)
					dynamic col = VALUE;
					BUILDER.AppendLine($"{head}({col.R:0.000}, {col.G:0.000}, {col.B:0.000}, {col.A:0.000})");
					return true;
				}
				case "Vector2f": {  // wrap with [...]
					dynamic vec = VALUE;
					BUILDER.AppendLine($"{head}[{vec.x}, {vec.y}]");
					return true;
				}
				case "Vector3f": {
					dynamic vec = VALUE;
					BUILDER.AppendLine($"{head}[{vec.x}, {vec.y}, {vec.z}]");
					return true;
				}
				case "Vector4f": {
					dynamic vec = VALUE;
					BUILDER.AppendLine($"{head}[{vec.x}, {vec.y}, {vec.z}, {vec.t}]");
					return true;
				}
				case "Quaternion": {  // wrap with |...|
					dynamic quat = VALUE;
					BUILDER.AppendLine($"{head}|{quat.x}, {quat.y}, {quat.z}, {quat.w}|");
					return true;
				}
			}

			if( type == NMSTemplateType || type.IsSubclassOf(NMSTemplateType) ) {
				if( NAME.IsNullOrEmpty() ) BUILDER.AppendLine($"{HEAD}{type_name}");
				else BUILDER.AppendLine($"{HEAD}{NAME} {type_name}");

				var indent = INDENT + '\t';
				var is_localization = (type_name == "TkLocalisationEntry");

				foreach( var field in type.GetFields() ) {
					var attrib  = field.GetCustomAttribute(NMSAttributeType);
					if( attrib != null && HasNMSAttributeIgnore ) {
						if( ((dynamic)attrib).Ignore ) continue;
					}

					var value = field.GetValue(VALUE);

					// skip blank language strings
					if( is_localization && IsString(value).IsNullOrEmpty() ) continue;

					var fake_enum = NMSAttributeEnumType(field.Name, attrib);

					MbinToEbin("", field.Name, field.FieldType, value, fake_enum, BUILDER, indent, LOG);
				}
				return true;
			}

			return false;
		}
	}
}

//=============================================================================
