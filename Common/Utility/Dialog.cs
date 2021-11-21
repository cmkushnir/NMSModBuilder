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
	// Can be called from SCripts where using UI classes from the common lib
	// can give odd error about not finding DependencyObject;
	// Believe due to common saying it needs core 5 but using 5.0.x at runtime.
	// We load runtime into roslyn host so it wants 5.0 and find 5.0.x and complains.
	public static class Dialog
	{
		public static string SelectFolder()
		{
			var path = "";
			Application.Current?.Dispatcher.Invoke(() => {
				var dialog = new cmk.SelectFolderDialog();
				if( dialog.ShowDialog() == true ) path = dialog.Path;
			});
			return path;
		}

		//...........................................................

		public static string OpenFile(
			string INITIAL_DIRECTORY = null,
			string INITIAL_FILEPATH  = null
		){
			var path = "";
			Application.Current?.Dispatcher.Invoke(() => {
				var dialog = new Microsoft.Win32.OpenFileDialog {
					InitialDirectory = INITIAL_DIRECTORY,
					FileName         = INITIAL_FILEPATH,
					Multiselect      = false,
				};
				if( dialog.ShowDialog() == true ) path = dialog.FileName;
			});
			return path;
		}

		//...........................................................

		public static string SaveFile(
			string INITIAL_DIRECTORY = null,
			string INITIAL_FILEPATH  = null,
			bool   CREATE_PROMPT     = true
		){
			var path = "";
			Application.Current?.Dispatcher.Invoke(() => {
				var dialog = new Microsoft.Win32.SaveFileDialog {
					InitialDirectory = INITIAL_DIRECTORY,
					FileName         = INITIAL_FILEPATH,
					CreatePrompt     = CREATE_PROMPT,
				};
				if( dialog.ShowDialog() == true ) path = dialog.FileName;
			});
			return path;
		}
	}
}

//=============================================================================
