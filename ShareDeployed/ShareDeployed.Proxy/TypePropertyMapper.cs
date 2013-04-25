using ShareDeployed.Common.Proxy.Caching;
using ShareDeployed.Common.Proxy.FastReflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	public sealed class TypePropertyMapper
	{
		private static Lazy<TypePropertyMapper> _lazyInit;
		private static Cache<Type, ConcurrentDictionary<string, FastProperty>> _cache;

		static TypePropertyMapper()
		{
			_lazyInit = new Lazy<TypePropertyMapper>(() => new TypePropertyMapper(), true);
			_cache = new Cache<Type, ConcurrentDictionary<string, FastProperty>>();
		}

		private TypePropertyMapper()
		{
		}

		public static TypePropertyMapper Instance
		{
			get
			{
				return _lazyInit.Value;
			}
		}

		public void Add(Type type, PropertyInfo pi)
		{
			if (!_cache.Contains(type))
			{
				_cache.Insert(type, new ConcurrentDictionary<string, FastProperty>(), CacheStrategy.Permanent);
				FastProperty pFast = new FastProperty(pi);
				_cache[type].TryAdd(pi.Name, pFast);
			}
			else if (!_cache[type].ContainsKey(pi.Name))
			{
				FastProperty pFast = new FastProperty(pi);
				_cache[type].TryAdd(pi.Name, pFast);
			}
		}

		public void Add(Type type, PropertyInfo pi, out FastProperty fProp)
		{
			fProp = null;
			if (!_cache.Contains(type))
			{
				_cache.Insert(type, new ConcurrentDictionary<string, FastProperty>(), CacheStrategy.Permanent);
				FastProperty pFast = new FastProperty(pi);
				fProp = pFast;
				_cache[type].TryAdd(pi.Name, pFast);
			}
			else if (!_cache[type].ContainsKey(pi.Name))
			{
				FastProperty pFast = new FastProperty(pi);
				fProp = pFast;
				_cache[type].TryAdd(pi.Name, pFast);
			}
		}

		public FastProperty Get(Type type, string name)
		{
			if (_cache.Contains(type))
			{
				FastProperty pFast = null;
				_cache[type].TryGetValue(name, out pFast);
				return pFast;
			}
			return null;
		}

	}
}
