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
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk.NMS.Game
{
	/// <summary>
	/// Hold all state related to a game instance.
	/// </summary>
	public partial class Data
	: cmk.NMS.PAK.Item.ICollection
	{
		public delegate void LanguageChangedEventHandler(
			NMS.Game.Language.Collection OLD,
			NMS.Game.Language.Collection NEW
		);
		public event LanguageChangedEventHandler LanguageChanged;

		//...........................................................

		/// <summary>
		/// protected - force construction through static Create method in order
		/// to ensure that any Game instances are valid at time of creation.
		/// </summary>
		protected Data( Location.Data LOCATION, Language.Identifier LANGUAGE = null )
		{
			Log.Default.AddHeading($"Creating {GetType().FullName} from {LOCATION.Path}");

			Location = LOCATION;

			var release = LOCATION.Release;
			if( release.MbincVersion != MBINC.Linked.Version ) {
				Log.Default.AddWarning(
					$"Incorrect libMBIN.dll for selected game, have {MBINC.Linked.Version} need {release.MbincVersion}.\n" +
					$"May prompt to download required libMBIN.dll, but it wil have wrong name and cannot be hot swapped with loaded libMBIN.dll.\n" +
					$"Need to close app and manually update; will continue with reduced functionality."
				);
			}

			// download|load|cache instance.
			var mbinc  = MBINC.LoadRelease(release);
			if( mbinc == null ) return;

			// create a game instance specific copy of libMBIN wrapper
			// so we can assoc game instance specific mbin's with its classes.
			// i.e. multiple game instances may use same libMBIN version,
			//      but may have different mbins.
			Mbinc = new(mbinc.Assembly);

			// testing shows slightly faster to load with minimal contention.
			Parallel.Invoke(
				() => PCBANKS = new(this),  // starts Task to build merged info tree
				() => MODS    = new(this)
			);
			PakCollections = new() { MODS, PCBANKS };  // in search order

			Languages = new(this, PCBANKS);
			Parallel.Invoke(
				() => Language = Languages.Get(NMS.Game.Language.Identifier.Default),			
				() => LinkMbinClasses()  // link game pak mbin items to Mbinc classes
			);

			Products     = new(this);
			Substances   = new(this);
			Technologies = new(this);
			Parallel.Invoke(  // require Language
				() => Products    .Load(),
				() => Technologies.Load(),
				() => Substances  .Load()
			);

			RefinerRecipes = new(this);
			CookingRecipes = new(this);
			Parallel.Invoke(  // require Substances, Products
				() => CookingRecipes.Load(),
				() => RefinerRecipes.Load()
			);
		}

		//...........................................................

		public readonly Location.Data Location;
		public readonly MBINC         Mbinc;

		/// <summary>
		/// Path\GAMEDATA\PCBANKS\*.pak
		/// </summary>
		public Game.PCBANKS.Files PCBANKS { get; protected set; }

		/// <summary>
		/// Path\GAMEDATA\PCBANKS\MODS\*.pak
		/// </summary>
		public Game.MODS.Files MODS { get; protected set; }

		/// <summary>
		/// { MODS, PCBANKS }
		/// Note that MODS is first so that searches find items in
		/// MODS before looking in PCBANKS, like game would.
		/// </summary>
		public List<NMS.PAK.Files> PakCollections { get; }

		//...........................................................

		/// <summary>
		/// Cached language collections.
		/// </summary>
		public readonly Language.Cache Languages;

		/// <summary>
		/// Currently selected language for game instance.
		/// </summary>
		protected Language.Collection m_language;
		public    Language.Collection   Language {
			get { return m_language; }
			set {
				if( value == m_language ) return;
				var old = m_language;
				m_language = value;
				LanguageChanged?.Invoke(old, m_language);
			}
		}

		public readonly Items.Product   .Collection Products;
		public readonly Items.Substance .Collection Substances;
		public readonly Items.Technology.Collection Technologies;

		public readonly Recipes.Refine.Collection RefinerRecipes;
		public readonly Recipes.Cook  .Collection CookingRecipes;

		//...........................................................

		/// <summary>
		/// When a pak file is loaded it parses all mbin headers.
		/// LinkMbinClasses links the pak mbin items to the
		/// MBINC class specified in the mbin header.
		/// </summary>
		protected void LinkMbinClasses()
		{
			Log.Default.AddInformation($"Linking MBIN paths to libMBIN classes");

			// ForEachInfo does parallel ForEachInfo on each PAK file, does not use merged tree.
			PCBANKS.ForEachInfo(( INFO, CANCEL, LOG ) => {
				if( INFO.MbinHeader == null) return;

				var class_info  = Mbinc.FindClass(INFO.MbinHeader?.ClassName);
				if( class_info == null ) return;

				// don't link mbin's to classes if guid's don't match,
				// but still allow mbin's to be explicitly selected|viewed.
				if( class_info.NMSAttributeGUID != INFO.MbinHeader.ClassGuid ) {
					if( NMS.PAK.MBIN.Data.LogGuidMismatch ) {
						Log.Default.AddWarning($"{INFO.Path} mbin GUID {INFO.MbinHeader.ClassGuid:x16} != struct GUID {class_info.NMSAttributeGUID:x16}");
					}
					return;
				}

				lock( class_info.PakItems ) class_info.PakItems.Add(INFO.Path);
			},	default);

			Mbinc.Classes.ForEach(CLASS => CLASS.PakItems.Sort());

			Log.Default.AddInformation($"Linked MBIN paths to libMBIN classes");
		}

		//...........................................................
		// cmk.NMS.PAK.Item.Interface
		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH (case-sensitive).
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);
			foreach( var files in PakCollections ) {
				var found  = files.FindInfo(PATH, false);
				if( found != null ) return found;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first MATCH.
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public List<NMS.PAK.Item.Info> FindInfo( Predicate<NMS.PAK.Item.Info> MATCH, bool SORT = true )
		{
			var list = new List<NMS.PAK.Item.Info>();
			foreach( var files in PakCollections ) {
				var files_list = files.FindInfo(MATCH, false);
				list.Capacity += files_list.Count;
				list.AddRange(files_list);
			}
			if( SORT ) list.Sort();
			return list;
		}

		//...........................................................

		public List<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoStartsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoContains( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoContains(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoEndsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoRegex( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoRegex(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfo( Regex REGEX, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfo(REGEX, SORT);

		//...........................................................

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		=> ((NMS.PAK.Item.ICollection)this).DefaultExtractData(PATH, NORMALIZE, LOG, CANCEL);

		/// <summary>
		/// Loop through all PakCollections for first with PATH (case-sensitive).
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in any game or mod *.pak file");
				return null;
			}
			return info.ExtractData<AS_T>(LOG);
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH.
		/// Return extracted raw data converted to BitmapSource.
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null, CancellationToken CANCEL = default )
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in any game or mod *.pak file");
				return null;
			}
			return info.ExtractDdsBitmapSource(HEIGHT, LOG);
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH.
		/// Return extracted raw data converted to mbin AS_T.
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class // libMBIN.NMSTemplate
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in any game or mod *.pak file");
				return null;
			}
			return info.ExtractMbin<AS_T>(LOG);
		}

		//...........................................................

		/// <summary>
		/// foreach PAK.Item.Info in all PakCollections.
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PakCollections.ForEach(FILES =>
				FILES.ForEachInfo(HANDLER, LOG, CANCEL)
			);
		}

		//...........................................................

		/// <summary>
		/// foreach PAK.Item.Data in all PakCollections.
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PakCollections.ForEach(FILES =>
				FILES.ForEachData(HANDLER, LOG, CANCEL)
			);
		}

		//...........................................................

		/// <summary>
		/// foreach PAK.MBIN.Data in all PakCollections.
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PakCollections.ForEach(FILES =>
				FILES.ForEachMbin(HANDLER, LOG, CANCEL)
			);
		}

		//...........................................................
		// cached game data
		//...........................................................

		/// <summary>
		/// ID is a language entry ID or substance|product|technology ID.
		/// If ID is a substance|product|technology ID return <NameId, Name>.
		/// If ID is a language ID return <ID, text>.
		/// </summary>
		public Language.Data FindLanguageId( string ID )
		{
			if( ID.IsNullOrEmpty() ) return null;

			// if ID is for substance|product|technology then use it's NameId
			var data  = FindItem(ID);
			if( data != null ) ID = data.NameId;

			return Language.GetData(ID);
		}

		//...........................................................

		/// <summary>
		/// Find Substance, Product, Technology by it's ID.
		/// </summary>
		public Items.Data FindItem( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				var data = Substances  .Find(ID); if( data != null ) return data;
				    data = Products    .Find(ID); if( data != null ) return data;
				    data = Technologies.Find(ID); if( data != null ) return data;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Find Refiner or Cooking recipe by it's ID.
		/// </summary>
		public Recipes.Data FindRecipe( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				var data = RefinerRecipes.Find(ID); if( data != null ) return data;
				    data = CookingRecipes.Find(ID); if( data != null ) return data;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in a specified Substance|Product ID.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeWithResult( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					if( recipe.ResultId == ID ) yield return recipe;
				}
				foreach( var recipe in CookingRecipes.List ) {
					if( recipe.ResultId == ID ) yield return recipe;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeWithIngredient( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ID ) {
							yield return recipe;
							break;
						}
					}
				}
				foreach( var recipe in CookingRecipes.List ) {
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ID ) {
							yield return recipe;
							break;
						}
					}
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in or use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeWith( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					if( recipe.ResultId == ID ) yield return recipe;
					else foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ID ) {
							yield return recipe;
							break;
						}
					}
				}
				foreach( var recipe in CookingRecipes.List ) {
					if( recipe.ResultId == ID ) yield return recipe;
					else foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ID ) {
							yield return recipe;
							break;
						}
					}
				}
			}
		}
	}
}

//=============================================================================
