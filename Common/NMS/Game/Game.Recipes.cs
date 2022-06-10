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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS.Game.Recipes
{
	public class Data
	: System.ComponentModel.INotifyPropertyChanged
	{
		protected void PropertyChangedInvoke( [CallerMemberName] string NAME = "" )
		{
			PropertyChanged?.Invoke(this, new(NAME));
		}
		public event PropertyChangedEventHandler PropertyChanged;

		public string Id { get; set; }

		// RecipeTypeId converted for specific language
		protected string m_recipe_type;

		public string RecipeTypeId { get; set; }
		public string RecipeType {
			get { return m_recipe_type; }
			set {
				if( m_recipe_type != value ) {
					m_recipe_type  = value;
					PropertyChangedInvoke();
				}
			}
		}

		public string RecipeName { get; set; }
		public float  TimeToMake { get; set; }

		public GcInventoryType.InventoryTypeEnum ResultType { get; set; }

		public string  ResultId { get; set; }
		public int ResultAmount { get; set; }

		public NMS.Game.Items.Data ResultData { get; set; }  // lookup using ResultType & ResultId

		public struct Ingredient
		{
			public GcInventoryType.InventoryTypeEnum Type { get; set; }
			public string              Id     { get; set; }
			public int                 Amount { get; set; }
			public NMS.Game.Items.Data Data   { get; set; }  // lookup using Type & Id
		}
		public Ingredient[] Ingredients { get; set; }
	}

	//=============================================================================

	public class Collection
	{
		protected List<Data>           m_list;
		protected ManualResetEventSlim m_built = new(false);

		//...........................................................

		public Collection( Game.Data GAME, int CAPACITY, NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null )
		{
			Game   = GAME;
			m_list = new(CAPACITY);
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game.PCBANKS;
		}

		//...........................................................

		public Game.Data Game { get; }

		// PakItemCollection used to extract and load items from mbin's.
		// In general one of: Game, Game.PCBANKS (default), Game.Mods, Game.Mods[i]
		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		//...........................................................

		public IReadOnlyList<Data> List {
			get { return m_built.Wait(Int32.MaxValue) ? m_list : null; }
		}

		//...........................................................

		public Data Find( string ID )  // case-sensitive
		{
			return List.FindFirst(ITEM =>  // scan, not sorted by Id
				string.Equals(ITEM.Id, ID)
			);
		}

		//...........................................................

		protected virtual void LoadMBIN( GcRecipeTable MBIN, bool COOKING )
		{
			_ = Parallel.ForEach(MBIN.Table, RECIPE => {
				if( RECIPE.Cooking != COOKING ) return;
				var data = new Data {
					Id           = RECIPE.Id?.Value,                  // "REFINERECIPE_41"
					RecipeTypeId = RECIPE.RecipeType?.Value,          // "RECIPE_DUSTY1"
					RecipeName   = RECIPE.RecipeName?.Value,          // "R_NAME_DUSTY1"
					TimeToMake   = RECIPE.TimeToMake,                 // 90
					ResultType   = RECIPE.Result.Type.InventoryType,  // InventoryTypeEnum.Substance
					ResultId     = RECIPE.Result.Id?.Value,           // "LAND1"
					ResultAmount = RECIPE.Result.Amount,              // 1
					Ingredients  = new Data.Ingredient[RECIPE.Ingredients.Count],
				};
				for( var i = 0; i < RECIPE.Ingredients.Count; ++i ) {
					var recipe_ingredient = RECIPE.Ingredients[i];
					data.Ingredients[i].Type   = recipe_ingredient.Type.InventoryType;
					data.Ingredients[i].Id     = recipe_ingredient.Id?.Value;
					data.Ingredients[i].Amount = recipe_ingredient.Amount;
				}
				lock( m_list ) m_list.Add(data);
			});

			// will block until products, substances, technologies tables built;

			_ = Parallel.ForEach(m_list, RECIPE => {
				switch( RECIPE.ResultType ) {
					case GcInventoryType.InventoryTypeEnum.Product:
						RECIPE.ResultData = Game.Products.Find(RECIPE.ResultId);
						break;
					case GcInventoryType.InventoryTypeEnum.Substance:
						RECIPE.ResultData = Game.Substances.Find(RECIPE.ResultId);
						break;
					case GcInventoryType.InventoryTypeEnum.Technology:
						RECIPE.ResultData = Game.Technologies.Find(RECIPE.ResultId);
						break;
				}
				if( RECIPE.ResultData == null ) {
					// there are cases where they have mislabeled the InventoryTypeEnum
					// e.g. saying PLANT_CASE is a product not a substance,
					// so we check all types if not found for specified type.
					RECIPE.ResultData = Game.FindItem(RECIPE.ResultId);
				}
				for( var i = 0; i < RECIPE.Ingredients.Length; ++i ) {
					switch( RECIPE.Ingredients[i].Type ) {
						case GcInventoryType.InventoryTypeEnum.Product:
							RECIPE.Ingredients[i].Data = Game.Products.Find(RECIPE.Ingredients[i].Id);
							break;
						case GcInventoryType.InventoryTypeEnum.Substance:
							RECIPE.Ingredients[i].Data = Game.Substances.Find(RECIPE.Ingredients[i].Id);
							break;
						case GcInventoryType.InventoryTypeEnum.Technology:
							RECIPE.Ingredients[i].Data = Game.Technologies.Find(RECIPE.Ingredients[i].Id);
							break;
					}
					if( RECIPE.Ingredients[i].Data == null ) {
						RECIPE.Ingredients[i].Data  = Game.FindItem(RECIPE.Ingredients[i].Id);
					}
				}
			});

			// sort: name, result amount, time to make, id
			m_list.Sort(( LHS, RHS ) => {
				var cmp  = string.Compare(LHS.ResultData?.Name, RHS.ResultData?.Name);
				if( cmp != 0 ) return cmp;

				cmp = LHS.ResultAmount.CompareTo(RHS.ResultAmount);
				if( cmp != 0 ) return cmp;

				cmp = LHS.TimeToMake.CompareTo(RHS.TimeToMake);
				if( cmp != 0 ) return cmp;

				return String.CompareNumeric(LHS.Id, RHS.Id);
			});
		}

		//...........................................................

		/// <summary>
		/// Only called by Game.Data constructor.
		/// </summary>
		protected void Load( bool COOKING )
		{
			if( !m_built.IsSet ) try {  // only need to call Load once			
					Log.Default.AddInformation($"Loading {GetType().FullName}");

					if( IPakItemCollection == null ) {
						Log.Default.AddFailure($"{GetType().FullName} - Load failed, no IPakItemCollection set");
						return;
					}

					var mbin = IPakItemCollection?.ExtractMbin<GcRecipeTable>(
						"METADATA/REALITY/TABLES/NMS_REALITY_GCRECIPETABLE.MBIN",
						false, Log.Default
					);
					if( mbin != null ) LoadMBIN(mbin, COOKING);

					Log.Default.AddInformation($"Loaded {GetType().FullName} - {m_list.Count} recipes");

					if( Game != null ) {
						UpdateLanguage(Game.Language);
						Game.LanguageChanged += ( OLD, NEW ) => UpdateLanguage(NEW);
					}
				}
				finally { m_built.Set(); }
		}

		//...........................................................

		protected void UpdateLanguage( NMS.Game.Language.Collection LANGUAGE )
		{
			_ = Parallel.ForEach(m_list, RECIPE => {
				var type   = LANGUAGE.GetText(RECIPE.RecipeTypeId, RECIPE.RecipeTypeId);
				var colon  = type.IndexOf(':');
				if( colon >= 0 ) type = type.Substring(colon + 2);
				RECIPE.RecipeType = type;
			});
			Log.Default.AddInformation($"Updated {GetType().FullName} {LANGUAGE.Identifier.Name}");
		}
	}
}

//=============================================================================
