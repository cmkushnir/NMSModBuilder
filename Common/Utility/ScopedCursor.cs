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
using System.Windows;
using System.Windows.Input;

//=============================================================================

namespace cmk
{
    public class ScopedCursor
	: System.IDisposable
	{
		protected Cursor m_orig        = null;
		protected bool   m_is_disposed = true;

		public ScopedCursor( Cursor CURSOR )  // e.g. Cursors.Wait;
		{
			Application.Current.Dispatcher.Invoke(() => {
				m_orig = Mouse.OverrideCursor;
				Mouse.OverrideCursor = CURSOR;
				m_is_disposed = false;
			});
		}

		~ScopedCursor() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose( bool DISPOSING )
		{
			if( m_is_disposed ) return;
			m_is_disposed = true;
			Application.Current.Dispatcher.Invoke(() => {
				Mouse.OverrideCursor = m_orig;
				m_orig = null;
			});
		}

	}
}

//=============================================================================
