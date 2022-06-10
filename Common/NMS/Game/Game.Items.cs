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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

using libMBIN.NMS.GameComponents;

//=============================================================================

namespace cmk.NMS.Game.Items
{
	/// <summary>
	/// Base for substance, product, technology Data item.
	/// </summary>
	public class Data
	: System.ComponentModel.INotifyPropertyChanged
	{
		protected void PropertyChangedInvoke( [CallerMemberName] string NAME = "" )
		{
			PropertyChanged?.Invoke(this, new(NAME));
		}
		public event PropertyChangedEventHandler PropertyChanged;

		// hack: store item type for each item instead of relying on collection they are in.
		//       usuful when look up by ID in all collections and return item data.
		// todo: in order to to move to no linked libMBIN will need to get rid of all libMBIN types in code.
		public GcInventoryType.InventoryTypeEnum ItemType = GcInventoryType.InventoryTypeEnum.Product;

		// from reality/tables/*.mbin, language key strings
		public string Id            { get; set; }
		public string NameId        { get; set; }
		public string DescriptionId { get; set; }

		// Id's converted for specific language
		protected string m_name;
		protected string m_desc;

		/// <summary>
		/// Updated when current language changes.
		/// </summary>
		public string Name {
			get { return m_name; }
			set {
				if( m_name != value ) {
					m_name  = value;
					PropertyChangedInvoke();
				}
			}
		}

		/// <summary>
		/// Updated when current language changes.
		/// </summary>
		public string Description {
			get { return m_desc; }
			set {
				if( m_desc != value ) {
					m_desc  = value;
					PropertyChangedInvoke();
				}
			}
		}

		// from enum, so always english
		public string CategoryName { get; set; }

		// do the scaling on load instead of on display
		public string      IconPath { get; set; }
		public ImageSource Icon32   { get; set; }
		public ImageSource Icon48   { get; set; }
		public ImageSource Icon64   { get; set; }

		//=====================================================================

		/// <summary>
		/// For use in list control to group by category.
		/// </summary>
		public class CategoryComparer
		: System.Collections.IComparer
		, System.Collections.Generic.IComparer<Data>
		{
			public int Compare( object LHS, object RHS )
			{
				return Compare(LHS as Data, RHS as Data);
			}

			public int Compare( Data LHS, Data RHS )
			{
				var cmp  = string.Compare(LHS.CategoryName, RHS.CategoryName);
				if( cmp == 0 ) cmp = String.CompareNumeric(LHS.Id, RHS.Id);
				return cmp;
			}
		}
	}

	//=============================================================================

	/// <summary>
	/// Base for substance, product, technology table data.
	/// A snapshot of the data for the Game specified in constructor.
	/// Once constructed cannot change Game, once Loaded cannot reload.
	/// </summary>
	public class Collection<DATA_T>
	where DATA_T : cmk.NMS.Game.Items.Data
	{
		protected readonly List<DATA_T>         m_list;
		protected readonly ManualResetEventSlim m_built = new(false);

		//...........................................................

		public Collection( Game.Data GAME, int CAPACITY, NMS.PAK.Item.ICollection PAK_ITEM_COLLECTION = null )
		{
			Game   = GAME;
			m_list = new(CAPACITY);
			IPakItemCollection = PAK_ITEM_COLLECTION ?? Game.PCBANKS;
		}

		//...........................................................

		public readonly NMS.Game.Data Game;

		// PakItemCollection used to extract and load items from mbin's.
		// In general one of: Game, Game.PCBANKS (default), Game.Mods, Game.Mods[i]
		public readonly NMS.PAK.Item.ICollection IPakItemCollection;

		//...........................................................

		/// <summary>
		/// Blocks until Load() complete.
		/// </summary>
		public IReadOnlyList<DATA_T> List {
			get { return m_built.Wait(int.MaxValue) ? m_list : null; }
		}

		//...........................................................

		/// <summary>
		/// Blocks until Load() complete.
		/// </summary>
		public Data Find( string ID )  // case-sensitive
		{
			return List.Bsearch(ID,
				(ITEM, KEY) => String.CompareNumeric(ITEM.Id, KEY)
			);
		}

		//...........................................................

		protected virtual void LoadMBIN()
		{
			// derived
		}

		public void Load()
		{
			if( !m_built.IsSet ) try {
					m_list.Clear();  // doesn't reduce capacity
					if( (Game?.Language.List.Count ?? 0) < 1 ) return;

					Log.Default.AddInformation($"Loading {GetType().FullName}");

					LoadMBIN();  // derived specific
					m_list.Sort(( LHS, RHS ) => String.CompareNumeric(LHS.Id, RHS.Id));

					Log.Default.AddInformation($"Loaded {GetType().FullName} - {m_list.Count} items");

					UpdateLanguage(Game.Language);
					Game.LanguageChanged += (OLD, NEW) => UpdateLanguage(NEW);
				}
				finally { m_built.Set(); }
		}

		//...........................................................

		/// <summary>
		/// Called when Game language changes.
		/// Updates Name and Description for each item.
		/// </summary>
		protected void UpdateLanguage( Game.Language.Collection LANGUAGE )
		{
			if( LANGUAGE == null ) return;
			_ = Parallel.ForEach(m_list, ITEM => {
				ITEM.Name        = LANGUAGE.GetText(ITEM.NameId,        ITEM.NameId);
				ITEM.Description = LANGUAGE.GetText(ITEM.DescriptionId, ITEM.DescriptionId)
								 ?.Replace("\r\n", "\n")
								  .Replace("\n \n", "\n")
								  .Replace("\n\n", "\n");
			});
			Log.Default.AddInformation($"Updated {GetType().FullName} {LANGUAGE.Identifier.Name}");
		}
	}
}

//=============================================================================
