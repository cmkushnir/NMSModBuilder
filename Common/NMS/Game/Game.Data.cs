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
		/// <summary>
		/// protected - force construction through static Create method in order
		/// to ensure that any Game instances are valid at time of creation.
		/// </summary>
		protected Data( Location.Data LOCATION, Language.Identifier LANGUAGE = null )
		{
			Log.Default.AddHeading($"Creating {GetType().FullName} from {LOCATION.Path}");

			Location = LOCATION;

			// download|load|cache instance.
			var mbinc  = MBINC.LoadRelease(LOCATION.Release);
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

			Language = new(this);
			Parallel.Invoke(
				// load default language
				() => Language.Identifier = LANGUAGE ?? Game.Language.Identifier.Default,
				// link game pak mbin items to Mbinc classes.
				() => LinkMbinClasses()
			);

			Products     = new(this);
			Substances   = new(this);
			Technologies = new(this);
			Parallel.Invoke(
				// Substances, Products, Technologies require Language
				() => Products    .Load(),
				() => Technologies.Load(),
				() => Substances  .Load()
			);

			RefinerRecipes = new(this);
			CookingRecipes = new(this);
			Parallel.Invoke(
				// Recipes require Substances, Products
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
		/// Currently selected game language ID - value pairs.
		/// </summary>
		public readonly Language.Collection Language;

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
				var extension  = INFO.Path.Extension.ToUpper();
				if( extension != ".MBIN" &&
					extension != ".PC"  // all .PC are .MBIN.PC
				)	return;

				var class_info  = Mbinc.FindClass(INFO.MbinHeader.ClassName);
				if( class_info == null ) return;

				lock( class_info.PakItems ) class_info.PakItems.Add(INFO.Path);
			},	default);

			Mbinc.Classes.ForEach(CLASS => CLASS.PakItems.Sort());

			Log.Default.AddInformation($"Linked MBIN paths to libMBIN classes");
		}

		//...........................................................
		// cmk.NMS.PAK.Item.Interface
		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first MATCH.
		/// For each collection calls FindInfo, which scans InfoList's, not InfoTree's.
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( Predicate<NMS.PAK.Item.Info> MATCH )
		{
			foreach( var files in PakCollections ) {
				var found  = files.FindInfo(MATCH);
				if( found != null ) return found;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH (case-sensitive).
		/// For each collection calls FindInfo, which scans InfoList's, not InfoTree's.
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
		/// Loop through all PakCollections return all whose info.Path starts with with PATTERN (case-sensitive).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN )
		{
			foreach( var files in PakCollections ) {
				foreach( var info in files.FindInfoStartsWith(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections return all whose info.Path contains with PATTERN (case-sensitive).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoContains( string PATTERN )
		{
			foreach( var files in PakCollections ) {
				foreach( var info in files.FindInfoContains(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections return all whose info.Path ends with PATTERN (case-sensitive).
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN )
		{
			foreach( var files in PakCollections ) {
				foreach( var info in files.FindInfoEndsWith(PATTERN) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections return all whose info.Path matches REGEX.
		/// </summary>
		public IEnumerable<NMS.PAK.Item.Info> FindInfoRegex( Regex REGEX )
		{
			foreach( var files in PakCollections ) {
				foreach( var info in files.FindInfoRegex(REGEX) ) {
					yield return info;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH (case-sensitive).
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : NMS.PAK.Item.Data
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in all game and mod *.pak files");
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
		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null )
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in all game and mod *.pak files");
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
		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : class // libMBIN.NMSTemplate
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in all game and mod *.pak files");
				return null;
			}
			return info.ExtractMbin<AS_T>(LOG);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach PAK.Item.Info in all PakCollections.
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			_ = Parallel.ForEach(PakCollections,
				new() {
					CancellationToken      = CANCEL,
					MaxDegreeOfParallelism = System.Environment.ProcessorCount,
				},
				FILES => FILES.ForEachInfo(HANDLER, CANCEL, LOG)
			);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach PAK.Item.Data in all PakCollections.
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			_ = Parallel.ForEach(PakCollections,
				new() {
					CancellationToken      = CANCEL,
					MaxDegreeOfParallelism = System.Environment.ProcessorCount,
				},
				FILES => FILES.ForEachData(HANDLER, CANCEL, LOG)
			);
		}

		//...........................................................

		/// <summary>
		/// Parallel foreach PAK.MBIN.Data in all PakCollections.
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL, Log LOG = null
		){
			_ = Parallel.ForEach(PakCollections,
				new() {
					CancellationToken      = CANCEL,
					MaxDegreeOfParallelism = System.Environment.ProcessorCount,
				},
				FILES => FILES.ForEachMbin(HANDLER, CANCEL, LOG)
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

			var data  = FindItemId(ID);  // first check if substance|product|technology ID
			if( data != null ) return new(data.NameId, data.Name);

			var text  = Language.FindId(ID, null);
			if( text == null ) return null;

			return new(ID, text);
		}

		//...........................................................

		/// <summary>
		/// Find Substance, Product, Technology by it's ID.
		/// </summary>
		public Items.Data FindItemId( string ITEM_ID )
		{
			if( !ITEM_ID.IsNullOrEmpty() ) {
				var data = Substances  .Find(ITEM_ID); if( data != null ) return data;
				    data = Products    .Find(ITEM_ID); if( data != null ) return data;
				    data = Technologies.Find(ITEM_ID); if( data != null ) return data;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Find Refiner or Cooking recipe by it's ID.
		/// </summary>
		public Recipes.Data FindRecipeId( string RECIPE_ID )
		{
			if( !RECIPE_ID.IsNullOrEmpty() ) {
				var data = RefinerRecipes.Find(RECIPE_ID); if( data != null ) return data;
				    data = CookingRecipes.Find(RECIPE_ID); if( data != null ) return data;
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in a specified Substance|Product ID.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeResultItemId( string ITEM_ID )
		{
			if( !ITEM_ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					if( recipe.ResultId == ITEM_ID ) yield return recipe;
				}
				foreach( var recipe in CookingRecipes.List ) {
					if( recipe.ResultId == ITEM_ID ) yield return recipe;
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeIngredientItemId( string ITEM_ID )
		{
			if( !ITEM_ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ITEM_ID ) yield return recipe;
					}
				}
				foreach( var recipe in CookingRecipes.List ) {
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ITEM_ID ) yield return recipe;
					}
				}
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in or use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeItemId( string ITEM_ID )
		{
			if( !ITEM_ID.IsNullOrEmpty() ) {
				foreach( var recipe in RefinerRecipes.List ) {
					if( recipe.ResultId == ITEM_ID ) yield return recipe;
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ITEM_ID ) yield return recipe;
					}
				}
				foreach( var recipe in CookingRecipes.List ) {
					if( recipe.ResultId == ITEM_ID ) yield return recipe;
					foreach( var ingredient in recipe.Ingredients ) {
						if( ingredient.Id == ITEM_ID ) yield return recipe;
					}
				}
			}
		}
	}
}

//=============================================================================
