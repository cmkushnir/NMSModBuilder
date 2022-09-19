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
using System.IO;
using System.Linq;
using System.Reflection;

//=============================================================================

namespace cmk.NMS
{
    /// <summary>
    /// Wrapper around a libMBIN.dll or MBINCompiler.exe instance.
    /// </summary>
    public partial class MBINC
	{
		public readonly bool     Valid                    = false;  // all expected types & methods available
		public readonly bool     HasNMSAttributeEnumType  = false;
		public readonly bool     HasNMSAttributeEnumValue = false;
		public readonly bool     HasNMSAttributeSize      = false;
		public readonly bool     HasNMSAttributeGUID      = false;
		public readonly bool     HasNMSAttributeIgnore    = false;
		public readonly Assembly Assembly;

		public readonly Type
			MBINFileType,
			MBINHeaderType,
			NMSTemplateType,
			EXmlFileType,
			NMSAttributeType
		;

		protected readonly MethodInfo
			m_NMSTemplate_DeserializeBinaryTemplate,
			m_EXmlFile_WriteTemplate,
			m_EXmlFile_ReadTemplateFromString
		;

		//...........................................................

		public MBINC( Assembly ASSEMBLY )
		{
			Assembly = ASSEMBLY;
			if( Assembly == null ) {
				NMSTemplateTypes = new();
				return;
			}

			// libMBIN | MBINCompiler has:
			// Type GetTemplateType(string)
			// NMTemplate TemplateFromName(string)
			// but GetTemplateType is not in all versions
			// and TemplateFromName doesn't accept arguments.

			// since 1.59.0.2
			MBINFileType     = Assembly.GetType("libMBIN.MBINFile");
			EXmlFileType     = Assembly.GetType("libMBIN.EXmlFile");
			MBINHeaderType   = Assembly.GetType("libMBIN.MBINHeader");
			NMSTemplateType  = Assembly.GetType("libMBIN.NMSTemplate");
			NMSAttributeType = Assembly.GetType("libMBIN.NMSAttribute");

			// but earlier ... 1.38.0, libMBIN created, static linked to MBINCompiler until 1.53.0.2
			if( MBINFileType     == null ) MBINFileType     = Assembly.GetType("MBINCompiler.MBINFile");
			if( EXmlFileType     == null ) EXmlFileType     = Assembly.GetType("MBINCompiler.EXmlFile");
			if( MBINHeaderType   == null ) MBINHeaderType   = Assembly.GetType("libMBIN.Models.MBINHeader");
			if( NMSTemplateType  == null ) NMSTemplateType  = Assembly.GetType("libMBIN.Models.NMSTemplate");
			if( NMSAttributeType == null ) NMSAttributeType = Assembly.GetType("libMBIN.Models.NMSAttribute");

			// in the beginning ... 1.13.2
			if( MBINHeaderType   == null ) MBINHeaderType   = Assembly.GetType("MBINCompiler.Models.MBINHeader");
			if( NMSTemplateType  == null ) NMSTemplateType  = Assembly.GetType("MBINCompiler.Models.NMSTemplate");
			if( NMSAttributeType == null ) NMSAttributeType = Assembly.GetType("MBINCompiler.Models.NMSAttribute");

			if( NMSAttributeType != null ) {
				var fields = NMSAttributeType.GetRuntimeFields();
				foreach( var field in fields ) {
					if( field.Name.Contains("EnumType") ) {
						HasNMSAttributeEnumType = true;  // else may have string [] EnumValue
					}
					else if( field.Name.Contains("EnumValue") ) {
						HasNMSAttributeEnumValue = true;
					}
					else if( field.Name.Contains("Length") ) {
						HasNMSAttributeSize = true;
					}
					else if( field.Name.Contains("GUID") ) {
						HasNMSAttributeGUID = true;
					}
					else if( field.Name.Contains("Ignore") ) {
						HasNMSAttributeIgnore = true;
					}
				}
			}

			// some churn in mbinc code, have had cases of ambiguous overloads,
			// so first try to find single instance of method name,
			// then look for specific instance based on params we will use.

			m_NMSTemplate_DeserializeBinaryTemplate = NMSTemplateType?.FindMethod(
				"DeserializeBinaryTemplate", typeof(BinaryReader), typeof(string)
			);
			m_EXmlFile_WriteTemplate = EXmlFileType?.FindMethod(
				"WriteTemplate", NMSTemplateType
			);
			m_EXmlFile_ReadTemplateFromString = EXmlFileType?.FindMethod(
				"ReadTemplateFromString", typeof(string)
			);

			Valid =
				MBINFileType     != null &&
				EXmlFileType     != null &&
				MBINHeaderType   != null &&
				NMSTemplateType  != null &&
				NMSAttributeType != null
				&&
				m_NMSTemplate_DeserializeBinaryTemplate != null &&
				m_EXmlFile_WriteTemplate                != null &&
				m_EXmlFile_ReadTemplateFromString       != null
			;

			// NMSTemplateType names are unique across namespaces, enums are not.
			if( NMSTemplateType != null ) {
				NMSTemplateTypes = Assembly.GetExportedTypes()
					.Where(TYPE => TYPE == NMSTemplateType || TYPE.IsSubclassOf(NMSTemplateType))
					.ToDictionary(TYPE => TYPE.Name)
				;
			}
			LoadTypes();  // takes 1-2 seconds
		}

