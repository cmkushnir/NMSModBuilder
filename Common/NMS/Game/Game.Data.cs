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
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

//=============================================================================

namespace cmk.NMS.Game
{
    /// <summary>
    /// Hold all state related to a game instance.
    /// </summary>
    public partial class Data
	: cmk.NMS.PAK.Item.ICollection
	{
		public delegate void LanguageChangedEventHandler(
			NMS.Game.Language.Identifier OLD,
			NMS.Game.Language.Identifier NEW
		);
		public event LanguageChangedEventHandler LanguageChanged;

		//...........................................................

		/// <summary>
		/// protected - force construction through static Create method in order
		/// to ensure that any Game instances are valid at time of creation.
		/// </summary>
		protected Data( Location.Data LOCATION, Language.Identifier LANGUAGE_ID = null )
		{
			if( LANGUAGE_ID == null ) LANGUAGE_ID = NMS.Game.Language.Identifier.Default;

			Log.Default.AddHeading($"Creating {GetType().FullName} from {LOCATION.Path}");

			Location = LOCATION;

			var release = Location.Release;
			if( release.MbincVersion != MBINC.Linked.Version ) {
				Log.Default.AddWarning(
					$"Incorrect libMBIN.dll for selected game, have {MBINC.Linked.Version} need {release.MbincVersion}.\n" +
					$"May prompt to download required libMBIN.dll, but it wil have wrong name and cannot be hot swapped with loaded libMBIN.dll.\n" +
					$"Need to close app and manually update; will continue with reduced functionality."
				);
			}

			// download|load|cache instance.
			var mbinc  = MBINC.LoadRelease(release);
			if( mbinc == null ) return;

			// create a game instance specific copy of libMBIN wrapper
			// so we can assoc game instance specific mbin's with its classes.
			// i.e. multiple game instances may use same libMBIN version,
			//      but may have different mbins.
			Mbinc = new(mbinc.Assembly);

			PCBANKS = new(this, LANGUAGE_ID);  // starts Task to build merged info tree
			MODS    = new(this, LANGUAGE_ID);

			LinkMbinClasses();  // link PCBANKS pak mbin items to Mbinc classes

			// wait for merged PCBANKS.InfoTree to be built
			var tree = PCBANKS.InfoTree;
		}

		//...........................................................

		public readonly Location.Data Location;
		public readonly MBINC         Mbinc;

		//...........................................................

		/// <summary>
		/// Path\GAMEDATA\PCBANKS\*.pak
		/// </summary>
		public Game.PCBANKS.Files PCBANKS { get; protected set; }

		/// <summary>
		/// Path\GAMEDATA\PCBANKS\MODS\*.pak
		/// </summary>
		public Game.MODS.Files MODS { get; protected set; }

		//...........................................................

		/// <summary>
		/// Currently selected language for game instance.
		/// </summary>
		public Language.Identifier LanguageId {
			get { return PCBANKS.LanguageId; }
			set {
				if( value == null ) {
					value  = NMS.Game.Language.Identifier.Default;
				}

				var old  = LanguageId;
				if( old == value ) return;

				PCBANKS.LanguageId = value;
				MODS   .LanguageId = value;

				LanguageChanged?.Invoke(old, value);
			}
		}

		//...........................................................

		public void ClearEbinCache()
		{
			Log.Default.AddInformation("Clearing ebin cache ...");
			Parallel.Invoke(
				() => PCBANKS.ClearEbinCache(),
				() => MODS   .ClearEbinCache()
			);
			GC.Collect();  // for user perception
			GC.WaitForPendingFinalizers();
			Log.Default.AddInformation("Cleared ebin cache.");
		}

		//...........................................................

		/// <summary>
		/// When a pak file is loaded it parses all mbin headers.
		/// LinkMbinClasses links the pak mbin items to the
		/// MBINC class specified in the mbin header.
		/// </summary>
		protected void LinkMbinClasses()
		{
			Log.Default.AddInformation($"Linking MBIN paths to libMBIN classes");

			// ForEachInfo does parallel ForEachInfo on each PAK file, does not use merged tree.
			PCBANKS.ForEachInfo(( INFO, CANCEL, LOG ) => {
				if( INFO.MbinHeader == null) return;

				var class_info  = Mbinc.FindClass(INFO.MbinHeader?.ClassName);
				if( class_info == null ) return;

				// don't link mbin's to classes if guid's don't match,
				// but still allow mbin's to be explicitly selected|viewed.
				if( class_info.NMSAttributeGUID != INFO.MbinHeader.ClassGuid ) {
					if( NMS.PAK.MBIN.Data.LogGuidMismatch ) {
						Log.Default.AddWarning($"{INFO.Path} mbin GUID {INFO.MbinHeader.ClassGuid:x16} != struct GUID {class_info.NMSAttributeGUID:x16}");
					}
					return;
				}

				lock( class_info.PakItems ) class_info.PakItems.Add(INFO.Path);
			},	default);

			Mbinc.Classes.ForEach(CLASS => CLASS.PakItems.Sort());

			Log.Default.AddInformation($"Linked MBIN paths to libMBIN classes");
		}

		//...........................................................

		public bool Launch()
		{
			if( Location.IsValid ) try {
				ClearEbinCache();
				using( Process process = new() ) {
					process.StartInfo.FileName = Location.ExePath;
					//	process.StartInfo.Arguments = "";
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow  = true;
					process.Start();
				}
				return true;
			}
			catch( Exception EX ) { Log.Default.AddFailure(EX); }
			return false;
		}

		//...........................................................
		// cmk.NMS.PAK.Item.ICollection
		//...........................................................

		/// <summary>
		/// MODS.FindInfo then PCBANKS.FindInfo i.e. like game
		/// </summary>
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false )
		{
			if( NORMALIZE ) PATH = NMS.PAK.Item.Path.Normalize(PATH);

			var found  = MODS.FindInfo(PATH, false);
			if( found != null ) return found;

			return PCBANKS.FindInfo(PATH, false);
		}

