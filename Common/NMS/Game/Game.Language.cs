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

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using libMBIN.NMS.Globals;
using libMBIN.NMS.Toolkit;

//=============================================================================

namespace cmk.NMS
{
	public static partial class _x_
	{
		public static ref string Ref( this TkLocalisationEntry ENTRY, NMS.Game.Language.Identifier IDENTIFIER )
		{
			return ref Ref(ENTRY, IDENTIFIER.Index);
		}

		/// <summary>
		/// Returned ref is never null, may be empty.
		/// </summary>
		public static ref string Ref( this TkLocalisationEntry ENTRY, int INDEX )
		{
			switch( INDEX ) {  // use same order as in TkLocalisationEntry, Identifier.List
				default:
				case 0:
					if( ENTRY.English == null ) ENTRY.English = "";
					return ref ENTRY.English.Value;
				case 1:
					if( ENTRY.French == null ) ENTRY.French = "";
					return ref ENTRY.French.Value;
				case 2:
					if( ENTRY.Italian == null ) ENTRY.Italian = "";
					return ref ENTRY.Italian.Value;
				case 3:
					if( ENTRY.German == null ) ENTRY.German = "";
					return ref ENTRY.German.Value;
				case 4:
					if( ENTRY.Spanish == null ) ENTRY.Spanish = "";
					return ref ENTRY.Spanish.Value;
				case 5:
					if( ENTRY.Russian == null ) ENTRY.Russian = "";
					return ref ENTRY.Russian.Value;
				case 6:
					if( ENTRY.Polish == null ) ENTRY.Polish = "";
					return ref ENTRY.Polish.Value;
				case 7:
					if( ENTRY.Dutch == null ) ENTRY.Dutch = "";
					return ref ENTRY.Dutch.Value;
				case 8:
					if( ENTRY.Portuguese == null ) ENTRY.Portuguese = "";
					return ref ENTRY.Portuguese.Value;
				case 9:
					if( ENTRY.LatinAmericanSpanish == null ) ENTRY.LatinAmericanSpanish = "";
					return ref ENTRY.LatinAmericanSpanish.Value;
				case 10:
					if( ENTRY.BrazilianPortuguese == null ) ENTRY.BrazilianPortuguese = "";
					return ref ENTRY.BrazilianPortuguese.Value;
				case 11:
					if( ENTRY.SimplifiedChinese == null ) ENTRY.SimplifiedChinese = "";
					return ref ENTRY.SimplifiedChinese.Value;
				case 12:
					if( ENTRY.TraditionalChinese == null ) ENTRY.TraditionalChinese = "";
					return ref ENTRY.TraditionalChinese.Value;
				case 13:
					if( ENTRY.TencentChinese == null ) ENTRY.TencentChinese = "";
					return ref ENTRY.TencentChinese.Value;
				case 14:
					if( ENTRY.Korean == null ) ENTRY.Korean = "";
					return ref ENTRY.Korean.Value;
				case 15:
					if( ENTRY.Japanese == null ) ENTRY.Japanese = "";
					return ref ENTRY.Japanese.Value;
				case 16:
					if( ENTRY.USEnglish == null ) ENTRY.USEnglish = "";
					return ref ENTRY.USEnglish.Value;
			}
		}
	}
}

//=============================================================================

namespace cmk.NMS.Game.Language
{
	using Enum = libMBIN.NMS.Toolkit.TkLanguages.LanguageEnum;

