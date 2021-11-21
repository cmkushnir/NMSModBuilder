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
	public static class Inventory
	{
		/// <summary>
		/// Create new inventory object.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement CreateElement(
			GcInventoryType.InventoryTypeEnum TYPE,
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			var item = new libMBIN.NMS.GameComponents.GcInventoryElement {
				Type           = new() { InventoryType = TYPE },
				Index          = new() { X = X, Y = Y },
				FullyInstalled = true,
				Id             = ID,
				Amount         = AMOUNT,
				MaxAmount      = MAX_AMOUNT,
				DamageFactor   = DAMAGE_FACTOR,
			};
			return item;
		}

		//...........................................................

		/// <summary>
		/// Create new tech inventory object.
		/// Cannot go in bulk storage e.g. exo-suit cargo, containers, ...
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement CreateTechnology(
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return CreateElement(
				GcInventoryType.InventoryTypeEnum.Technology,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Create new product inventory object.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement CreateProduct(
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return CreateElement(
				GcInventoryType.InventoryTypeEnum.Product,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}

		//...........................................................

		/// <summary>
		/// Create new substance inventory object.
		/// </summary>
		public static libMBIN.NMS.GameComponents.GcInventoryElement CreateSubstance(
			int X, int Y, string ID,
			int AMOUNT, int MAX_AMOUNT,
			float DAMAGE_FACTOR = 0
		){
			return CreateElement(
				GcInventoryType.InventoryTypeEnum.Substance,
				X, Y, ID, AMOUNT, MAX_AMOUNT, DAMAGE_FACTOR
			);
		}
	}
}

//=============================================================================
