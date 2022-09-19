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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

		public Data( Collection COLLECTION )
		{
			Collection = COLLECTION;
		}

		public readonly Collection Collection;

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
	: System.Collections.Generic.List<Data>
	, System.Collections.Specialized.INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public readonly cmk.ReadWriteLock Lock = new();

		//...........................................................

		public Collection( NMS.Game.Files.Cache PAK_FILES, int CAPACITY = 0 )
		{
			this.EnsureCapacity(CAPACITY);
			Cache = PAK_FILES;
		}

		//...........................................................

		public readonly NMS.Game.Files.Cache Cache;

		public NMS.PAK.Item.Info RecipeInfo { get; protected set; } = null;

		//...........................................................

		// "METADATA/REALITY/TABLES/NMS_REALITY_GCRECIPETABLE.MXML" in default reality
		// "METADATA/REALITY/TABLES/NMS_REALITY_GCRECIPETABLE.MBIN" actual mbin
		public NMS.PAK.Item.Info FindRecipeInfo()
		{
			string path = Cache.DefaultRealityMbin()?.RecipeTable ??
				"METADATA/REALITY/TABLES/NMS_REALITY_GCRECIPETABLE.MBIN"
			;
			path = NMS.PAK.Item.Path.NormalizeExtension(path);
			return Cache.IPakItemCollection.FindInfo(path);
		}

		//...........................................................

		/// <summary>
		/// Find Refiner or Cooking recipe by it's ID.
		/// </summary>
		public Data Find( string ID )  // case-sensitive
		{
			Lock.AcquireRead();
			try {
				return this.FindFirst(ITEM =>  // scan, not sorted by Id
					string.Equals(ITEM.Id, ID)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in a specified Substance|Product ID.
		/// </summary>
		public IEnumerable<Data> FindWithResult( string ID )
		{
			Lock.AcquireRead();
			foreach( var recipe in this ) {
				if( recipe.ResultId == ID ) yield return recipe;
			}
			Lock.ReleaseRead();
		}

		/// <summary>
		/// Find all Refiner|Cooking recipes that use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindWithIngredient( string ID )
		{
			Lock.AcquireRead();
			foreach( var recipe in this ) {
				foreach( var ingredient in recipe.Ingredients ) {
					if( ingredient.Id == ID ) {
						yield return recipe;
						break;
					}
				}
			}
			Lock.ReleaseRead();
		}

		/// <summary>
		/// Find all Refiner|Cooking recipes that result in or use a specified Substance|Product ID as an ingredient.
		/// </summary>
		public IEnumerable<Recipes.Data> FindWith( string ID )
		{
			Lock.AcquireRead();
			foreach( var recipe in this ) {
				if( recipe.ResultId == ID ) yield return recipe;
				else foreach( var ingredient in recipe.Ingredients ) {
					if( ingredient.Id == ID ) {
						yield return recipe;
						break;
					}
				}
			}
			Lock.ReleaseRead();
		}

		//...........................................................

		public void Reset()
		{
			Lock.AcquireWrite();
			this.Clear();
			RecipeInfo = null;
			Lock.ReleaseWrite();
		}

		public virtual void Load()
		{
		}

		protected void Load( bool COOKING )
		{
			// only (re)load if recipe mbin in diff pak.
			var recipe_info = FindRecipeInfo();

			Lock.AcquireWrite();
			try {			
				if( NMS.PAK.Item.Info.Equals(recipe_info, RecipeInfo) ) return;
				RecipeInfo = recipe_info;

				Log.Default.AddInformation($"Loading {GetType().FullName}");
				LoadMBIN(COOKING);
				Log.Default.AddInformation($"Loaded {GetType().FullName} - {this.Count} recipes");
			}
			finally { Lock.ReleaseWrite(); }

			UpdateLanguage(NMS.Game.Language.Identifier.Default);
		}

		//...........................................................

		protected void LoadMBIN( bool COOKING )
		{
			this.Clear();

			var mbin_data = RecipeInfo?.ExtractData<NMS.PAK.MBIN.Data>(Log.Default);
			var mbin      = mbin_data?.ModObject() as dynamic;
			if( mbin == null ) return;

			_ = Parallel.ForEach((IEnumerable<dynamic>)mbin.Table, RECIPE => {
				if( RECIPE.Cooking != COOKING ) return;
				var data = new Data(this) {
					Id           = (string)RECIPE.Id,                         // "REFINERECIPE_41"
					RecipeTypeId = (string)RECIPE.RecipeType,                 // "RECIPE_DUSTY1"
					RecipeName   = (string)RECIPE.RecipeName,                 // "R_NAME_DUSTY1"
					TimeToMake   =         RECIPE.TimeToMake,                 // 90
					ResultType   =         RECIPE.Result.Type.InventoryType,  // InventoryTypeEnum.Substance
					ResultId     = (string)RECIPE.Result.Id,                  // "LAND1"
					ResultAmount = RECIPE.Result.Amount,                      // 1
					Ingredients  = new Data.Ingredient[RECIPE.Ingredients.Count],
				};
				for( var i = 0; i < RECIPE.Ingredients.Count; ++i ) {
					var recipe_ingredient      = RECIPE.Ingredients[i];
					data.Ingredients[i].Type   =         recipe_ingredient.Type.InventoryType;
					data.Ingredients[i].Id     = (string)recipe_ingredient.Id;
					data.Ingredients[i].Amount =         recipe_ingredient.Amount;
				}
				lock( this ) this.Add(data);
			});

			// will block until products, substances, technologies tables built;

			_ = Parallel.ForEach(this, RECIPE => {
				switch( RECIPE.ResultType ) {
					case GcInventoryType.InventoryTypeEnum.Product:
						RECIPE.ResultData = Cache.Products.Find(RECIPE.ResultId);
						break;
					case GcInventoryType.InventoryTypeEnum.Substance:
						RECIPE.ResultData = Cache.Substances.Find(RECIPE.ResultId);
						break;
					case GcInventoryType.InventoryTypeEnum.Technology:
						RECIPE.ResultData = Cache.Technologies.Find(RECIPE.ResultId);
						break;
				}
				if( RECIPE.ResultData == null ) {
					// there are cases where they have mislabeled the InventoryTypeEnum
					// e.g. saying PLANT_CASE is a product not a substance,
					// so we check all types if not found for specified type.
					RECIPE.ResultData = Cache.FindItemData(RECIPE.ResultId);
				}
				for( var i = 0; i < RECIPE.Ingredients.Length; ++i ) {
					switch( RECIPE.Ingredients[i].Type ) {
						case GcInventoryType.InventoryTypeEnum.Product:
							RECIPE.Ingredients[i].Data = Cache.Products.Find(RECIPE.Ingredients[i].Id);
							break;
						case GcInventoryType.InventoryTypeEnum.Substance:
							RECIPE.Ingredients[i].Data = Cache.Substances.Find(RECIPE.Ingredients[i].Id);
							break;
						case GcInventoryType.InventoryTypeEnum.Technology:
							RECIPE.Ingredients[i].Data = Cache.Technologies.Find(RECIPE.Ingredients[i].Id);
							break;
					}
					if( RECIPE.Ingredients[i].Data == null ) {
						RECIPE.Ingredients[i].Data  = Cache.FindItemData(RECIPE.Ingredients[i].Id);
					}
				}
			});

			// sort: name, result amount, time to make, id
			this.Sort(( LHS, RHS ) => {
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

		public void UpdateLanguage( NMS.Game.Language.Identifier LANGUAGE_ID )
		{
			if( Count < 1 ) return;

			var language  = Cache.Languages.Get(LANGUAGE_ID);
			if( language == null ) return;

			Lock.AcquireWrite();

			_ = Parallel.ForEach(this, RECIPE => {
				var type   = language.GetText(RECIPE.RecipeTypeId, RECIPE.RecipeTypeId);
				var colon  = type.IndexOf(':');
				if( colon >= 0 ) type = type.Substring(colon + 2);
				RECIPE.RecipeType = type;
			});

			Lock.ReleaseWrite();
			Log.Default.AddInformation($"Updated {GetType().FullName} {LANGUAGE_ID.Name}");

			CollectionChanged?.DispatcherInvoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Reset
				)
			);
		}

		//...........................................................

		protected void OnCacheCollectionChanged( object SENDER, NotifyCollectionChangedEventArgs ARGS )
		{
			switch( ARGS.Action ) {
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Remove:
					Load();
					break;
			//	case NotifyCollectionChangedAction.Move:
			}
		}
	}

	//=========================================================================

	public class Cooking
	: cmk.NMS.Game.Recipes.Collection
	{
		public Cooking( NMS.Game.Files.Cache PAK_FILES )
		: base(PAK_FILES, 2000)  // 3.98 - 857
		{
		}

		public override void Load() => Load(true);
	}

	//=========================================================================

	public class Refiner
	: cmk.NMS.Game.Recipes.Collection
	{
		public Refiner( NMS.Game.Files.Cache PAK_FILES )
		: base(PAK_FILES, 600)  // 3.98 - 303
		{
		}

		public override void Load() => Load(false);
	}
}

//=============================================================================
