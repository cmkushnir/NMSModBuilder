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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

using libMBIN.NMS.Toolkit;

//=============================================================================

namespace cmk.NMS.Game.Language
{
	/// <summary>
	/// Identify a specific game language.
	/// </summary>
	public class Identifier
	{
		public static readonly Identifier English              = new(Resource.BitmapImage("FlagCanada.png"),      "English",                "ENGLISH");
		public static readonly Identifier German               = new(Resource.BitmapImage("FlagGermany.png"),     "German",                 "GERMAN");
		public static readonly Identifier Dutch                = new(Resource.BitmapImage("FlagNetherlands.png"), "Dutch",                  "DUTCH");
		public static readonly Identifier French               = new(Resource.BitmapImage("FlagFrance.png"),      "French",                 "FRENCH");
		public static readonly Identifier Italian              = new(Resource.BitmapImage("FlagItaly.png"),       "Italian",                "ITALIAN");
		public static readonly Identifier Spanish              = new(Resource.BitmapImage("FlagSpain.png"),       "Spanish",                "SPANISH");
		public static readonly Identifier LatinAmericanSpanish = new(Resource.BitmapImage("FlagMexico.png"),      "Latin American Spanish", "LATINAMERAICANSPANISH");
		public static readonly Identifier Portuguese           = new(Resource.BitmapImage("FlagPortugal.png"),    "Portuguese",             "PORTUGUESE");
		public static readonly Identifier BrazilianPortuguese  = new(Resource.BitmapImage("FlagBrazil.png"),      "Brazilian Portuguese",   "BRAZILIANPORTUGUESE");
		public static readonly Identifier Russian              = new(Resource.BitmapImage("FlagRussia.png"),      "Russian",                "RUSSIAN");
		public static readonly Identifier Polish               = new(Resource.BitmapImage("FlagPoland.png"),      "Polish",                 "POLISH");
		public static readonly Identifier Japanese             = new(Resource.BitmapImage("FlagJapan.png"),       "Japanese",               "JAPANESE");
		public static readonly Identifier Korean               = new(Resource.BitmapImage("FlagSouthKorea.png"),  "Korean",                 "KOREAN");
		public static readonly Identifier SimplifiedChinese    = new(Resource.BitmapImage("FlagChina.png"),       "Simplified Chinese",     "SIMPLIFIEDCHINESE");
		public static readonly Identifier TraditionalChinese   = new(Resource.BitmapImage("FlagChina.png"),       "Traditional Chinese",    "TRADITIONALCHINESE");
		public static readonly Identifier TenCentChinese       = new(Resource.BitmapImage("FlagChina.png"),       "TenCent Chinese",        "TENCENTCHINESE");
		public static readonly Identifier USEnglish            = new(Resource.BitmapImage("FlagUSA.png"),         "US English",             "USENGLISH");

		public static readonly Identifier[] List = new[] {
			English,
			German,
			Dutch,
			French,
			Italian,
			Spanish,
			LatinAmericanSpanish,
			Portuguese,
			BrazilianPortuguese,
			Russian,
			Polish,
			Japanese,
			Korean,
			SimplifiedChinese,
			TraditionalChinese,
			TenCentChinese,
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

		public ImageSource Icon { get; }  // flag
		public string      Text { get; }  // display
		public string      Name { get; }  // Game.Language.Name

		public Identifier( ImageSource ICON, string TEXT, string NAME )
		{
			Icon = ICON;
			Text = TEXT;
			Name = NAME;
		}
	}

	//=========================================================================

	/// <summary>
	/// Language ID - value pair.
	/// </summary>
	public class Data
	{
		public string Id   { get; set; }
		public string Text { get; set; }

		public Data( string ID, string TEXT )
		{
			Id   = ID;
			Text = TEXT;
		}

		//=====================================================================

		public class Comparer
		: System.Collections.Generic.IComparer<Data>
		{
			public int Compare( Data LHS, Data RHS )
			{
				return String.CompareNumeric(LHS.Id, RHS.Id);
			}
		}
	}

	//=========================================================================

	/// <summary>
	/// Localized string data for a given game language.
	/// </summary>
	public class Collection
	{
		public readonly ReaderWriterLockSlim Lock = new();

		public delegate void IdentifierChangedEventHandler(
			NMS.Game.Language.Collection COLLECTION,
			NMS.Game.Language.Identifier OLD,
			NMS.Game.Language.Identifier NEW
		);
		public event IdentifierChangedEventHandler IdentifierChanged;

		//...........................................................

		public Collection( NMS.Game.Data GAME, string NAME = null,
			NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null
		){
			Game = GAME;
			Name = NAME;
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game.PCBANKS;
		}

		//...........................................................

