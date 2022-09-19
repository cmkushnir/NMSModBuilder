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
using System.Threading;

//=============================================================================

namespace cmk
{
    /// <summary>
    /// Notes:
    /// - ReleaseWrite thread must be same as AcquireWrite thread
    /// - # of ReleaseRead must equal # of AcquireRead
    /// - Supports re-entrant Acquires for both Readers and Writer
    /// - Reader not upgradable, Writer not downgradable,
    ///   but Writer can AquireReead.
    /// - Not fair, does not favor Readers or Writers
    /// 
    /// .NET ReadWriteLock|ReadWriteLockSlim have thread affinity (threads track locks acquired).
    /// This means that when a thread acquires a read|write lock the same thread must release.
    /// In general this would be desired, but in a AquireRead - yield return or async op - ReleaseRead
    /// situation the ReleaseRead thread may not be the same as the AquireRead thread.
    /// </summary>
    public class ReadWriteLock
	{
		int   m_thd_id    = 0;  // thread id of curernt writer.
		short m_recursion = 0;  // recurrsion for current writer.
		short m_readers   = 0;  // # of current readers, includes

		//...........................................................

		public ReadWriteLock()
		{
		}

		//...........................................................

		~ReadWriteLock()
		{
			if( m_thd_id != 0 ) {
				throw new Exception("~ReadWriteLock m_thd_id != 0");
			}
			if( m_recursion != 0 ) {
				throw new Exception("~ReadWriteLock m_recursion != 0");
			}
			if( m_readers != 0 ) {
				throw new Exception("~ReadWriteLock m_readers != 0");
			}
		}

		//...........................................................

		// try to acquire write lock for writing
		bool try_acquire_write()  // called by AcquireWrite
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			var old = Interlocked.CompareExchange(ref m_thd_id, tid, 0);

			if( old == 0 ) {            // write acquired
				if( m_readers <= 0 ) {  // can keep write if no readers
					if( m_recursion != 0 ) {
						throw new Exception("try_acquire_write m_recursion != 0");
					}
					++m_recursion;
					return true;
				}
				else {  // existing readers, can't keep
					m_thd_id = 0;
					//::WakeByAddressAll((DWORD*)&m_thd_id);
				}
			}
			else if( old == tid ) {  // re-entrant write acquired
				++m_recursion;
				return true;
			}

			return false;
		}

		//...........................................................

		// try to acquire write lock for reading
		bool try_acquire_write_read()  // called by aquire_write_read_wait
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			var old = Interlocked.CompareExchange(ref m_thd_id, tid, 0);

			if( old == 0 || old == tid ) {  // write acquired or re-entrant write acquired
				++m_recursion;
				return true;
			}

