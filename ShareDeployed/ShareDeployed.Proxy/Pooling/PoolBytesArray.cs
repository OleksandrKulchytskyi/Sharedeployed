using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ShareDeployed.Proxy.Pooling
{
	public sealed class PoolBytesArray
	{
		internal const int MaxChunkSize = 0x1f40;
		private readonly int _capacity;
		private readonly int _chunkLen;
		private ConcurrentQueue<ByteArray> _internal;
		private int _inUseCount;

		public PoolBytesArray()
			: this(2, 85000)
		{
		}

		public PoolBytesArray(int capacity)
			: this(capacity, 85000)
		{
		}

		public PoolBytesArray(int initCapacity, int chunkLen)
		{
			_capacity = initCapacity;
			_chunkLen = chunkLen;
			_internal = new ConcurrentQueue<ByteArray>();

			for (int i = 0; i < _capacity; i++)
			{
				_internal.Enqueue(CreateInstance());
			}
		}

		// <summary>
		/// Gets the overall number of elements managed by this pool.
		/// </summary>
		public int Count
		{
			get { return _inUseCount + this._internal.Count; }
		}

		/// <summary>
		/// Gets the number of available elements currently contained in the pool.
		/// </summary>
		public int AvailableCount
		{
			get { return this._internal.Count; }
		}

		/// <summary>
		/// Gets the number of elements currently in use and not available in this pool.
		/// </summary>
		public int ItemsInUse
		{
			get { return _inUseCount; }
		}

		/// <summary>
		/// Gets a value that indicates whether the available pool is empty.
		/// </summary>
		public bool IsEmpty
		{
			get { return _internal.IsEmpty; }
		}

		public IEnumerable<ByteArray> Acquire(long overallLength)
		{
			if (overallLength <= 0)
				throw new ArgumentOutOfRangeException("Index must be greater than zero.");

			int partsCount = (int)Math.Floor((double)((overallLength / _chunkLen) + 0.5));
			while (AvailableCount < partsCount)
			{
				_internal.Enqueue(CreateInstance());
			}

			while (partsCount != 0)
			{
				ByteArray array;
				if (!_internal.TryDequeue(out array))
					throw new InvalidOperationException("Fail to perform deque operation.");

				partsCount--;
				Interlocked.Increment(ref _inUseCount);
				array.Lock();
				yield return array;
			}
		}

		public void Release(ByteArray chunk)
		{
			chunk.Unlock();
			_internal.Enqueue(chunk);
			Interlocked.Decrement(ref _inUseCount);
		}

		public void Release(IEnumerable<ByteArray> chunks)
		{
			foreach (ByteArray array in chunks)
			{
				Release(array);
			}
		}

		/// <summary>
		/// Allocates a new instance of BytesArray.
		/// </summary>
		/// <returns>Allocated instance of BytesArray.</returns>
		private ByteArray CreateInstance()
		{
			return new ByteArray(_chunkLen);
		}
	}

	public struct ByteArray
	{
		private byte[] _array;
		private int _arrayAddr;
		private int _capacity;
		private int _len;
		private int _isLocked;
		private GCHandle _gcHandle;

		public ByteArray(int capacity)
		{
			_capacity = capacity;
			_arrayAddr = -1;
			_len = 0;
			_isLocked = -1;
			_gcHandle = default(GCHandle);
			_array = new byte[capacity];
		}

		public bool IsLocked
		{
			get { return (Interlocked.CompareExchange(ref _isLocked, 1, 1) == 1); }
		}

		public int GetLen
		{
			get { return Interlocked.CompareExchange(ref _len, 0, 0); }
		}

		public byte[] GetBytesArray() { return _array; }

		public int GetBytesAddress() { return _arrayAddr; }

		public void ResetArray() { _array.Initialize(); }

		public bool Lock()
		{
			if (Interlocked.CompareExchange(ref _isLocked, 1, -1) == 1)
				return false;

			if (_gcHandle.IsAllocated)
				return false;

			try
			{
				_gcHandle = GCHandle.Alloc(_array, GCHandleType.Pinned);
			}
			catch (Exception)
			{
				Interlocked.Exchange(ref _isLocked, -1);
				return false;
			}

			try
			{
				_arrayAddr = _gcHandle.AddrOfPinnedObject().ToInt32();
			}
			catch (Exception)
			{
				if (_gcHandle.IsAllocated) _gcHandle.Free();
				Interlocked.Exchange(ref _isLocked, -1);
				return false;
			}

			ResetArray();
			return true;
		}

		public void Unlock()
		{
			if (Interlocked.CompareExchange(ref _isLocked, -1, 1) != 1)
				return;

			if (_gcHandle.IsAllocated)
				_gcHandle.Free();
			_arrayAddr = -1;
		}

		public void AssignWithValues(byte[] chunk)
		{
			chunk.ThrowIfNull("chunk", "Parameter cannot be null.");
			if (chunk.Length > _capacity)
				throw new IndexOutOfRangeException(string.Format("Chunk length must be less or equals internal array capacity, {0}: {1}", chunk.Length, _capacity));
		}
	}
}
