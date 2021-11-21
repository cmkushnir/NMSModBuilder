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

using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS
{
	public class Recipe
	{
		protected static uint s_new_recipe_id = 0;

		//...........................................................

		/// <summary>
		/// Create new recipe object.
		/// </summary>
		public static GcRefinerRecipe Create(
			string ID,
			string NAME,
			float  TIME,
			string RESULT_ID,
			int    AMOUNT  = 1,
			bool   COOKING = false,
			List<Tuple<string, int>> INGREDIENT_IDS = null,  // <id, amount>
			Log    LOG = null
		){
			var result  = NMS.Game.Data.Selected.FindItemId(RESULT_ID);
			if( result == null ) {
				LOG.AddFailure($"Recipe.Create({ID}, {NAME}) - can't find result {RESULT_ID}");
				return null;
			}

			var ingredients = new List<GcRefinerRecipeElement>();
			if( !INGREDIENT_IDS.IsNullOrEmpty() ) {
				ingredients.Capacity = INGREDIENT_IDS.Count;
				foreach( var ingredient_t in INGREDIENT_IDS ) {
					var ingredient  = NMS.Game.Data.Selected.FindItemId(ingredient_t.Item1);
					if( ingredient == null ) {
						LOG.AddFailure($"Recipe.Create({ID}, {NAME}) - can't find ingredient {RESULT_ID}");
						continue;
					}
					ingredients.Add(new GcRefinerRecipeElement {
						Id     = ingredient.Id,
						Type   = new GcInventoryType{ InventoryType = ingredient.ItemType },
						Amount = ingredient_t.Item2
					});
				}
			}

			var recipe = new GcRefinerRecipe {
				Id          = ID,
				Name        = NAME,
				TimeToMake  = TIME,
				Cooking     = COOKING,
				Result      = new GcRefinerRecipeElement {
					Id     = result.Id,
					Type   = new GcInventoryType{ InventoryType = result.ItemType },
					Amount = AMOUNT
				},
				Ingredients = ingredients,
			};
			return recipe;
		}

		//...........................................................

		/// <summary>
		/// Create new refiner recipe object.
		/// </summary>
		public static GcRefinerRecipe CreateRefiner(
			string NAME,
			float  TIME,
			string RESULT_ID,
			int    AMOUNT  = 1,
			List<Tuple<string, int>> INGREDIENT_IDS = null,  // <id, amount>
			Log    LOG = null
		){
			return Create(
				$"cmkRecipeRefiner{++s_new_recipe_id:d4}",
				NAME, TIME, RESULT_ID, AMOUNT, false, INGREDIENT_IDS, LOG
			);
		}

		//...........................................................

		/// <summary>
		/// Create new cooking recipe object.
		/// </summary>
		public static GcRefinerRecipe CreateCooking(
			string NAME,
			float  TIME,
			string RESULT_ID,
			int    AMOUNT  = 1,
			List<Tuple<string, int>> INGREDIENT_IDS = null,  // <id, amount>
			Log    LOG = null
		){
			return Create(
				$"cmkRecipeCooking{++s_new_recipe_id:d4}",
				NAME, TIME, RESULT_ID, AMOUNT, false, INGREDIENT_IDS, LOG
			);
		}
	}
}

//=============================================================================