			return false;
		}

		//...........................................................

		// wait to acquire write lock for reading
		bool acquire_write_read_wait( TimeSpan TIMEOUT )  // called by AcquireRead
		{
			var end = Environment.TickCount64 + TIMEOUT.Ticks;
			if( TIMEOUT == default ) end = TimeSpan.MaxValue.Ticks;

			while( !try_acquire_write_read() ) {
				var now = Environment.TickCount64;
				if( now >= end ) return false;

				var thd_id = m_thd_id;
				if( thd_id == 0 ) thd_id = -1;

				//::WaitOnAddress(&m_thd_id, &thd_id, sizeof(m_thd_id), end - now);
				Thread.Sleep(8);
			}

			return true;
		}

		//...........................................................

		public bool AcquireWrite( TimeSpan TIMEOUT = default )
		{
			var end = Environment.TickCount64 + TIMEOUT.Ticks;
			if( TIMEOUT == default ) end = TimeSpan.MaxValue.Ticks;

			while( !try_acquire_write() ) {
				var now = Environment.TickCount64;
				if( now >= end ) return false;

				var thd_id = m_thd_id;
				if( thd_id == 0 ) thd_id = -1;

				//::WaitOnAddress(&m_thd_id, &thd_id, sizeof(m_thd_id), end - now);
				Thread.Sleep(8);
			}

			return true;
		}

		//...........................................................

		public bool ReleaseWrite()
		{
			// if current thread calling ReleaseWrite then it should have 
			// previously called AcquireWrite.  Testing for !m_thd_id handles
			// case where current thread AcquireWrite failed but it is still
			// blindly calling ReleaseWrite.
			if( m_thd_id == 0 ) return false;

			// ReleaseWrite thread must be same as AcquireWrite thread.
			if( Thread.CurrentThread.ManagedThreadId != m_thd_id ) {
				throw new Exception("ReleaseWrite Thread.CurrentThread.ManagedThreadId != m_thd_id");
			}
			if( m_recursion <= 0 ) {
				throw new Exception("ReleaseWrite m_recursion <= 0");
			}

			if( --m_recursion == 0 ) {
				m_thd_id = 0;
				//::WakeByAddressAll((chara*)&m_thd_id);
			}

			return true;
		}

		//...........................................................

		public bool AcquireRead( TimeSpan TIMEOUT = default )
		{
			if( !acquire_write_read_wait(TIMEOUT) ) return false;
			++m_readers;
			return ReleaseWrite();
		}

		//...........................................................

		public bool ReleaseRead( TimeSpan TIMEOUT = default )
		{
			if( !acquire_write_read_wait(TIMEOUT) ) return false;
			--m_readers;
			return ReleaseWrite();
		}

		//...........................................................

		public void InvokeRead( Action ACTION, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireRead(TIMEOUT);
			try {
				ACTION.Invoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
			}
			finally {
				if( !ReleaseRead(TIMEOUT) ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeRead failed to ReleaseRead - report"
					);
				}
			}
		}

		public RETURN_T InvokeRead<RETURN_T>( Func<RETURN_T> FUNC, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireRead(TIMEOUT);
			try {
				return FUNC.Invoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return default;
			}
			finally {
				if( !ReleaseRead(TIMEOUT) ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeRead<> failed to ReleaseRead - report"
					);
				}
			}
		}

		//...........................................................

		public void InvokeDispatcherRead( Action ACTION, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireRead(TIMEOUT);
			try {
				ACTION.DispatcherInvoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
			}
			finally {
				if( !ReleaseRead(TIMEOUT) ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeRead failed to ReleaseRead - report"
					);
				}
			}
		}

		public RETURN_T InvokeDispatcherRead<RETURN_T>( Func<RETURN_T> FUNC, Log LOG = null, TimeSpan TIMEOUT = default )
		where  RETURN_T : class
		{
			AcquireRead(TIMEOUT);
			try {
				return FUNC.DispatcherInvoke() as RETURN_T;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return default;
			}
			finally {
				if( !ReleaseRead(TIMEOUT) ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeRead failed to ReleaseRead - report"
					);
				}
			}
		}

		//...........................................................

		public void InvokeWrite( Action ACTION, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireWrite(TIMEOUT);
			try {
				ACTION.Invoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
			}
			finally {
				if( !ReleaseWrite() ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeWrite failed to ReleaseWrite - report"
					);
				}
			}
		}

		public RETURN_T InvokeWrite<RETURN_T>( Func<RETURN_T> FUNC, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireWrite(TIMEOUT);
			try {
				return FUNC.Invoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return default;
			}
			finally {
				if( !ReleaseWrite() ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeWrite<> failed to ReleaseWrite - report"
					);
				}
			}
		}

		//...........................................................

		public void InvokeDispatcherWrite( Action ACTION, Log LOG = null, TimeSpan TIMEOUT = default )
		{
			AcquireWrite(TIMEOUT);
			try {
				ACTION.DispatcherInvoke();
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
			}
			finally {
				if( !ReleaseWrite() ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeDispatcherWrite failed to ReleaseWrite - report"
					);
				}
			}
		}

		public RETURN_T InvokeDispatcherWrite<RETURN_T>( Func<RETURN_T> FUNC, Log LOG = null, TimeSpan TIMEOUT = default )
		where  RETURN_T : class
		{
			AcquireWrite(TIMEOUT);
			try {
				return FUNC.DispatcherInvoke() as RETURN_T;
			}
			catch( Exception EX ) {
				LOG.AddFailure(EX);
				return default;
			}
			finally {
				if( !ReleaseWrite() ) {
					Log.Default.AddFailure(
						"ReadWriteLock.InvokeDispatcherWrite<> failed to ReleaseWrite - report"
					);
				}
			}
		}
	}
}

//=============================================================================
