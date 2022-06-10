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

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

//=============================================================================

namespace cmk
{
	public static partial class _x_
	{
		/// <summary>
		/// Wait for TASK to complete while still pumping messages for thread.
		/// </summary>
		public static void Pump(
			this Task         TASK,
			CancellationToken CANCEL = default
		){
			// run TASK async, when TASK completes set nested.Continue = false
			var nested = new DispatcherFrame();
			TASK.ContinueWith(_ => nested.Continue = false, TaskScheduler.Default);
			Dispatcher.PushFrame(nested);  // pump until nested.Continue = false
			TASK.Wait(CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Wait for TASK to complete while still pumping messages for thread.
		/// </summary>
		public static T Pump<T>(
			this Task<T>      TASK,
			CancellationToken CANCEL = default
		){
			var nested = new DispatcherFrame();
			TASK.ContinueWith(_ => nested.Continue = false, TaskScheduler.Default);
			Dispatcher.PushFrame(nested);
			return TASK.Result(CANCEL);
		}

		//...........................................................

		/// <summary>
		/// Wait for TASK to complete.
		/// </summary>
        public static T Result<T>( this Task<T> TASK, CancellationToken CANCEL = default )
        {
			//TASK.Wait(CANCEL);
			return TASK.Result;
        }
	}
}

//=============================================================================
