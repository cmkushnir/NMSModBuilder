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

namespace cmk
{
    public static class Dialog
	{
		public static string SelectFolder()
		{
			var path   = "";
			var action = () => {
				var dialog = new cmk.SelectFolderDialog();
				if( dialog.ShowDialog() == true ) path = dialog.Path;
			};
			action.DispatcherInvoke();
			return path;
		}

		//...........................................................

		public static string OpenFile(
			string INITIAL_DIRECTORY = null,
			string INITIAL_FILEPATH  = null
		){
			var path   = "";
			var action = () => {
				var dialog = new Microsoft.Win32.OpenFileDialog {
					InitialDirectory = INITIAL_DIRECTORY,
					FileName         = INITIAL_FILEPATH,
					Multiselect      = false,
				};
				if( dialog.ShowDialog() == true ) path = dialog.FileName;
			};
			action.DispatcherInvoke();
			return path;
		}

		//...........................................................

		public static string SaveFile(
			string INITIAL_DIRECTORY = null,
			string INITIAL_FILEPATH  = null,
			bool   CREATE_PROMPT     = true
		){
			var path   = "";
			var action = () => {
				var dialog = new Microsoft.Win32.SaveFileDialog {
					InitialDirectory = INITIAL_DIRECTORY,
					FileName         = INITIAL_FILEPATH,
					CreatePrompt     = CREATE_PROMPT,
				};
				if( dialog.ShowDialog() == true ) path = dialog.FileName;
			};
			action.DispatcherInvoke();
			return path;
		}

		//...........................................................

		public static string TextPrompt(
			string TITLE = null,
			string TEXT  = null
		){
			var text   = TEXT;
			var action = () => {
				var dialog = new cmk.TextBoxDialog {
					Title = TITLE ?? "Enter value:",
					Text  = TEXT  ?? ""
				};
				if( dialog.ShowDialog() == true ) text = dialog.Text;
			};
			action.DispatcherInvoke();
			return text;
		}
	}
}

//=============================================================================