	/// <summary>
	/// Identify a specific game language.
	/// </summary>
	public class Identifier
	{
		// use same order as in TkLocalisationEntry, which of course is different than TkLanguages.LanguageEnum
		public static readonly Identifier English              = new( 0, Enum.English,              Resource.BitmapImage("FlagCanada.png"),      "English",                "ENGLISH");
		public static readonly Identifier French               = new( 1, Enum.French,               Resource.BitmapImage("FlagFrance.png"),      "French",                 "FRENCH");
		public static readonly Identifier Italian              = new( 2, Enum.Italian,              Resource.BitmapImage("FlagItaly.png"),       "Italian",                "ITALIAN");
		public static readonly Identifier German               = new( 3, Enum.German,               Resource.BitmapImage("FlagGermany.png"),     "German",                 "GERMAN");
		public static readonly Identifier Spanish              = new( 4, Enum.Spanish,              Resource.BitmapImage("FlagSpain.png"),       "Spanish",                "SPANISH");
		public static readonly Identifier Russian              = new( 5, Enum.Russian,              Resource.BitmapImage("FlagRussia.png"),      "Russian",                "RUSSIAN");
		public static readonly Identifier Polish               = new( 6, Enum.Polish,               Resource.BitmapImage("FlagPoland.png"),      "Polish",                 "POLISH");
		public static readonly Identifier Dutch                = new( 7, Enum.Dutch,                Resource.BitmapImage("FlagNetherlands.png"), "Dutch",                  "DUTCH");
		public static readonly Identifier Portuguese           = new( 8, Enum.Portuguese,           Resource.BitmapImage("FlagPortugal.png"),    "Portuguese",             "PORTUGUESE");
		public static readonly Identifier LatinAmericanSpanish = new( 9, Enum.LatinAmericanSpanish, Resource.BitmapImage("FlagMexico.png"),      "Latin American Spanish", "LATINAMERAICANSPANISH");
		public static readonly Identifier BrazilianPortuguese  = new(10, Enum.BrazilianPortuguese,  Resource.BitmapImage("FlagBrazil.png"),      "Brazilian Portuguese",   "BRAZILIANPORTUGUESE");
		public static readonly Identifier SimplifiedChinese    = new(11, Enum.SimplifiedChinese,    Resource.BitmapImage("FlagChina.png"),       "Simplified Chinese",     "SIMPLIFIEDCHINESE");
		public static readonly Identifier TraditionalChinese   = new(12, Enum.TraditionalChinese,   Resource.BitmapImage("FlagChina.png"),       "Traditional Chinese",    "TRADITIONALCHINESE");
		public static readonly Identifier TenCentChinese       = new(13, Enum.TencentChinese,       Resource.BitmapImage("FlagChina.png"),       "TenCent Chinese",        "TENCENTCHINESE");
		public static readonly Identifier Korean               = new(14, Enum.Korean,               Resource.BitmapImage("FlagSouthKorea.png"),  "Korean",                 "KOREAN");
		public static readonly Identifier Japanese             = new(15, Enum.Japanese,             Resource.BitmapImage("FlagJapan.png"),       "Japanese",               "JAPANESE");
		public static readonly Identifier USEnglish            = new(16, Enum.USEnglish,            Resource.BitmapImage("FlagUSA.png"),         "US English",             "USENGLISH");

		public static readonly Identifier[] List = new[] {
			English,
			French,
			Italian,
			German,
			Spanish,
			Russian,
			Polish,
			Dutch,
			Portuguese,
			LatinAmericanSpanish,
			BrazilianPortuguese,
			SimplifiedChinese,
			TraditionalChinese,
			TenCentChinese,
			Korean,
			Japanese,
			USEnglish,
		};

		//...........................................................

		protected static Identifier s_default = List[0];

		/// <summary>
		/// Never null.
		/// </summary>
		public static Identifier Default {
			get { return s_default; }
			set { s_default = value ?? List[0]; }
		}

		//...........................................................

		public static Identifier FromName( string NAME )
		{
			return List.FindFirst(IDENTIFIER =>
				string.Equals(NAME, IDENTIFIER.Name, System.StringComparison.OrdinalIgnoreCase)
			);
		}

		//...........................................................

		public byte        Index { get; }  // Identifier.List[Index]
		public Enum        Enum  { get; }  // e.g. TkLanguages.LanguageEnum.English
		public ImageSource Icon  { get; }  // flag
		public string      Text  { get; }  // e.g. "Latin American Spanish"
		public string      Name  { get; }  // e.g. "LATINAMERAICANSPANISH"

		public Identifier( byte INDEX, Enum ENUM, ImageSource ICON, string TEXT, string NAME )
		{
			Index = INDEX;
			Enum  = ENUM;
			Icon  = ICON;
			Text  = TEXT;
			Name  = NAME;
		}
	}

	//=========================================================================

