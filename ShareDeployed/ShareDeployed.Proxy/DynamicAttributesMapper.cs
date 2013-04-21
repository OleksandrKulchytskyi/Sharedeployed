using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Proxy
{
	public sealed class DynamicAttributesMapper
	{
		private static ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>> _interceptorsMappings;
		private static Lazy<DynamicAttributesMapper> _instance;

		static DynamicAttributesMapper()
		{
			_instance = new Lazy<DynamicAttributesMapper>(() => new DynamicAttributesMapper(), true);
			_interceptorsMappings = new ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>>();
		}

		private DynamicAttributesMapper()
		{
		}

		public static DynamicAttributesMapper Instance
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
}
