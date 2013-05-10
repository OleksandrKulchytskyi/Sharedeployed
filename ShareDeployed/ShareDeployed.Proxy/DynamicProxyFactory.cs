using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ShareDeployed.Proxy
{
	public sealed class DynamicProxyFactory
	{
		private DynamicProxyFactory()
		{
		}

		public static dynamic CreateDynamicProxy(Type type)
		{
			return CreateDynamicProxy(type, false);
		}

		public static dynamic CreateDynamicProxy(Type type, bool isWeak)
		{
			if (ServicesMapper.CanBeResolved(type))
			{
				return CreateDynamicProxy(DynamicProxyPipeline.Instance.ContracResolver.Resolve(type), isWeak);
			}
			else
			{
				type.BindToSelfWithAlias(type.FullName);
				return CreateDynamicProxy(DynamicProxyPipeline.Instance.ContracResolver.Resolve(type), isWeak);
			}
		}

		public static dynamic CreateDynamicProxy(object target)
		{
			return CreateDynamicProxy(target, false);
		}

		public static dynamic CreateDynamicProxy(object target, bool makeTargetWeak)
		{
			CreateInstanceDelegate del = ObjectCreatorHelper.ObjectInstantiater(typeof(DynamicProxy));
			dynamic proxy = del();
			proxy.SetTargetObject(target, makeTargetWeak);
			return proxy;
		}
	}

	public interface IDynamicProxyManager
	{
		int Count { get; }

		bool Contains(string proxyId);
		void Clear();

		void Add(string proxyId, dynamic value);
		dynamic Get(string proxyId);
		void Remove(string proxyId);
	}

	public class DynamicProxyManager : IDynamicProxyManager, IConfigurable
	{
		private ConcurrentDictionary<string, dynamic> _proxies;
		private int _count;

		public DynamicProxyManager()
		{
			_count = 0;
			_proxies = new ConcurrentDictionary<string, dynamic>();
		}

		#region IDynamicProxyManager members
		public int Count
		{
			get { return Interlocked.CompareExchange(ref _count, 0, 0); }
		}

		public bool Contains(string proxyId)
		{
			proxyId.ThrowIfNull("proxyId", "Parameter cannot be null.");
			return _proxies.ContainsKey(proxyId);
		}

		public void Clear()
		{
			if (Interlocked.CompareExchange(ref _count, 0, 0) > 0)
				_proxies.Clear();
		}

		public void Add(string proxyId, dynamic value)
		{
			proxyId.ThrowIfNull("proxyId", "Parameter cannot be null.");
			value.ThrowIfNull("value", "Parameter cannot be null.");
			if (_proxies.ContainsKey(proxyId))
				throw new InvalidOperationException(string.Format("Proxy with same name [ {0} ] already exists.", proxyId));
			else
			{
				if (_proxies.TryAdd(proxyId, value))
					Interlocked.Increment(ref _count);
			}
		}

		public dynamic Get(string proxyId)
		{
			proxyId.ThrowIfNull("proxyId", "Parameter cannot be null.");
			dynamic val;
			_proxies.TryGetValue(proxyId, out val);
			return val;
		}

		public void Remove(string proxyId)
		{
			proxyId.ThrowIfNull("proxyId", "Parameter cannot be null.");
			dynamic val;
			if (_proxies.TryRemove(proxyId, out val))
			{
				Interlocked.Decrement(ref _count);
			}
		}
		#endregion

		#region IConfigurable members
		public void Configure()
		{
			var config = Config.ProxyConfigHandler.GetConfig();
			foreach (Config.ProxyMappingElement item in config.Proxies)
			{
				Type targetType = Type.GetType(item.TargetType, false);
				if (targetType == null)
					continue;

				dynamic proxy= DynamicProxyFactory.CreateDynamicProxy(targetType, item.IsWeak);
				this.Add(string.IsNullOrEmpty(item.Id) ? string.Concat(targetType.Name, "Proxy") : item.Id, proxy);
			}
		}
		#endregion
	}
}
