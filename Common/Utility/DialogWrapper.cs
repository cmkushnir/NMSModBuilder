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

using System.Windows;

//=============================================================================

namespace cmk
{
    public class DialogWrapper<DIALOG_T>
	where DIALOG_T : System.Windows.Window, new()
	{
		public DIALOG_T Dialog { get; protected set; }

		//...........................................................

		public DialogWrapper()
		{
			Application.Current?.Dispatcher.Invoke(() => {
				Dialog = new DIALOG_T();
			});
		}

		//...........................................................

		public bool Show()
		{
			var result = false;
			Application.Current?.Dispatcher.Invoke(() => {
				result = Dialog.ShowDialog() ?? false;
			});
			return result;
		}

		//...........................................................

		public object Invoke( string NAME, params object[] PARAMS )
		{
			object result = null;
			Application.Current?.Dispatcher.Invoke(() => {
				var method = Dialog.GetType().GetMethod(NAME);
				result = method?.Invoke(Dialog, PARAMS);
			});
			return result;
		}

		//...........................................................

		public TYPE_T Field<TYPE_T>( string NAME )
		{
			TYPE_T value = default;
			Application.Current?.Dispatcher.Invoke(() => {
				var field = Dialog.GetType().GetField(NAME);
				value = (TYPE_T)field?.GetValue(Dialog);
			});
			return value;
		}

		public bool Field<TYPE_T>( string NAME, TYPE_T VALUE )
		{
			var result = false;
			Application.Current?.Dispatcher.Invoke(() => {
				var field  = Dialog.GetType().GetField(NAME);
				if( field != null ) {
					field?.SetValue(Dialog, VALUE);
					result = true;
				}
			});
			return result;
		}

		//...........................................................

		public TYPE_T Property<TYPE_T>( string NAME )
		{
			TYPE_T value = default;
			Application.Current?.Dispatcher.Invoke(() => {
				var prop = Dialog.GetType().GetProperty(NAME);
				value = (TYPE_T)prop?.GetValue(Dialog);
			});
			return value;
		}

		public bool Property<TYPE_T>( string NAME, TYPE_T VALUE )
		{
			var result = false;
			Application.Current?.Dispatcher.Invoke(() => {
				var prop  = Dialog.GetType().GetProperty(NAME);
				if( prop != null ) {
					prop?.SetValue(Dialog, VALUE);
					result = true;
				}
			});
			return result;
		}
	}
}

//=============================================================================