	/// <summary>
	/// Language ID - value pair.
	/// </summary>
	public class Data
	: System.IComparable<Data>
	, System.IComparable<string>
	{
		public Identifier Identifier { get; }
		public string     Path       { get; }
		public string     Id         { get; }
		public string     Text       { get; }

		public Data( Identifier IDENTIFIER, string PATH, string ID, string TEXT )
		{
			Id   = ID;
			Text = TEXT;
			Path = PATH;
			Identifier = IDENTIFIER;
		}

		//...........................................................

		public static int Compare( Data LHS, string RHS )
		{
			return String.CompareNumeric(LHS?.Id, RHS, true);
		}
		public static int Compare( Data   LHS, Data RHS ) =>  Compare(LHS, RHS.Id);
		public static int Compare( string LHS, Data RHS ) => -Compare(RHS, RHS.Id);

		public int CompareTo( Data   RHS ) => Compare(this, RHS);
		public int CompareTo( string RHS ) => Compare(this, RHS);

		//...........................................................

		public static bool Equals( Data   LHS, Data   RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( Data   LHS, string RHS ) => Compare(LHS, RHS) == 0;
		public static bool Equals( string LHS, Data   RHS ) => Compare(LHS, RHS) == 0;

		public override bool Equals( object RHS )
		{
			if( RHS is Data   rhs_data ) return Equals(this, rhs_data);
			if( RHS is string rhs_str  ) return Equals(this, rhs_str);
			return false;
		}

		//...........................................................

		public override int GetHashCode() => base.GetHashCode();
		public override string ToString() => Id;
	}

	//=========================================================================

	/// <summary>
	/// Localized string data for a given game language.
	/// </summary>
	public class Collection
	{
		public Collection( NMS.Game.Data GAME, Identifier IDENTIFIER,
			NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null
		){
			Game               = GAME;
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game?.PCBANKS;
			Identifier         = IDENTIFIER;
			List               = new(
				IPakItemCollection == null || Identifier == null ?
				0 : 60000  // 3.90 - 58,118
			);
			Load();
		}

		//...........................................................

		public readonly NMS.Game.Data            Game;
		public readonly Identifier               Identifier;
		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		//...........................................................

		/// <summary>
		/// Sorted on Id.
		/// </summary>
		public readonly List<NMS.Game.Language.Data> List;

		//...........................................................

		/// <summary>
		/// Get localized string for ID from merged List.
		/// </summary>
		public Data GetData( string ID )
		{
			return List.Bsearch(ID,
				(ITEM, KEY) => Data.Compare(ITEM, KEY)
			);
		}

		//...........................................................

		/// <summary>
		/// Get localized string for ID from merged List.
		/// </summary>
		public string GetText( string ID, string ON_NULL = "" )
		{
			var    data = GetData(ID);
			return data?.Text ?? ON_NULL;
		}

		//...........................................................

		protected void Load()
		{
			Log.Default.AddInformation($"Loading {GetType().FullName} {Identifier.Name}");

			var pak_item_collection  = IPakItemCollection;
			if( pak_item_collection == null ) {
				Log.Default.AddFailure($"{Identifier.Name} - Load failed, no IPakItemCollection set");
				return;
			}

			// build list of language paths.
			var paths = new List<string>();
			var mbin  = pak_item_collection.ExtractMbin<GcDebugOptions>(
				"GCDEBUGOPTIONS.GLOBAL.MBIN"
			);
			if( mbin == null ) {  // likely new release broke GCDEBUGOPTIONS
				try {             // fall-back to regex any|all prefix
					var regex = $"LANGUAGE\\/.*_{Identifier.Name}.MBIN".CreateRegex(true, true);
					foreach( var info in pak_item_collection.FindInfo(regex) ) {
						paths.Add(info.Path);
					}
				}
				catch( System.Exception EX ) {
					Log.Default.AddFailure(EX);
					return;  // we have no fall-back fall-back
				}
			}
			else {  // mbin != null
				foreach( string prefix in mbin.LocTableList ) {
					var path = $"LANGUAGE/{prefix}_{Identifier.Name}.MBIN";
					paths.Add(NMS.PAK.Item.Path.Normalize(path));
				}
			}

			_ = Parallel.ForEach(paths, PATH => {
				var mbin = pak_item_collection.ExtractMbin<TkLocalisationTable>(
					PATH, true, Log.Default
				);
				if( mbin == null ) return;
				lock( List ) {
					Add(List, PATH, mbin, Identifier.Index);
				}
			});

			List.Sort();

			Log.Default.AddInformation($"Loaded {GetType().FullName} {Identifier.Name} - {List.Count} entries");
		}

		//...........................................................

		protected void Add( List<NMS.Game.Language.Data> LIST, string PATH, TkLocalisationTable MBIN, byte INDEX )
		{
			foreach( var item in MBIN.Table ) {
				LIST.Add(
					new(Identifier, PATH, item.Id, item.Ref(INDEX))
				);
			}
		}
	}

	//=========================================================================

	public class Cache
	{
		protected Collection [] m_collection = new Collection[Identifier.List.Length];

		//...........................................................

		public Cache( NMS.Game.Data GAME = null, NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null )
		{
			Game               = GAME ?? NMS.Game.Data.Selected;
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game?.PCBANKS;
		}

		//...........................................................

		public readonly NMS.Game.Data            Game;
		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		//...........................................................

		public Collection Get( Identifier IDENTIFIER )
		{
			return Get(IDENTIFIER?.Index ?? 255);
		}

		//...........................................................

		public Collection Get( int INDEX )
		{
			if( INDEX >= Identifier.List.Length ) return null;
			if( m_collection[INDEX] == null ) {
				m_collection[INDEX]  = new(Game, Identifier.List[INDEX], IPakItemCollection);
			}
			return m_collection[INDEX];
		}
	}
}

//=============================================================================
