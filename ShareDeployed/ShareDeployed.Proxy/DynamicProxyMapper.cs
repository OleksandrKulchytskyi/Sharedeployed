using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Proxy
{
	public class InterceptorInfo
	{
		public InterceptorInfo(Type interceptorType, ExecutionInjectionMode mode)
		{
			Interceptor = interceptorType;
			Mode = mode;
		}

		public Type Interceptor { get; private set; }
		public ExecutionInjectionMode Mode { get; private set; }
	}

	public sealed class DynamicProxyMapper
	{
		private static ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>> _interceptorsMappings;

		static Lazy<DynamicProxyMapper> _instance;

		private DynamicProxyMapper()
		{
			_instance = new Lazy<DynamicProxyMapper>(() => new DynamicProxyMapper(), true);
			_interceptorsMappings = new ConcurrentDictionary<Type, InterceptionsInfo>();
		}

		public static DynamicProxyMapper Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		public bool Add(Type type, InterceptorInfo info)
		{
			if (!_interceptorsMappings.ContainsKey(type))
			{
				return _interceptorsMappings.TryAdd(type, info);
			}
			return false;
		}

		public InterceptionsInfo GetInterceptions(Type type)
		{
			InterceptionsInfo info = default(InterceptionsInfo); ;
			if (_interceptorsMappings.ContainsKey(type))
				if (_interceptorsMappings.TryGetValue(type, out info))
				{
					return info;
				}

			return info;
		}

		public bool Contains(Type t)
		{
			return _interceptorsMappings.ContainsKey(t);
		}

		public bool Remove(Type type)
		{
			if (!_interceptorsMappings.ContainsKey(type))
				return false;
			else
			{
				InterceptionsInfo info;
				return _interceptorsMappings.TryRemove(type, out info);
			}
		}
	}

	public sealed class SafeCollection<T> : ICollection<T>
	{
		private readonly ConcurrentDictionary<T, bool> _inner;
		int counter;

		public SafeCollection()
		{
			_inner = new ConcurrentDictionary<T, bool>();
			counter = 0;
		}

		public void Add(T item)
		{
			if (_inner.TryAdd(item, true))
				Interlocked.Increment(ref counter);
		}

		public void Clear()
		{
			_inner.Clear(); Interlocked.Exchange(ref counter, 0);
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
			if( _inner.TryRemove(item, out value))
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
