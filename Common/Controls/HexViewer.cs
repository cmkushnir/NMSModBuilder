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

using System.IO;

//=============================================================================

namespace cmk
{
    public class HexViewer  // todo
	: cmk.TextViewer
	{
		public HexViewer( Stream RAW = null ) : base()
		{
			Raw = RAW;
		}

		//...........................................................

		public Stream Raw {
			get;
			set;
		}
	}
}

//=============================================================================
