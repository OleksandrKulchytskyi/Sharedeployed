using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ShareDeployed.Common.Proxy
{
	public sealed class SafeCollection<T> : ICollection<T>
	{
		private readonly ConcurrentDictionary<T, bool> _inner;
		private int counter;

		public SafeCollection()
		{
			_inner = new ConcurrentDictionary<T, bool>();
			counter = 0;
		}

		public SafeCollection(int capacity)
		{
			_inner = new ConcurrentDictionary<T, bool>(4, capacity);
			counter = 0;
		}

		public void Add(T item)
		{
			if (_inner.TryAdd(item, true))
				Interlocked.Increment(ref counter);
		}

		public void AddRange(IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				if (_inner.TryAdd(item, true))
					Interlocked.Increment(ref counter);
			}
		}

		public void Clear()
		{
			_inner.Clear();
			Interlocked.Exchange(ref counter, 0);
		}

		public bool Contains(T item)
		{
			return _inner.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_inner.Keys.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				return Interlocked.CompareExchange(ref counter, 0, 0);
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			bool value;
			if (_inner.TryRemove(item, out value))
			{
				Interlocked.Decrement(ref counter);
				return true;
			}
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _inner.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