		//...........................................................

		public System.Version Version {
			get { return Assembly?.GetName().Version; }
		}

		public bool IsLinkedVersion {
			get { return Version == Linked.Version; }
		}

		/// <summary>
		/// Lookup types by (short) name i.e. no namespace.
		/// </summary>
		public readonly Dictionary<string, Type> NMSTemplateTypes;

		//...........................................................

		/// <summary>
		/// If VALUE is a string, NMSString*, or VariableSizeString
		/// return a non-null string, else return null.
		/// </summary>
		public static string IsString( object VALUE )
		{
			if( VALUE is string value ) return value ?? "";
			if( VALUE is Array || VALUE == null ) return null;

			// blah !!!
			var type_name  = VALUE.GetType().Name;
			if( type_name == "VariableSizeString" ||  // NMS
				type_name.StartsWith("NMSString")
			) {
				dynamic nms_string = VALUE;  // not good for perf
				string  nms_value  = (string)nms_string;
				return  nms_value ?? "";
			}

			return null;
		}

		//...........................................................

		public bool IsNMSTemplate<CLASS_T>()
		{
			if( !Valid ) return false;
			var    object_t = typeof(CLASS_T);
			return NMSTemplateType.IsAssignableFrom(object_t);
		}

		//...........................................................

		public bool IsNMSTemplate( object OBJECT )
		{
			if( !Valid || OBJECT == null ) return false;
			var    object_t = OBJECT.GetType();
			return NMSTemplateType.IsAssignableFrom(object_t);
		}

		//...........................................................

		/// <summary>
		/// In older versions of libMBIN|MBINCompiler NMSAttribute
		/// did not have an EnumType field, it instead used string [] EnumValue.
		/// In order to handle both cases we create a wrapper around
		/// whichever is used by this instance.
		/// FIELD_NAME    is used to create a fake enum name if needed.
		/// NMS_ATTRIBUTE must be a NMSAttribute from this MbincVersion.
		/// </summary>
		public FakeEnum NMSAttributeEnumType( string FIELD_NAME, object NMS_ATTRIBUTE )
		{
			if( !Valid || NMS_ATTRIBUTE == null ||
				!NMSAttributeType.IsAssignableFrom(NMS_ATTRIBUTE.GetType())
			)	return null;

			dynamic  nms_atribute = NMS_ATTRIBUTE;
			FakeEnum fake         = null;

			if( HasNMSAttributeEnumType ) {
				fake = new FakeEnum(nms_atribute.EnumType);
			}
			else if( HasNMSAttributeEnumValue ) {
				fake = new FakeEnum(FIELD_NAME += "Enum", nms_atribute.EnumValue);
			}
			else return null;

			return fake.Values.IsNullOrEmpty() ? null : fake;
		}

		//...........................................................

		public ulong NMSAttributeGUID( object NMS_ATTRIBUTE )
		{
			if( !Valid || NMS_ATTRIBUTE == null ||
				!HasNMSAttributeGUID ||
				!NMSAttributeType.IsAssignableFrom(NMS_ATTRIBUTE.GetType())
			)	return 0;

			dynamic nms_atribute = NMS_ATTRIBUTE;
			return  nms_atribute.GUID;
		}

		//...........................................................

		public int NMSAttributeSize( object NMS_ATTRIBUTE )
		{
			if( !Valid || NMS_ATTRIBUTE == null ||
				!HasNMSAttributeSize ||
				!NMSAttributeType.IsAssignableFrom(NMS_ATTRIBUTE.GetType())
			)	return 0;

			dynamic nms_atribute = NMS_ATTRIBUTE;
			return  nms_atribute.Size;
		}

		//...........................................................

		/// <summary>
		/// RAW contains the NMSTemplate derived CLASS bytes for the object.
		/// Assumes RAW is at start of data.
		/// Non-null return can be cast to CLASS type.
		/// </summary>
		public object RawToNMSTemplate( Stream RAW, string CLASS, Log LOG = null )
		{
			if( !Valid || RAW == null || CLASS.IsNullOrEmpty() ) return null;
			try {
				var    reader = new System.IO.BinaryReader(RAW);
				return m_NMSTemplate_DeserializeBinaryTemplate.Invoke(null, new object[] { reader, CLASS });
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX, $"{CLASS} - ");
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Convert NMSTemplate based object NMSTEMPLATE to raw stream.
		/// Returns non-resizable MemoryStream, copy it to data stream.
		/// Never returns null, may return empty MemoryStream.
		/// </summary>
		public System.IO.MemoryStream NMSTemplateToRaw( object NMSTEMPLATE, Log LOG = null )
		{
			var    bytes = NMSTemplateToBytes(NMSTEMPLATE, LOG);
			return bytes == null ? null : new(bytes);
		}