		//...........................................................

		/// <summary>
		/// MODS.FindInfo then PCBANKS.FindInfo i.e. like game
		/// </summary>
		public List<NMS.PAK.Item.Info> FindInfo( Predicate<NMS.PAK.Item.Info> MATCH, bool SORT = true )
		{
			var list = new List<NMS.PAK.Item.Info>();

			var files_list = MODS.FindInfo(MATCH, false);
			list.Capacity += files_list.Count;
			list.AddRange(files_list);

			files_list = PCBANKS.FindInfo(MATCH, false);
			list.Capacity += files_list.Count;
			list.AddRange(files_list);

			if( SORT ) list.Sort();
			return list;
		}

		//...........................................................

		public List<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoStartsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoContains( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoContains(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoEndsWith( string PATTERN, bool SORT = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoEndsWith(PATTERN, SORT);

		public List<NMS.PAK.Item.Info> FindInfoRegex( string PATTERN, bool SORT = true, bool WHOLE_WORDS = false, bool CASE_SENS = true, bool PATTERN_IS_REGEX = true )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfoRegex(PATTERN, SORT, WHOLE_WORDS, CASE_SENS, PATTERN_IS_REGEX);

		public List<NMS.PAK.Item.Info> FindInfo( Regex REGEX, bool SORT = true, bool WHOLE_WORDS = false )
		=> ((NMS.PAK.Item.ICollection)this).DefaultFindInfo(REGEX, SORT, WHOLE_WORDS);

		//...........................................................

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		=> ((NMS.PAK.Item.ICollection)this).DefaultExtractData(PATH, NORMALIZE, LOG, CANCEL);

		/// <summary>
		/// Loop through all PakCollections for first with PATH (case-sensitive).
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in any game or mod *.pak file");
				return null;
			}
			return info.ExtractData<AS_T>(LOG);
		}

		//...........................................................

		/// <summary>
		/// Loop through all PakCollections for first with PATH.
		/// Return extracted raw data converted to mbin AS_T.
		/// Note: PakCollections[0] == MODS, PakCollections[1] == PCBANKS
		/// i.e. get pak item the way the game does.
		/// </summary>
		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class // libMBIN.NMSTemplate
		{
			var info  = FindInfo(PATH, NORMALIZE);
			if( info == null ) {
				LOG.AddFailure($"{PATH} - unable to find info in any game or mod *.pak file");
				return null;
			}
			return info.ExtractMbin<AS_T>(LOG);
		}

		//...........................................................

		/// <summary>
		/// PCBANKS.ForEachInfo then MODS.ForEachInfo
		/// </summary>
		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PCBANKS.ForEachInfo(HANDLER, LOG, CANCEL);
			MODS   .ForEachInfo(HANDLER, LOG, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// PCBANKS.ForEachData then MODS.ForEachData
		/// </summary>
		public void ForEachData(
			Action<NMS.PAK.Item.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PCBANKS.ForEachData(HANDLER, LOG, CANCEL);
			MODS   .ForEachData(HANDLER, LOG, CANCEL);
		}

		//...........................................................

		/// <summary>
		/// PCBANKS.ForEachMbin then MODS.ForEachMbin
		/// </summary>
		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		){
			PCBANKS.ForEachMbin(HANDLER, LOG, CANCEL);
			MODS   .ForEachMbin(HANDLER, LOG, CANCEL);
		}

		//...........................................................
		// lookup cached data, first in MODS then PCBANKS
		//...........................................................

		/// <summary>
		/// ID is a language entry ID or substance|product|technology ID.
		/// If ID is a substance|product|technology ID return <NameId, Name>.
		/// If ID is a language ID return <ID, text>.
		/// </summary>
		public Language.Data FindLanguageData( string ID )
		{
			var    data = MODS.FindLanguageData(ID);
			return data == null ? PCBANKS.FindLanguageData(ID) : data;
		}

		//...........................................................

		/// <summary>
		/// Find Substance, Product, Technology by it's ID.
		/// </summary>
		public Items.Data FindItemData( string ID )
		{
			var    data = MODS.FindItemData(ID);
			return data == null ? PCBANKS.FindItemData(ID) : data;
		}

		//...........................................................

		/// <summary>
		/// Find Refiner or Cooking recipe by it's ID.
		/// </summary>
		public Recipes.Data FindRecipeData( string ID )
		{
			var    data = MODS.FindRecipeData(ID);
			return data == null ? PCBANKS.FindRecipeData(ID) : data;
		}
	}
}

//=============================================================================
