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
using System.Text.RegularExpressions;
using System.Threading;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
    public interface ICollection
	{
		//...........................................................
		// Find
		//...........................................................

		public      NMS.PAK.Item.Info  FindInfo( string PATH, bool NORMALIZE = false );
		public List<NMS.PAK.Item.Info> FindInfo( Predicate<NMS.PAK.Item.Info> MATCH, bool SORT = true );
		public List<NMS.PAK.Item.Info> FindInfoStartsWith( string PATTERN, bool SORT = true );
		public List<NMS.PAK.Item.Info> FindInfoContains  ( string PATTERN, bool SORT = true );
		public List<NMS.PAK.Item.Info> FindInfoEndsWith  ( string PATTERN, bool SORT = true );
		public List<NMS.PAK.Item.Info> FindInfoRegex     ( string PATTERN, bool SORT = true, bool WHOLE_WORDS = false, bool CASE_SENS = true, bool PATTERN_IS_REGEX = true );
		public List<NMS.PAK.Item.Info> FindInfo          ( Regex  REGEX,   bool SORT = true, bool WHOLE_WORDS = false );

		public List<NMS.PAK.Item.Info> DefaultFindInfoStartsWith( string PATTERN, bool SORT = true )
		{
			if( PATTERN.IsNullOrEmpty() ) return new();
			return FindInfo(INFO => INFO.Path.Full.StartsWith(PATTERN), SORT);
		}

		public List<NMS.PAK.Item.Info> DefaultFindInfoContains( string PATTERN, bool SORT = true )
		{
			if( PATTERN.IsNullOrEmpty() ) return new();
			return FindInfo(INFO => INFO.Path.Full.Contains(PATTERN), SORT);
		}

		public List<NMS.PAK.Item.Info> DefaultFindInfoEndsWith( string PATTERN, bool SORT = true )
		{
			if( PATTERN.IsNullOrEmpty() ) return new();
			return FindInfo(INFO => INFO.Path.Full.EndsWith(PATTERN), SORT);
		}

		public List<NMS.PAK.Item.Info> DefaultFindInfoRegex( string PATTERN, bool SORT = true, bool WHOLE_WORDS = false, bool CASE_SENS = true, bool PATTERN_IS_REGEX = true )
		{
			Regex  regex = null;
			try  { regex = PATTERN.CreateRegex(CASE_SENS, PATTERN_IS_REGEX); }
			catch( Exception EX ) {
				Log.Default.AddFailure(EX, $"{GetType().FullName}:\n");
				return new();
			}
			return DefaultFindInfo(regex, SORT, WHOLE_WORDS);
		}

		public List<NMS.PAK.Item.Info> DefaultFindInfo( Regex REGEX, bool SORT = true, bool WHOLE_WORDS = false )
		{
			if( REGEX == null ) return new();
			return FindInfo(INFO => {
				try {
					var path = INFO.Path;
					if( !WHOLE_WORDS ) {
						return REGEX.IsMatch(path);
					}
					else {  // WHOLE_WORDS
						var matches = REGEX.Matches(path);
						foreach( Match match in matches ) {
							var match_end = match.Index + match.Length;
							if( !IsWordBorder(path, match.Index - 1) ||
								!IsWordBorder(path, match_end)
							)	continue;
							return true;
						}
						return false;
					}
				}
				catch( Exception EX ) {
					Log.Default.AddFailure(EX, $"{GetType().FullName}:\n");
					return false;
				}
			},	SORT);
		}

		protected bool IsWordBorder( string TEXT, int OFFSET )
		{
			if( OFFSET < 0 || OFFSET >= TEXT.Length ) return true;
			return !Char.IsLetterOrDigit(TEXT[OFFSET]);
		}

		//...........................................................
		// Extract
		//...........................................................

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default );

		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : NMS.PAK.Item.Data;

		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		where  AS_T : class;  // libMBIN.NMSTemplate

		public NMS.PAK.Item.Data DefaultExtractData( string PATH, bool NORMALIZE = false, Log LOG = null, CancellationToken CANCEL = default )
		=> ExtractData<NMS.PAK.Item.Data>(PATH, NORMALIZE, LOG, CANCEL);

		//...........................................................
		// ForEach - Intended to query, not modify.
		// e.g. if ForEachMbin used in a mod script, modified mbin
		// Data objects will not be cached, so the mbin's will not
		// be included in any built mod pak.
		//...........................................................

		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		);

		public void ForEachData(
			Action<NMS.PAK.Item.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		);

		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, Log, CancellationToken> HANDLER,
			Log LOG = null, CancellationToken CANCEL = default
		);
	}

	//=========================================================================

	// PCBANKS, and each mod pak file
	public interface INamedCollection
	: ICollection
	{
		public string                  PakItemCollectionName { get; }
		public NMS.PAK.Item.Info.Node  PakItemCollectionTree { get; }  // may be null
	}
}

//=============================================================================
