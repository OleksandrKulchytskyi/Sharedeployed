using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Proxy
{
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
	public sealed class InterceptorAttribute : Attribute
	{
		public Type InterceptorType { get; set; }
		public ExecutionInjectionMode Mode { get; set; }
	}

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
			_interceptorsMappings = new ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>>();
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

				bool added = _interceptorsMappings.TryAdd(type, new SafeCollection<InterceptorInfo>());
				if (added)
				{
					_interceptorsMappings[type].Add(info);
					return added;
				}
				else
					return false;
			}
			return false;
		}

		public bool EmptyAndAddRange(Type type, SafeCollection<InterceptorInfo> interceptors)
		{
			if (!_interceptorsMappings.ContainsKey(type))
			{
				bool added = _interceptorsMappings.TryAdd(type, new SafeCollection<InterceptorInfo>());
				if (added)
				{
					_interceptorsMappings[type].AddRange(interceptors);
					return added;
				}
				else
					return false;
			}
			else
			{
				_interceptorsMappings[type].Clear();
				_interceptorsMappings[type].AddRange(interceptors);
				return true;
			}
			return false;
		}

		public SafeCollection<InterceptorInfo> GetInterceptions(Type type)
		{
			SafeCollection<InterceptorInfo> infos = default(SafeCollection<InterceptorInfo>); ;
			if (_interceptorsMappings.ContainsKey(type))
			{
				_interceptorsMappings.TryGetValue(type, out infos);
				return infos;
			}

			return infos;
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
				SafeCollection<InterceptorInfo> infos;
				return _interceptorsMappings.TryRemove(type, out infos);
			}
		}
	}

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
