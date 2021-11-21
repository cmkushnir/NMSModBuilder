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
using System.Windows.Media.Imaging;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
	public interface ICollection
	{
		//...........................................................
		// Find
		//...........................................................

		public NMS.PAK.Item.Info FindInfo( Predicate<NMS.PAK.Item.Info> MATCH );

		//...........................................................

		// info.Path.CompareTo(PATH)
		public NMS.PAK.Item.Info FindInfo( string PATH, bool NORMALIZE = false );

		//...........................................................

		// case-sensitive compare info.Path.Full against PATTERN

		public IEnumerable<NMS.PAK.Item.Info> FindInfoStartsWith ( string PATTERN );
		public IEnumerable<NMS.PAK.Item.Info> FindInfoContains   ( string PATTERN );
		public IEnumerable<NMS.PAK.Item.Info> FindInfoEndsWith   ( string PATTERN );

		//...........................................................

		public IEnumerable<NMS.PAK.Item.Info> FindInfoRegex( string PATTERN )
		{
			Regex  regex = null;
			try  { regex = PATTERN.CreateRegex(true, true); }
			catch( Exception EX ) {
				Log.Default.AddFailure(EX, $"{GetType().FullName}\n");
			}
			foreach( var info in FindInfoRegex(regex) ) {
				yield return info;
			}
		}

		public IEnumerable<NMS.PAK.Item.Info> FindInfoRegex( Regex REGEX );

		//...........................................................
		// Extract
		//...........................................................

		public NMS.PAK.Item.Data ExtractData( string PATH, bool NORMALIZE = false, Log LOG = null )
		{
			return ExtractData<NMS.PAK.Item.Data>(PATH, NORMALIZE, LOG);
		}

		public AS_T ExtractData<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : NMS.PAK.Item.Data;

		//...........................................................

		public BitmapSource ExtractDdsBitmapSource( string PATH, bool NORMALIZE = false, int HEIGHT = 32, Log LOG = null );

		//...........................................................

		public AS_T ExtractMbin<AS_T>( string PATH, bool NORMALIZE = false, Log LOG = null )
		where  AS_T : class;  // libMBIN.NMSTemplate

		//...........................................................
		// ForEach - Intended to query, not modify.
		// e.g. if ForEachMbin used in a mod script, modified mbin
		// Data objects will not be cached, so the mbin's will not
		// be included in any built mod pak.
		//...........................................................

		public void ForEachInfo(
			Action<NMS.PAK.Item.Info, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		);

		//...........................................................

		public void ForEachData(
			Action<NMS.PAK.Item.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		);

		//...........................................................

		public void ForEachMbin(
			Action<NMS.PAK.MBIN.Data, CancellationToken, Log> HANDLER,
			CancellationToken CANCEL = default, Log LOG = null
		);
	}
}

//=============================================================================
