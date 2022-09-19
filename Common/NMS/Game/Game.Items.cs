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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

		//...........................................................

		public Data( Collection COLLECTION, GcInventoryType.InventoryTypeEnum TYPE )
		{
			Collection = COLLECTION;
			ItemType   = TYPE;
		}

		//...........................................................

		public readonly Collection Collection;

		// hack: store item type for each item instead of relying on collection they are in.
		//       usuful when look up by ID in all collections and return item data.
		// todo: in order to to move to no linked libMBIN will need to get rid of all libMBIN types in code.
		public readonly GcInventoryType.InventoryTypeEnum ItemType = GcInventoryType.InventoryTypeEnum.Product;

		// from enum, so always english
		public string CategoryName { get; set; }

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
		/// Use METADATA/UI/SPECIALSTYLESDATA.MBIN to convert tags to (animated) colors.
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

		// do the scaling on load instead of on display
		public NMS.PAK.Item.Info IconInfo { get; set; }
		public ImageSource       Icon32   { get; set; }
		public ImageSource       Icon48   { get; set; }
		public ImageSource       Icon64   { get; set; }

		public struct Requirement
		{
			public GcInventoryType.InventoryTypeEnum Type { get; set; }
			public string              Id     { get; set; }
			public int                 Amount { get; set; }
			public NMS.Game.Items.Data Data   { get; set; }  // lookup using Type & Id
		}
		public Requirement[] Requirements { get; set; }

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

		public NMS.PAK.Item.Info ItemInfo { get; protected set; } = null;

		//...........................................................

		public virtual NMS.PAK.Item.Info FindItemInfo()
		=> null;

		//...........................................................

		public Data Find( string ID )  // case-sensitive
		{
			Lock.AcquireRead();
			try {
				return this.Bsearch(ID,
					( ITEM, KEY ) => String.CompareNumeric(ITEM.Id, KEY)
				);
			}
			finally { Lock.ReleaseRead(); }
		}

		//...........................................................

		public void Reset()
		{
			Lock.AcquireWrite();
			this.Clear();
			ItemInfo = null;
			Lock.ReleaseWrite();
		}

		public void Load()
		{
			// only (re)load if recipe mbin in diff pak.
			var item_info = FindItemInfo();

			Lock.AcquireWrite();		
			try {
				if( NMS.PAK.Item.Info.Equals(item_info, ItemInfo) ) return;
				ItemInfo = item_info;

				Log.Default.AddInformation($"Loading {GetType().FullName}");

				this.Clear();
				LoadMBIN();
				this.Sort(( LHS, RHS ) => String.CompareNumeric(LHS.Id, RHS.Id));

				Log.Default.AddInformation($"Loaded {GetType().FullName} - {this.Count} items");
			}
			finally { Lock.ReleaseWrite(); }

			UpdateLanguage(NMS.Game.Language.Identifier.Default);
		}

		//...........................................................

		protected virtual void LoadMBIN()
		{
		}

		//...........................................................

		public void LinkRequirements()
		{
			if( Count < 1 ) return;  // don't get lang if no items, could cause lang Load

			_ = Parallel.ForEach(this, ITEM => {
				for( var i = 0; i < ITEM.Requirements.Length; ++i ) {
					// ITEM.Requirements is struct
					var data  = Cache?.FindItemData(ITEM.Requirements[i].Type, ITEM.Requirements[i].Id);
					if( data == null ) {
						data  = Cache?.FindItemData(ITEM.Requirements[i].Id);
						if( data != null ) {  // fix type error
							ITEM.Requirements[i].Type = data.ItemType;
						}
					}
					ITEM.Requirements[i].Data = data;
				}
			});

			CollectionChanged?.DispatcherInvoke(this,
				new NotifyCollectionChangedEventArgs(
					NotifyCollectionChangedAction.Reset
				)
			);
		}

		//...........................................................

		/// <summary>
		/// Called when Game language changes.
		/// Updates Name and Description for each item.
		/// </summary>
		public void UpdateLanguage( NMS.Game.Language.Identifier LANGUAGE_ID )
		{
			if( Count < 1 ) return;  // don't get lang if no items, could cause lang Load

			var language  = Cache.Languages.Get(LANGUAGE_ID);
			if( language == null ) return;

			Lock.AcquireWrite();

			_ = Parallel.ForEach(this, ITEM => {
				ITEM.Name        = language.GetText(ITEM.NameId,        ITEM.NameId);
				ITEM.Description = language.GetText(ITEM.DescriptionId, ITEM.DescriptionId)
								 ?.Replace("\r\n", "\n")
								  .Replace("\n \n", "\n")
								  .Replace("\n\n", "\n")
								  .TrimEnd('\n');
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
}

//=============================================================================