		//...........................................................

		/// <summary>
		/// Convert NMSTemplate based object NMSTEMPLATE to raw stream.
		/// Returns non-resizable MemoryStream, copy it to data stream.
		/// Never returns null, may return empty MemoryStream.
		/// </summary>
		public byte[] NMSTemplateToBytes( object NMSTEMPLATE, Log LOG = null )
		{
			if( !Valid || !IsNMSTemplate(NMSTEMPLATE) ) return null;
			try {
				dynamic nms_template = NMSTEMPLATE;
				return  nms_template.SerializeBytes();  // assumes NMSTEMPLATE compatible w/ this MBINC
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Convert .exml string to NMSTemplate based object.
		/// </summary>
		public object ExmlToNMSTemplate( string EXML, Log LOG = null )
		{
			if( !Valid || EXML.IsNullOrEmpty() ) return null;
			try {
				return m_EXmlFile_ReadTemplateFromString.Invoke(null, new object[] { EXML });
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Convert NMSTemplate based object NMSTEMPLATE to .exml string.
		/// </summary>
		public string NMSTemplateToExml( object NMSTEMPLATE, Log LOG = null )
		{
			if( !Valid || !IsNMSTemplate(NMSTEMPLATE) ) return null;
			try {
				return (string)m_EXmlFile_WriteTemplate.Invoke(null, new object[] { NMSTEMPLATE });
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Create a deep copy of NMSTemplate based object NMSTEMPLATE.
		/// </summary>
		public CLASS_T CloneNMSTemplate<CLASS_T>( CLASS_T NMSTEMPLATE, Log LOG = null )
		where  CLASS_T : class  // NMSTemplate
		{
			if( !Valid || !IsNMSTemplate(NMSTEMPLATE) ) return null;
			try {
				// NMSTemplateToRaw
				var     name         = NMSTEMPLATE.GetType().Name;
				dynamic nms_template = NMSTEMPLATE;
				byte [] bytes        = nms_template.SerializeBytes();  // assumes NMSTEMPLATE compatible w/ this MBINC
				var     stream       = new System.IO.MemoryStream(bytes);

				// RawToNMSTemplate
				var reader = new System.IO.BinaryReader(stream);
				var clone  = m_NMSTemplate_DeserializeBinaryTemplate.Invoke(null, new object[] { reader, name });

				return clone as CLASS_T;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Alloc a new mbin header and CLASS_T object.
		/// </summary>
		public System.IO.MemoryStream CreateMbinStream<CLASS_T>( Log LOG = null )
		where  CLASS_T : class, new()  // NMSTemplate
		{
			try {
				if( !Valid || !IsNMSTemplate<CLASS_T>() ) return null;

				var class_name = typeof(CLASS_T).Name;
				var stream     = new System.IO.MemoryStream();

				var header       = new PAK.MBIN.Header(class_name, Version);
				header.Format    = PAK.MBIN.HeaderFormat.V2;
				header.ClassName = class_name;

				dynamic obj        = new CLASS_T();
				byte [] obj_bytes  = obj.SerializeBytes();
				var     obj_stream = new System.IO.MemoryStream(obj_bytes);

				header.SaveTo(stream);
				obj_stream.WriteTo(stream);

				return stream;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return null;
			}
		}

		//...........................................................

		/// <summary>
		/// Find all types that have a field of type NAME.
		/// </summary>
		public IEnumerable<Type> FindContainingTypes( string NAME )
		{
			if( Assembly != null && !NAME.IsNullOrEmpty() ) {
				var index = NAME.LastIndexOf('.');
				if( index >= 0 ) NAME = NAME.Substring(++index);

				foreach( var type in Assembly.ExportedTypes ) {
					foreach( var field in type.GetFields() ) {
						if( field.FieldType.GenericName(false) == NAME ) {
							yield return type;
							break;
						}
					}
				}
			}
		}

		//...........................................................

		/// <summary>
		/// NAME can be short (w/o namespace) or long (w/ namespace) name.
		/// </summary>
		public Type FindNMSTemplateType( string NAME )
		{
			if( NAME.IsNullOrEmpty() ) return null;

			var index  = NAME.LastIndexOf('.');
			if( index >= 0 ) NAME = NAME.Substring(++index);

			Type type = null;
			return NMSTemplateTypes.TryGetValue(NAME, out type) ?
				type : null
			;
		}

		//...........................................................

		/// <summary>
		/// NAME can be short (w/o namespace) or long (w/ namespace) name.
		/// </summary>
		public Attribute FindNMSTemplateTypeNMSAttr( string NAME )
		{
			var    type = FindNMSTemplateType(NAME);
			return type?.GetCustomAttribute(NMSAttributeType);
		}
	}
}

//=============================================================================
