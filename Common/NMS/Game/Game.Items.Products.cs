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

using System.Threading.Tasks;

using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS.Game.Items.Product
{
	public class Data
	: cmk.NMS.Game.Items.Data
	{
		public string SubstanceCategoryName { get; set; }
		public GcProductCategory.ProductCategoryEnum Category { get; set; }  // e.g. Component
		public GcRealitySubstanceCategory.SubstanceCategoryEnum SubstanceCategory { get; set; }  // e.g. Catalyst
	}

	//=========================================================================

	public class Collection
	: cmk.NMS.Game.Items.Collection<NMS.Game.Items.Product.Data>
	{
		public Collection( Game.Data GAME, NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null )
		: base(GAME, 2000, PAK_ITEM_COLLECTION)  // 3.71 - 1,774
		{
		}

		//...........................................................

		protected override void LoadMBIN()
		{
			var mbin = IPakItemCollection.ExtractMbin<GcProductTable>(
				"METADATA/REALITY/TABLES/NMS_REALITY_GCPRODUCTTABLE.MBIN",
				false, Log.Default
			);
			if( mbin == null ) return;

			// build list of items, don't need to resolve language ID's here
			_ = Parallel.ForEach(mbin.Table, ITEM => {
				var data = new Data {
					ItemType = GcInventoryType.InventoryTypeEnum.Product,
					Id                    = ITEM.Id?.Value,
					NameId                = ITEM.NameLower?.Value,
					DescriptionId         = ITEM.Description?.Value,
					Category              = ITEM.Type.ProductCategory,
					CategoryName          = ITEM.Type.ProductCategory.ToString().Expand(),
					SubstanceCategory     = ITEM.Category.SubstanceCategory,
					SubstanceCategoryName = ITEM.Category.SubstanceCategory.ToString().Expand(),
					IconPath              = NMS.PAK.Item.Path.NormalizeExtension(ITEM.Icon.Filename?.Value, true)
				};
				var dds  = Game.PCBANKS.ExtractData<NMS.PAK.DDS.Data>(data.IconPath, false)?.Dds;
				if( dds == null ) {
					Log.Default.AddWarning($"Unable to load {data.IconPath} for {data.Id}");
				}
				else {
					data.Icon32 = dds.GetBitmap(32);
					data.Icon48 = dds.GetBitmap(48);
					data.Icon64 = dds.GetBitmap(64);
				}
				lock( m_list ) m_list.Add(data);
			});
		}
	}
}

//=============================================================================