		public Collection( NMS.Game.Data GAME, Identifier IDENTIFIER,
			NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null
		){
			Game       = GAME;
			Identifier = IDENTIFIER;
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game.PCBANKS;
		}

		//...........................................................

		public readonly NMS.Game.Data Game;

		// PakItemCollection used to extract and load items from mbin's.
		// In general one of: Game, Game.PCBANKS (default), Game.Mods, Game.Mods[i]
		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		//...........................................................

		protected Identifier m_identifier;

		public Identifier Identifier {
			get { return m_identifier; }
			set {
				if( m_identifier == value ) return;

				var old = m_identifier;

				List.Clear();
				m_identifier = value;

				if( m_identifier != null ) {
					Log.Default.AddInformation($"Loading {GetType().FullName} {Name}");

					Lock.EnterWriteLock();
					try     { Load(); }
					catch   ( Exception EX ) { Log.Default.AddFailure(EX); }
					finally { Lock.ExitWriteLock(); }

					Log.Default.AddInformation($"Loaded {GetType().FullName} {Name} - {List.Count} entries");
				}

				IdentifierChanged?.Invoke(this, old, m_identifier);
			}
		}

		public int IdentifierIndex {
			get { return Array.IndexOf(Identifier.List, Identifier); }
		}

		public string Name {
			get { return Identifier?.Name ?? ""; }
			set {
				value      = value?.Trim().ToUpper();
				Identifier = Array.Find(Identifier.List, IDENTIFIER => string.Equals(IDENTIFIER.Name, value));
			}
		}

		//...........................................................

		/// <summary>
		/// Sorted on Id.
		/// </summary>
		public List<NMS.Game.Language.Data> List { get; }
			= new List<NMS.Game.Language.Data>(60000)  // 3.71 - 55,834
		;

		//...........................................................

		/// <summary>
		/// Get Language.Name string for Game item ID.
		/// </summary>
		public string FindId( string ID, string ON_NULL )
		{
			Lock.EnterReadLock();
			try {
				return List.Find(ID, ( LHS, ID ) =>
					String.CompareNumeric(LHS.Id, ID)
				)?.Text ?? ON_NULL;
			}
			finally { Lock.ExitReadLock(); }
		}

		//...........................................................

		protected void Load()
		{
			if( Game == null ) {
				Log.Default.AddFailure("No game set");
				return;
			}

			var regex = new Regex(
				@"(LANGUAGE\/NMS_)[0-9a-zA-Z]+(_" + Name + @".MBIN)",
				RegexOptions.Singleline | RegexOptions.Compiled,
				System.TimeSpan.FromSeconds(1)
			);
			var identifier_index = IdentifierIndex;

			// get all game language files.
			// FindInfoRegex may aquire read lock on IPakItemCollection (Files),
			// INFO.ExtractMbin may execute after FindInfoRegex returns and releases any lock.
			// ExtractMbin may aquire read lock.
			// Possible for INFO to no longer be valid when ExtractMbin called,
			// ExtractMbin may log error about Instance not matching.
			_ = Parallel.ForEach(IPakItemCollection.FindInfoRegex(regex), INFO => {
				var mbin  = INFO.ExtractMbin<TkLocalisationTable>(Log.Default);
				if( mbin != null ) {
					lock( List ) Add(List, identifier_index, mbin);
				}
			});

			List.Sort(new Data.Comparer());
		}

		//...........................................................

		protected void Add( List<NMS.Game.Language.Data> LIST, int INDEX, TkLocalisationTable MBIN )
		{
			foreach( var item in MBIN.Table ) {
				string  value = null;
				switch( INDEX ) {  // sync w/ Language.Identifier.List
					case  0: value = item.English             ?.Value; break;
					case  1: value = item.German              ?.Value; break;
					case  2: value = item.Dutch               ?.Value; break;
					case  3: value = item.French              ?.Value; break;
					case  4: value = item.Italian             ?.Value; break;
					case  5: value = item.Spanish             ?.Value; break;
					case  6: value = item.LatinAmericanSpanish?.Value; break;
					case  7: value = item.Portuguese          ?.Value; break;
					case  8: value = item.BrazilianPortuguese ?.Value; break;
					case  9: value = item.Russian             ?.Value; break;
					case 10: value = item.Polish              ?.Value; break;
					case 11: value = item.Japanese            ?.Value; break;
					case 12: value = item.Korean              ?.Value; break;
					case 13: value = item.SimplifiedChinese   ?.Value; break;
					case 14: value = item.TraditionalChinese  ?.Value; break;
					case 15: value = item.TencentChinese      ?.Value; break;
					case 16: value = item.USEnglish           ?.Value; break;
				}
				LIST.Add(new(item.Id, value ?? item.Id));
			}
		}
	}
}

//=============================================================================
