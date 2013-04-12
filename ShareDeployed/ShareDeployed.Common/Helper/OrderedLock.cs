using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Helper
{

	/// <summary>
	/// <example>
	///Example of usage :
	///public class someResource
	///{
	///    private OrderedLock lock1 = new OrderedLock(1);
	///    private OrderedLock lock2 = new OrderedLock(2);
	///
	///    public void lockInOrder()
	///    {
	///        lock1.AcquireWriteLock();
	///        lock2.AcquireWriteLock();
	///        // do something
	///        lock1.ReleaseWriteLock();
	///        lock2.ReleaseWriteLock();
	///    }
	///    
	///    public void lockOutOfOrder()
	///    {
	///        lock2.AcquireReadLock();
	///        lock1.AcquireReadLock(); // throws exception
	///        // read something
	///        lock2.ReleaseReadLock();
	///        lock1.ReleaseReadLock();
	///    }
	///}
	/// </example>
	/// </summary>
	public sealed class OrderedLock : IDisposable
	{
		private static readonly ConcurrentDictionary<int, object> _locks =
			new ConcurrentDictionary<int, object>();

		[ThreadStatic]
		private static ISet<int> _acquiredLocks;

		private readonly ThreadLocal<int> _refCounts = new ThreadLocal<int>();
		private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		private readonly int _id;

		public OrderedLock(int id)
		{
			if (!_locks.TryAdd(id, null))
				throw new InvalidOperationException("Duplicate identifier detected.");

			_id = id;
			_refCounts.Value = 0;
		}

		public void AcquireReadLock()
		{
			this.CheckLockOrder();
			this._locker.EnterReadLock();
		}

		public void AcquireWriteLock()
		{
			this.CheckLockOrder();
			this._locker.ExitWriteLock();
		}

		public void ReleaseReadLock()
		{
			this._refCounts.Value--;
			this._locker.ExitReadLock();
			if (_refCounts.Value == 0)
			{
				object val;
				if (!_locks.TryRemove(this._id, out val))
					throw new InvalidOperationException("Fail to delete locker.");
			}
		}

		public void ReleaseWriteLock()
		{
			this._refCounts.Value--;
			this._locker.ExitWriteLock();
			if (_refCounts.Value == 0)
			{
				object val;
				if (!_locks.TryRemove(this._id, out val))
					throw new InvalidOperationException("Fail to delete locker.");
			}
		}

		public void Dispose()
		{
			while (_locker.IsWriteLockHeld)
			{
				this.ReleaseWriteLock();
			}

			while (_locker.IsReadLockHeld)
			{
				this.ReleaseReadLock();
			}

			_locker.Dispose();
			_refCounts.Dispose();
			GC.SuppressFinalize(this);
		}

		private void CheckLockOrder()
		{
			if (_acquiredLocks == null)
				_acquiredLocks = new HashSet<int>();

			if (!_acquiredLocks.Contains(_id))
			{
				if (_acquiredLocks.Any() && _acquiredLocks.Max() > _id)
					throw new InvalidOperationException("Invalid order of locking detected.");

				_acquiredLocks.Add(_id);
			}
			_refCounts.Value++;
		}

	}
}
