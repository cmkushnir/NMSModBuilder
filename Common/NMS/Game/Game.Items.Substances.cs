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

namespace cmk.NMS.Game.Items.Substance
{
	public class Data
	: cmk.NMS.Game.Items.Data
	{
		public GcRealitySubstanceCategory.SubstanceCategoryEnum Category { get; set; }
	}

	//=========================================================================

	public class Collection
	: cmk.NMS.Game.Items.Collection<NMS.Game.Items.Substance.Data>
	{
		public Collection( Game.Data GAME, NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null )
		: base(GAME, 100, PAK_ITEM_COLLECTION)  // 3.71 - 89
		{
		}

		//...........................................................

		protected override void LoadMBIN()
		{
			var mbin = IPakItemCollection.ExtractMbin<GcSubstanceTable>(
				"METADATA/REALITY/TABLES/NMS_REALITY_GCSUBSTANCETABLE.MBIN",
				false, Log.Default
			);
			if( mbin == null ) return;

			_ = Parallel.ForEach(mbin.Table, ITEM => {
				var data = new Data {
					ItemType = GcInventoryType.InventoryTypeEnum.Substance,
					Id            = ITEM.ID?.Value,
					NameId        = ITEM.NameLower?.Value,
					DescriptionId = ITEM.Description?.Value,
					Category      = ITEM.Category.SubstanceCategory,
					CategoryName  = ITEM.Category.SubstanceCategory.ToString().Expand(),
					IconPath      = NMS.PAK.Item.Path.NormalizeExtension(ITEM.Icon.Filename?.Value, true)
				};
				// sulphurine and nitrogen icons reversed
				if( data.Id == "GAS1" ) data.IconPath = data.IconPath.Replace(".3.", ".1.");
				if( data.Id == "GAS3" ) data.IconPath = data.IconPath.Replace(".1.", ".3.");
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
