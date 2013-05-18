using System;
using System.Collections.Concurrent;

namespace ShareDeployed.Proxy.Pooling
{
	public sealed class Pool<T> where T : IDisposable
	{
		private ConcurrentBag<T> pool = new ConcurrentBag<T>();
		private Func<T> objectFactory;

		public Pool(Func<T> factory)
		{
			objectFactory = factory;
		}

		public T GetInstance()
		{
			T result;
			if (!pool.TryTake(out result))
			{
				result = objectFactory();
			}
			return result;
		}

		public void ReturnToPool(T instance)
		{
			pool.Add(instance);
		}
	}

	public class PoolableObjectBase<T> : IDisposable where T : IDisposable, new()
	{
		private static Pool<T> pool = new Pool<T>(() => new T());

		private T _data;

		public PoolableObjectBase()
			: this(new T())
		{
		}

		public PoolableObjectBase(T data)
		{
			_data = data;
		}

		public void Dispose()
		{
			pool.ReturnToPool(_data);
		}

		~PoolableObjectBase()
		{
			GC.ReRegisterForFinalize(this);
			pool.ReturnToPool(_data);
		}
	}

}