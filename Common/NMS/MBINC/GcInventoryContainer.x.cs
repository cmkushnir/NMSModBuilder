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

using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS
{
	public static partial class _x_
	{
		public static bool Equals(
			this GcInventoryIndex LHS,
				 GcInventoryIndex RHS
		){
			return
				LHS.Y == RHS.Y &&
				LHS.X == RHS.X
			;
		}

		public static int Compare(
			this GcInventoryIndex LHS,
				 GcInventoryIndex RHS
		){
			if( LHS.Y < RHS.Y ) return -1;
			if( LHS.Y > RHS.Y ) return  1;
			if( LHS.X < RHS.X ) return -1;
			if( LHS.X > RHS.X ) return  1;
			return 0;
		}

		//...........................................................

		/// <summary>
		/// Sort INVENTORY.Slots and INVENTORY.ValidSlotIndices by Index.
		/// Call after finished Add's and Set's.
		/// </summary>
		public static void Sort(
			this GcInventoryContainer INVENTORY
		){
			INVENTORY?.Slots?.Sort(( LHS, RHS ) => {
				return LHS.Index.Compare(RHS.Index);
			});
			INVENTORY?.ValidSlotIndices?.Sort(( LHS, RHS ) => {
				return LHS.Compare(RHS);
			});
		}

		//...........................................................

		public class SlotData
		{
			public libMBIN.NMS.GameComponents.GcInventoryElement Element;
			public bool               IsValid;  // Element.Index in INVENTORY.ValidSlotIndices
		}

		//...........................................................

		/// <summary>
		/// Get all possible slots as an 8x6 grid.
		/// Note: starting ship and multitool slots have unspecified
		/// Index (-1, -1), likely to indicate that the slot positions
		/// be assigned at runtime based on available slots.
		/// </summary>
		public static SlotData[][] GetSlotGrid(
			this GcInventoryContainer INVENTORY
		){
			var grid = new SlotData [INVENTORY.Height][];
			for( var row = 0; row < grid.Length; ++row ) {
				grid[row] = new SlotData[INVENTORY.Width];
				for( var col = 0; col < grid[row].Length; ++col ) {
					grid[row][col] = new() { IsValid = false };
				}
			}

			foreach( var slot in INVENTORY.Slots ) {
				if( slot.Index.Y < 0 || slot.Index.X < 0 ) continue;
				grid[slot.Index.Y][slot.Index.X].Element = slot;
			}
			foreach( var slot in INVENTORY.ValidSlotIndices ) {
				if( slot.Y < 0 || slot.X < 0 ) continue;
				grid[slot.Y][slot.X].IsValid = true;
			}

			return grid;
		}

		//...........................................................

		/// <summary>
		/// Set the contents of a specific slot.
		/// Adds if not used, else overwrites.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement Set(
			this GcInventoryContainer INVENTORY,
			GcInventoryType.InventoryTypeEnum TYPE,
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			var item = NMS.Inventory.CreateElement(TYPE,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
			var index = item.Index;

			// check if slot already used, overwrite if it is
			var existing = INVENTORY.Slots.FindIndex(
				ITEM => ITEM.Index.Equals(index)
			);
			if( existing < 0 ) INVENTORY.Slots.Add(item);
			else               INVENTORY.Slots[existing] = item;

			// mark slot as valid (not sure this is needed)
			existing = INVENTORY.ValidSlotIndices.FindIndex(
				ITEM => ITEM.Equals(index)
			);
			if( existing < 1 ) INVENTORY.ValidSlotIndices.Add(item.Index);

			return item;
		}

		//...........................................................

		/// <summary>
		/// Add a new item in the first available valid slot.
		/// If no valid slots left then try to add to first unused slot.
		/// If no unused slots then returns null.
		/// Cheaper to GetSlotGrid then Set for large # of Adds.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement Add(
			this GcInventoryContainer INVENTORY,
			GcInventoryType.InventoryTypeEnum TYPE,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			var grid = GetSlotGrid(INVENTORY);

			libMBIN.NMS.GameComponents.GcInventoryElement item = null;

			// find first available valid slot
			for( var row = 0; row < grid.Length; ++row ) {
				for( var col = 0; col < grid[row].Length; ++col ) {
					if( grid[row][col].Element == null &&
						grid[row][col].IsValid
					) {
						item = NMS.Inventory.CreateElement(TYPE,
							col, row, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
						);
						break;
					}
				}
			}

			// no available valid slot, add to first non-valid free slot
			if( item == null ) {
				for( var row = 0; row < grid.Length; ++row ) {
					for( var col = 0; col < grid[row].Length; ++col ) {
						if( grid[row][col].Element == null ) {
							item = NMS.Inventory.CreateElement(TYPE,
								col, row, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
							);
							break;
						}
					}
				}
			}

			INVENTORY.Slots.Add(item);
			if( !grid[item.Index.Y][item.Index.X].IsValid ) {
				INVENTORY.ValidSlotIndices.Add(item.Index);
			}

			return item;
		}

		/// <summary>
		/// Add a new item with an unassigned Index.
		/// Use for starting exosuit, ship and multitool inventory.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement AddUnindexed(
			this GcInventoryContainer INVENTORY,
			GcInventoryType.InventoryTypeEnum TYPE,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			var item = NMS.Inventory.CreateElement(TYPE,
				-1, -1, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
			INVENTORY.Slots.Add(item);
			return item;
		}

		//...........................................................

		/// <summary>
		/// Set the contents of a specific slot.
		/// Adds if not used, else overwrites.
		/// ID one of: GcTechnologyTableEnum.TechnologyEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement SetTechnology(
			this GcInventoryContainer INVENTORY,
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Set(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Technology,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Add a new item in the first available valid slot.
		/// If no valid slots left then try to add to first unused slot.
		/// If no unused slots then returns null.
		/// Cheaper to GetSlotGrid then call Set for large # of Adds.
		/// ID one of: GcTechnologyTableEnum.TechnologyEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement AddTechnology(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Add(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Technology,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		public static libMBIN.NMS.GameComponents.GcInventoryElement AddTechnologyUnindexed(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return AddUnindexed(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Technology,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Set the contents of a specific slot.
		/// Adds if not used, else overwrites.
		/// ID one of: GcProductTableEnum.gcproducttableEnumEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement SetProduct(
			this GcInventoryContainer INVENTORY,
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Set(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Product,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Add a new item in the first available valid slot.
		/// If no valid slots left then try to add to first unused slot.
		/// If no unused slots then returns null.
		/// Cheaper to GetSlotGrid then call Set for large # of Adds.
		/// ID one of: GcProductTableEnum.gcproducttableEnumEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement AddProduct(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Add(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Product,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		public static libMBIN.NMS.GameComponents.GcInventoryElement AddProductUnindexed(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return AddUnindexed(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Product,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Set the contents of a specific slot.
		/// Adds if not used, else overwrites.
		/// ID one of: GcSubstanceTableEnum.gcsubstancetableEnumEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement SetSubstance(
			this GcInventoryContainer INVENTORY,
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Set(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Substance,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Add a new item in the first available valid slot.
		/// If no valid slots left then try to add to first unused slot.
		/// If no unused slots then returns null.
		/// Cheaper to GetSlotGrid then call Set for large # of Adds.
		/// ID one of: GcSubstanceTableEnum.gcsubstancetableEnumEnum in string form.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement AddSubstance(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return Add(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Substance,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		public static libMBIN.NMS.GameComponents.GcInventoryElement AddSubstanceUnindexed(
			this GcInventoryContainer INVENTORY,
			string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return AddUnindexed(INVENTORY,
				GcInventoryType.InventoryTypeEnum.Substance,
				ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}
	}
}

//=============================================================================
