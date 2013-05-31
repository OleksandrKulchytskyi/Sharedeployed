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
		private readonly long _chunkLen;
		private ConcurrentQueue<ByteArray> _buckets;
		private const int sizeExcludingPromotionToGen2 = 84980;

		private int _inUseCount;
		private int _avaliable;

		public PoolBytesArray()
			: this(2, sizeExcludingPromotionToGen2)
		{
		}

		public PoolBytesArray(int capacity)
			: this(capacity, sizeExcludingPromotionToGen2)
		{
		}

		public PoolBytesArray(int initCapacity, long chunkLen)
		{
			_capacity = initCapacity;
			_chunkLen = chunkLen;
			_buckets = new ConcurrentQueue<ByteArray>();

			for (int i = 0; i < _capacity; i++)
			{
				_buckets.Enqueue(CreateInstance());
			}
			_avaliable = _capacity;
		}

		#region Properties
		/// <summary>
		/// Return the value which indicates the capacity of byte array
		/// </summary>
		public long ChunkLength
		{
			get { return _chunkLen; }
		}

		// <summary>
		/// Gets the overall number of elements managed by this pool.
		/// </summary>
		public int Count
		{
			get { return _inUseCount + this._avaliable; }
		}

		/// <summary>
		/// Gets the number of available elements currently contained in the pool.
		/// </summary>
		public int AvailableCount
		{
			get { return this._avaliable; }
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
			get { return _buckets.IsEmpty; }
		}
		#endregion

		public List<ByteArray> Acquire(long overallLength)
		{
			if (overallLength <= 0)
				throw new ArgumentOutOfRangeException("Index must be greater than zero.");

			int partsCount = (int)Math.Ceiling((Convert.ToDouble(overallLength) / Convert.ToDouble(_chunkLen)));
			while (AvailableCount < partsCount)
			{
				_buckets.Enqueue(CreateInstance());
				Interlocked.Increment(ref _avaliable);
			}

			List<ByteArray> buckets = new List<ByteArray>(partsCount);
			while (partsCount != 0)
			{
				ByteArray array;
				if (!_buckets.TryDequeue(out array))
					throw new InvalidOperationException("Fail to perform deque operation.");

				partsCount--;
				Interlocked.Increment(ref _inUseCount);
				Interlocked.Decrement(ref _avaliable);

				//array.Lock();// see drawbacks of pinned objects
				buckets.Add(array);
			}
			return buckets;
		}

		public void Release(ByteArray chunk)
		{
			if (chunk.IsLocked)
				chunk.Unlock();
			_buckets.Enqueue(chunk);
			Interlocked.Decrement(ref _inUseCount);
			Interlocked.Increment(ref _avaliable);
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
			return new ByteArray(Convert.ToInt32(_chunkLen));
		}
	}

	public sealed class ByteArray
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

		#region Properties
		public bool IsLocked
		{
			get { return (_isLocked == 1); }
		}

		public int RealLength
		{
			get
			{
				return _len;
			}
		}

		public int Capacity
		{
			get
			{
				return _capacity;
			}
		}
		#endregion

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
				//NOTE: Pin objects for the shortest possible time. Pinning is cheap if no garbage collection occurs
				//while the object is pinned. If calling unmanaged code that requires a pinned object for an indefinite amount of time (such as an asynchronous call), 
				//consider copying or unmanaged memory allocation instead of pinning a managed object. 
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

			if (_gcHandle.IsAllocated) _gcHandle.Free();
			_arrayAddr = -1;
		}

		public void AssignWithValues(byte[] chunk)
		{
			chunk.ThrowIfNull("chunk", "Parameter cannot be a null.");
			if (chunk.Length > _capacity)
				throw new IndexOutOfRangeException(string.Format("Chunk length must be less or equals internal array capacity, {0}: {1}", chunk.Length, _capacity));

			Array.Copy(chunk, _array, chunk.Length);
		}

		public void AssignRealLength(int length)
		{
			if (length > _capacity)
				throw new IndexOutOfRangeException(string.Format("Length parameter must be less or equals internal array capacity, {0}: {1}", length, _capacity));

			Interlocked.Exchange(ref _len, length);
		}

		#region overrides
		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
				return true;
			if (obj == null) return false;

			return (obj is ByteArray) ? Equals((ByteArray)obj) : false;
		}

		private bool Equals(ByteArray arrary)
		{
			return this._arrayAddr == arrary._arrayAddr &&
				this._capacity == arrary._capacity &&
				this._len == arrary._len &&
				this._isLocked == arrary._isLocked;
		}

		private int _hash = -1;
		public override int GetHashCode()
		{
			if (_hash == -1)
			{
				_hash = 17;
				_hash = _hash * 31 + _len.GetHashCode();
				_hash = _hash * 31 + _capacity.GetHashCode();
				_hash = _hash * 31 + _isLocked.GetHashCode();
				_hash = _hash * 31 + _arrayAddr.GetHashCode();
			}

			return _hash;
		}
		#endregion
	}
}
