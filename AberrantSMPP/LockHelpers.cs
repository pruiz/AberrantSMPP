using System;
using System.Threading;

namespace AberrantSMPP
{
	internal abstract class BaseLock : IDisposable
	{
		protected ReaderWriterLockSlim Locks;


		public BaseLock(ReaderWriterLockSlim locks)
		{
			Locks = locks;
		}


		public abstract void Dispose();
	}


	internal class ReadLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ReadLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Locks.EnterUpgradeableReadLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Locks.ExitUpgradeableReadLock();
		}
	}


	internal class ReadOnlyLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ReadOnlyLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Locks.EnterReadLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Locks.ExitReadLock();
		}
	}


	internal class WriteLock : BaseLock
	{
		private static readonly global::Common.Logging.ILog Log = global::Common.Logging.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public WriteLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Entering.. ({0})", _Locks.GetHashCode());
			Locks.EnterWriteLock();
		}


		public override void Dispose()
		{
			//if (_Log.IsDebugEnabled) _Log.DebugFormat("Exiting.. ({0})", _Locks.GetHashCode());
			Locks.ExitWriteLock();
		}
	}
}
