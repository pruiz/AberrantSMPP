using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Common.Logging;

namespace AberrantSMPP
{
	internal abstract class BaseLock : IDisposable
	{
		protected ReaderWriterLockSlim @Lock;

		public BaseLock(ReaderWriterLockSlim @lock)
		{
			Lock = @lock;
		}

		public abstract void Dispose();
	}


	internal class ReadLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ReadLock(ReaderWriterLockSlim @lock)
			: base(@lock)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Lock.EnterUpgradeableReadLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Lock.ExitUpgradeableReadLock();
		}
	}


	internal class ReadOnlyLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ReadOnlyLock(ReaderWriterLockSlim @lock)
			: base(@lock)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Lock.EnterReadLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Lock.ExitReadLock();
		}
	}


	internal class WriteLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public WriteLock(ReaderWriterLockSlim @lock)
			: base(@lock)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Lock.EnterWriteLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Lock.ExitWriteLock();
		}
	}

	// TODO: Implement overloads accepting Cancellator..
	
	public static class ReadWriterSlimLockExtensions
	{
		public static IDisposable ForRead(this @ReaderWriterLockSlim @this)
		{
			return new ReadLock(@this);
		}
		
		public static IDisposable ForReadOnly(this @ReaderWriterLockSlim @this)
		{
			return new ReadOnlyLock(@this);
		}
		
		public static IDisposable ForWrite(this @ReaderWriterLockSlim @this)
		{
			return new WriteLock(@this);
		}
	}
}
