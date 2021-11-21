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

		public string Id     { get; set; }
		public string NameId { get; set; }

		// NameId converted for specific language
		protected string m_name;

		public string Name {
			get { return m_name; }
			set {
				if( m_name != value ) {
					m_name  = value;
					PropertyChangedInvoke();
				}
			}
		}

		public float TimeToMake { get; set; }

		public GcInventoryType.InventoryTypeEnum ResultType { get; set; }

		public string ResultId     { get; set; }
		public int    ResultAmount { get; set; }

		public Items.Data ResultData { get; set; }  // lookup using ResultType & ResultId

		public struct Ingredient
		{
			public GcInventoryType.InventoryTypeEnum Type { get; set; }
			public string     Id     { get; set; }
			public int        Amount { get; set; }
			public Items.Data Data   { get; set; }  // lookup using Type & Id
		}
		public Ingredient[] Ingredients { get; set; }
	}

	//=============================================================================

	public class Collection
	{
		protected List<Data>           m_list;
		protected ManualResetEventSlim m_built = new(false);

		//...........................................................

		public Collection( Game.Data GAME, int CAPACITY )
		{
			Game   = GAME;
			m_list = new(CAPACITY);
		}

		//...........................................................

		public Game.Data Game { get; }

		//...........................................................

		public IReadOnlyList<Data> List {
			get { return m_built.Wait(Int32.MaxValue) ? m_list : null; }
		}

		//...........................................................

		public Data Find( string ID )  // case-sensitive
		{
			return List.FindFirst(ITEM => {  // scan
				return string.Compare(ITEM.Id, ID) == 0;
			});
		}

		//...........................................................

		protected virtual void LoadMBIN( GcRecipeTable MBIN, bool COOKING )
		{
			_ = Parallel.ForEach(MBIN.Table, RECIPE => {
				if( RECIPE.Cooking != COOKING ) return;
				var data = new Data {
					Id           = RECIPE.Id?.Value,
					NameId       = RECIPE.Name?.Value,
					TimeToMake   = RECIPE.TimeToMake,
					ResultType   = RECIPE.Result.Type.InventoryType,
					ResultId     = RECIPE.Result.Id?.Value,
					ResultAmount = RECIPE.Result.Amount,
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
					RECIPE.ResultData = Game.FindItemId(RECIPE.ResultId);
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
						RECIPE.Ingredients[i].Data  = Game.FindItemId(RECIPE.Ingredients[i].Id);
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

				var mbin = Game.PCBANKS.ExtractMbin<GcRecipeTable>(
					"METADATA/REALITY/TABLES/NMS_REALITY_GCRECIPETABLE.MBIN",
					false, Log.Default
				);
				if( mbin != null ) LoadMBIN(mbin, COOKING);

				Log.Default.AddInformation($"Loaded {GetType().FullName} - {m_list.Count} recipes");

				if( Game != null ) {
					UpdateLanguage(Game.Language);
					Game.Language.IdentifierChanged += ( COLLECTION, OLD, NEW ) => UpdateLanguage(COLLECTION);
				}
			}
			finally { m_built.Set(); }
		}

		//...........................................................

		protected void UpdateLanguage( NMS.Game.Language.Collection LANGUAGE )
		{
			_ = Parallel.ForEach(m_list, RECIPE => {
				var name   = LANGUAGE.FindId(RECIPE.NameId, RECIPE.NameId);
				var colon  = name.IndexOf(':');
				if( colon >= 0 ) name = name.Substring(colon + 2);
				RECIPE.Name = name;
			});
			Log.Default.AddInformation($"Updated {GetType().FullName} {LANGUAGE.Name}");
		}
	}
}

//=============================================================================
