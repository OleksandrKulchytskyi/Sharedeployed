using System;
using System.Collections.Concurrent;

namespace ShareDeployed.Common.Proxy
{
	public sealed class TypeAttributesMapper
	{
		private static ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>> _interceptorsMappings;
		private static Lazy<TypeAttributesMapper> _instance;

		#region ctors
		static TypeAttributesMapper()
		{
			_instance = new Lazy<TypeAttributesMapper>(() => new TypeAttributesMapper(), true);
			_interceptorsMappings = new ConcurrentDictionary<Type, SafeCollection<InterceptorInfo>>();
		}

		private TypeAttributesMapper()
		{
		} 
		#endregion

		public static TypeAttributesMapper Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		#region public methods
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
		#endregion
	}
}
