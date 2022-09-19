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



//=============================================================================

namespace cmk.NMS.PAK.Hex
{
    public class Viewer
	: cmk.HexViewer
	, Item.IViewer
	{
		public Viewer() : this(null) {}

		public Viewer( Item.Data DATA, Log LOG = null ) : base()
		{
			Data = DATA;
		}

		//...........................................................

		public ImageButton ViewerButton { get; } = new() {
			ToolTip = "Hex",
			Uri     = Resource.Uri("PakItemHex.png")
		};

		//...........................................................

		protected NMS.PAK.Item.Data m_data;

		public NMS.PAK.Item.Data Data {
			get { return m_data; }
			set {
				if( m_data == value ) return;
				m_data          = value;
				SourceLabel.Text = m_data?.FilePath?.NameExt;
				Raw             = m_data?.Raw;
			}
		}
	}
}

//=============================================================================
