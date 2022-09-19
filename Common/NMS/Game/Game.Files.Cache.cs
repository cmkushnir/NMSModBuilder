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
using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS.Game.Files
{
    /// <summary>
    /// Cache commonly used data from a set of pak files.
    /// </summary>
    public class Cache
	: cmk.NMS.PAK.Files
	{
		public Cache( Game.Data GAME, string PATH )
		: base(PATH)
		{
			Game = GAME;

			IPakItemCollection = IsPCBANKS ? this : Game;

			Languages = new(this);

			Products     = new(this);
			Substances   = new(this);
			Technologies = new(this);

			RefinerRecipes = new(this);
			CookingRecipes = new(this);
		}

		//...........................................................

		public readonly NMS.Game.Data Game;

		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		public readonly NMS.Game.Language.CollectionCache Languages;

		public readonly NMS.Game.Items.Product   .Collection Products;
		public readonly NMS.Game.Items.Substance .Collection Substances;
		public readonly NMS.Game.Items.Technology.Collection Technologies;

		public readonly NMS.Game.Recipes.Refiner RefinerRecipes;
		public readonly NMS.Game.Recipes.Cooking CookingRecipes;

		public NMS.Game.Language.Collection Language => Languages.Get(LanguageId);

		protected NMS.Game.Language.Identifier m_LanguageId = NMS.Game.Language.Identifier.Default;

		public NMS.Game.Language.Identifier LanguageId {
			get => m_LanguageId;
			set {
				if( m_LanguageId == value ) return;
				m_LanguageId = value;
				// could do in parallel, but doing all takes < 0.5 sec
				Substances    .UpdateLanguage(m_LanguageId);
				Products      .UpdateLanguage(m_LanguageId);
				Technologies  .UpdateLanguage(m_LanguageId);
				RefinerRecipes.UpdateLanguage(m_LanguageId);
				CookingRecipes.UpdateLanguage(m_LanguageId);
			}
		}

		//...........................................................

		public void ReloadCache( bool RELOAD )
		{
			ResetMbins();
			Languages.Reset();

			if( RELOAD ) {
				if( Language == null ) return;
				Language.Load();
				Parallel.Invoke(  // requires Language
					() => Substances  .Load(),
					() => Products    .Load(),
					() => Technologies.Load()
				);
				Parallel.Invoke(  // requires Substances, Products
					() => Products    .LinkRequirements(),
					() => Technologies.LinkRequirements()
				);
				Parallel.Invoke(  // requires Substances, Products
					() => RefinerRecipes.Load(),
					() => CookingRecipes.Load()
				);
			}
			else {
				Parallel.Invoke(
					() => CookingRecipes.Reset(),
					() => RefinerRecipes.Reset(),
					() => Technologies  .Reset(),
					() => Products      .Reset(),
					() => Substances    .Reset()
				);
			}
		}

		//...........................................................

		protected void ResetMbins()
		{
			lock( this ) {
				gcdebugoptions_global_mbin = null;
				defaultreality_mbin        = null;
			}
		}

		protected dynamic gcdebugoptions_global_mbin;
		protected dynamic defaultreality_mbin;

		public dynamic GlobalGcDebugOptionsMbin()
		{
			lock( this ) {
				if( gcdebugoptions_global_mbin == null ) {
					var data = IPakItemCollection.ExtractData<NMS.PAK.MBIN.Data>(
					"GCDEBUGOPTIONS.GLOBAL.MBIN", false, Log.Default
				);
					gcdebugoptions_global_mbin = data?.RawObject();
				}
				return gcdebugoptions_global_mbin;
			}
		}

		public dynamic DefaultRealityMbin()
		{
			lock( this ) {
				if( defaultreality_mbin == null ) {
					var global_gcdebugoptions_mbin = GlobalGcDebugOptionsMbin();
					if( global_gcdebugoptions_mbin == null ) return null;

					string defaultreality = global_gcdebugoptions_mbin.RealityPresetFile;
					if( defaultreality.IsNullOrEmpty() ) {
						Log.Default.AddWarning("Unable to extract GCDEBUGOPTIONS.GLOBAL.MBIN, using 'METADATA/REALITY/DEFAULTREALITY.MBIN' as RealityPresetFile");
						defaultreality = "METADATA/REALITY/DEFAULTREALITY.MBIN";
					}

					var data = IPakItemCollection.ExtractData<NMS.PAK.MBIN.Data>(
						defaultreality, true, Log.Default
					);
					defaultreality_mbin = data?.RawObject();
				}
				return defaultreality_mbin;
			}
		}

		public List<NMS.PAK.Item.Info> LanguageMbinInfo( NMS.Game.Language.Identifier LANGUAGE_ID = null )
		{
			if( LANGUAGE_ID == null ) LANGUAGE_ID = LanguageId;

			var language_name = LANGUAGE_ID?.Name.ToUpper() ?? "";
			var list          = new List<NMS.PAK.Item.Info>(8);

			var global_gcdebugoptions_mbin  = GlobalGcDebugOptionsMbin();
			if( global_gcdebugoptions_mbin == null ) {
				Log.Default.AddWarning("Unable to extract GCDEBUGOPTIONS.GLOBAL.MBIN, using regex to find language mbin's, may get dups");
				var regex = $"LANGUAGE\\/.*_{language_name}.MBIN".CreateRegex(true, true);
				foreach( var info in IPakItemCollection.FindInfo(regex) ) {
					list.Add(info);
				}
			}
			else {
				foreach( string prefix in global_gcdebugoptions_mbin.LocTableList ) {
					var path  = $"LANGUAGE/{prefix.ToUpper()}_{language_name}.MBIN";
					var info  = IPakItemCollection.FindInfo(path, false);
					if( info != null ) list.Add(info);
				}
			}

			list.Sort();
			return list;
		}

		//...........................................................

		/// <summary>
		/// ID is a language entry ID or substance|product|technology ID.
		/// If ID is a substance|product|technology ID return <NameId, Name>.
		/// If ID is a language ID return <ID, text>.
		/// </summary>
		public Language.Data FindLanguageData( string ID )
		{
			if( ID.IsNullOrEmpty() ) return null;

			// if ID is for substance|product|technology then use it's NameId
			var data  = FindItemData(ID);
			if( data != null ) ID = data.NameId;

			return Language?.GetData(ID);
		}

		//...........................................................

		/// <summary>
		/// Find Substance, Product, Technology by it's ID.
		/// </summary>
		public Items.Data FindItemData( string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() ) {
				var data = Substances  .Find(ID); if( data != null ) return data;
				    data = Products    .Find(ID); if( data != null ) return data;
				    data = Technologies.Find(ID); if( data != null ) return data;
			}
			return null;
		}

		/// <summary>
		/// Find Substance, Product, Technology by it's TYPE and ID.
		/// </summary>
		public Items.Data FindItemData( GcInventoryType.InventoryTypeEnum TYPE, string ID )
		{
			ID = ID?.ToUpper();
			if( !ID.IsNullOrEmpty() )
			switch( TYPE ) {
				case GcInventoryType.InventoryTypeEnum.Substance:  return Substances  .Find(ID);
				case GcInventoryType.InventoryTypeEnum.Product:    return Products    .Find(ID);
				case GcInventoryType.InventoryTypeEnum.Technology: return Technologies.Find(ID);
			}
			return null;
		}

		//...........................................................

		/// <summary>
		/// Find Refiner or Cooking recipe by it's ID.
		/// </summary>
		public Recipes.Data FindRecipeData( string ID )
		{
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
		public IEnumerable<Recipes.Data> FindRecipeDataWithResult( string ID )
		{
			if( ID.IsNullOrEmpty() ) yield break;
			foreach( var recipe in RefinerRecipes.FindWithResult(ID) ) {
				yield return recipe;
			}
			foreach( var recipe in CookingRecipes.FindWithResult(ID) ) {
				yield return recipe;
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeDataWithIngredient( string ID )
		{
			if( ID.IsNullOrEmpty() ) yield break;
			foreach( var recipe in RefinerRecipes.FindWithIngredient(ID) ) {
				yield return recipe;
			}
			foreach( var recipe in CookingRecipes.FindWithIngredient(ID) ) {
				yield return recipe;
			}
		}

		//...........................................................

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in or use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindRecipeDataWith( string ID )
		{
			if( ID.IsNullOrEmpty() ) yield break;
			foreach( var recipe in RefinerRecipes.FindWith(ID) ) {
				yield return recipe;
			}
			foreach( var recipe in CookingRecipes.FindWith(ID) ) {
				yield return recipe;
			}
		}
	}
}

//=============================================================================
