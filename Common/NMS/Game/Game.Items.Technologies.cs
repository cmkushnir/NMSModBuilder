﻿//=============================================================================
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
using static cmk.NMS.Game.Items.Data;

//=============================================================================

namespace cmk.NMS.Game.Items.Technology
{
    public class Collection
	: cmk.NMS.Game.Items.Collection
	{
		public Collection( NMS.Game.Files.Cache PAK_FILES )
		: base(PAK_FILES, 600)  // 4.00 - 310
		{
		}

		//...........................................................

		// "METADATA/REALITY/TABLES/NMS_REALITY_GCTECHNOLOGYTABLE.MXML" in default reality
		// "METADATA/REALITY/TABLES/NMS_REALITY_GCTECHNOLOGYTABLE.MBIN" actual mbin
		public override NMS.PAK.Item.Info FindItemInfo()
		{
			string path = Cache?.DefaultRealityMbin()?.TechnologyTable ??
				"METADATA/REALITY/TABLES/NMS_REALITY_GCTECHNOLOGYTABLE.MBIN"
			;
			path = NMS.PAK.Item.Path.NormalizeExtension(path);
			return Cache?.IPakItemCollection.FindInfo(path);
		}

		//...........................................................

		protected override void LoadMBIN()
		{
			var mbin  = ItemInfo?.ExtractMbin<GcTechnologyTable>(Log.Default);
			if( mbin == null ) return;

			var collection = Cache.IPakItemCollection;

			_ = Parallel.ForEach(mbin.Table, ITEM => {
				var icon      = NMS.PAK.Item.Path.NormalizeExtension(ITEM.Icon.Filename) as string;
				var cat_name  = ITEM.Category.TechnologyCategory.ToString() as string;
				var req_count = ITEM.Requirements?.Count ?? 0;

				var data = new Data(this, GcInventoryType.InventoryTypeEnum.Technology) {
					Id            = (string)ITEM.ID,
					NameId        = (string)ITEM.NameLower,
					DescriptionId = (string)ITEM.Description,
					CategoryName  = cat_name.Expand(),
					IconInfo      = collection.FindInfo(icon),
					Requirements  = new Requirement[req_count]
				};

				for( var i = 0; i < req_count; ++i ) {
					// data.Requirements is struct
					var item_req = ITEM.Requirements[i];
					data.Requirements[i].Type   =         item_req.Type.InventoryType;
					data.Requirements[i].Id     = (string)item_req.ID;
					data.Requirements[i].Amount =         item_req.Amount;
				}

				var dds  = data.IconInfo?.ExtractData<NMS.PAK.DDS.Data>();
				if( dds == null ) {
					Log.Default.AddWarning($"Unable to load {data.IconInfo?.Path} for {data.Id}");
				}
				else {
					data.Icon32 = dds.GetBitmap(32);  // recipe ingredient
					data.Icon48 = dds.GetBitmap(48);  // recipe result
					data.Icon64 = dds.GetBitmap(64);  // item list
				}

				lock( this ) this.Add(data);
			});
		}
	}
}

//=============================================================================
