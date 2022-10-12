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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

//=============================================================================

namespace cmk
{
    public class Log
	: System.Collections.Generic.IReadOnlyList<LogItem>
	, System.Collections.Specialized.INotifyCollectionChanged
	{
		public static readonly Log Default = new() {
			Name = Assembly.GetEntryAssembly().GetName().Name
		};

		// Add() also writes to DefaultFile if non-null
		public static StreamWriter DefaultFile = null;
		//System.IO.File.CreateText(
		//	System.IO.Path.Join(Resource.AppDirectory, "Log.txt")
		//);

		//...........................................................

		public event PropertyChangedEventHandler         PropertyChanged;
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		//...........................................................

		public Log( string NAME = null )
		{
			Name = NAME;
		}

		//...........................................................

		protected readonly List<LogItem> m_list = new();

		public string Name { get; set; }  // default name for save file

		//...........................................................

		public LogItem this[int INDEX]                 => m_list[INDEX];
		public int                     Count           => m_list.Count;
		public IEnumerator<LogItem>    GetEnumerator() => m_list.GetEnumerator();
		       IEnumerator IEnumerable.GetEnumerator() => m_list.GetEnumerator();

		//...........................................................

		public void Clear()
		{
			lock( this ) {
				if( Count < 1 ) return;  // don't call CollectionChanged
				m_list.Clear();
			}
			PropertyChanged?.DispatcherInvoke(this, new PropertyChangedEventArgs("Count"));
			CollectionChanged?.DispatcherInvoke(this, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Reset
			));
		}

		//...........................................................

		public void Add( LogItem ITEM )
		{
			if( ITEM == null ) return;
			int index;
			lock( this ) {
				index = m_list.Count;
				m_list.Add(ITEM);
				if( DefaultFile != null ) lock( DefaultFile ) {
					DefaultFile.WriteLine(ITEM.Text);
					if( ITEM.Type == LogItemType.Heading
					||  ITEM.Type == LogItemType.Failure
					)	DefaultFile.Flush();
				}
			}
			PropertyChanged?.DispatcherBeginInvoke(this, new PropertyChangedEventArgs("Count"));
			CollectionChanged?.DispatcherBeginInvoke(this, new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Add, ITEM, index
			));
			System.Threading.Thread.Yield();
		}

		//...........................................................

		public void Add( Log LOG )
		{
			if( !ReferenceEquals(this, LOG) ) lock( LOG ) {
				foreach( var item in LOG ) {
					Add(item);
				}
			}
		}

		//...........................................................

		public bool Save()
		{
			var name   = Name ?? "cmkNMS";
			var dialog = new SaveFileDialog {
				InitialDirectory = System.IO.Path.GetTempPath(),
				FileName = name + ".log",
				Filter   = "Log|*.log",
			};
			if( dialog.ShowDialog(System.Windows.Application.Current.MainWindow) != true ) return false;
			lock( this ) try {
				using( var file = System.IO.File.CreateText(dialog.FileName) ) {
					foreach( var item in m_list ) {
						file.WriteLine(item.Text);
					}
				}
				return true;
			}
			catch( Exception EX ) {
				Log.Default.AddFailure(EX);
				return false;
			}
		}
	}

	//=========================================================================

	public static partial class _x_
	{
		public static void Add( this Log LOG, LogItemType TYPE, string TEXT, object TAG0, object TAG1 )
		{
			LOG?.Add(new LogItem{
				Type = TYPE,
				Text = TEXT.IsNullOrEmpty() ? null : $"[{DateTime.Now.ToTimeStamp()}] {TEXT}",
				Tag0 = TAG0,
				Tag1 = TAG1,
			});
		}

		//...........................................................

		public static void AddHeading( this Log LOG, string TEXT, object TAG0 = null, object TAG1 = null )
		{
			Add(LOG, LogItemType.Heading, TEXT, TAG0, TAG1);
		}

		public static void AddFailure( this Log LOG, string TEXT, object TAG0 = null, object TAG1 = null )
		{
			Add(LOG, LogItemType.Failure, TEXT, TAG0, TAG1);
		}

		public static void AddWarning( this Log LOG, string TEXT, object TAG0 = null, object TAG1 = null )
		{
			Add(LOG, LogItemType.Warning, TEXT, TAG0, TAG1);
		}

		public static void AddInformation( this Log LOG, string TEXT, object TAG0 = null, object TAG1 = null )
		{
			Add(LOG, LogItemType.Information, TEXT, TAG0, TAG1);
		}

		public static void AddSuccess( this Log LOG, string TEXT, object TAG0 = null, object TAG1 = null )
		{
			Add(LOG, LogItemType.Success, TEXT, TAG0, TAG1);
		}

		//...........................................................

		public static void AddFailure( this Log LOG, Exception EX, string PREFIX = null, string SUFFIX = null, object TAG0 = null, object TAG1 = null )
		{
			if( EX is OperationCanceledException ) {
				LOG.AddWarning($"{PREFIX}{EX.Message}{SUFFIX}");
			}
			else if( EX is AggregateException ax ) {
				foreach( var ex in ax.InnerExceptions ) {
					AddFailure(LOG, ex, PREFIX, SUFFIX, null);
				}
			}
			//else if( EX.InnerException != null ) {
			//	AddFailure(LOG, EX.InnerException, PREFIX, SUFFIX, null);
			//}
			else {
				LOG.AddFailure($"{PREFIX}{EX?.ToString()}{SUFFIX}", TAG0, TAG1);
			}
		}
	}
}

//=============================================================================
