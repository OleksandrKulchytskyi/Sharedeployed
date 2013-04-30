﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	public sealed class DynamicProxyPipeline : IPipeline
	{
		private ConcurrentDictionary<Type, object> _internalServices;
		private static Lazy<DynamicProxyPipeline> _initializer;
		private ConcurrentDictionary<Type, SafeCollection<Type>> _container;

		#region ctors
		static DynamicProxyPipeline()
		{
			_initializer = new Lazy<DynamicProxyPipeline>(() => new DynamicProxyPipeline(), true);
		}

		private DynamicProxyPipeline()
		{
			_internalServices = new ConcurrentDictionary<Type, object>();
			_internalServices.TryAdd(typeof(IContractResolver), new ServicesMapper());

			Type logAggrType = typeof(Logging.ILogAggregator);
			ServicesMapper.RegisterTypes(logAggrType, typeof(Logging.LogAggregator)).InSingletonScope();

			_internalServices.TryAdd(logAggrType, ContracResolver.Resolve<Logging.ILogAggregator>());
			LoggerAggregator.AddLogger(new Logging.Log4netProvider());

			_container = new ConcurrentDictionary<Type, SafeCollection<Type>>();
		} 
		#endregion

		public static DynamicProxyPipeline Instance
		{
			get
			{
				return _initializer.Value;
			}
		}

		public void Initialize()
		{
			Assembly assembly = typeof(IPipeline).Assembly;
			//var types = GetInjectioneers(assembly);
			var types = GetTypesWithHelpAttribute<GetInstanceAttribute>(assembly);
			if (types != null)
			{
				foreach (Type curType in types)
				{
					IEnumerable<GetInstanceAttribute> attributes = curType.GetCustomAttributes(typeof(GetInstanceAttribute), false).Cast<GetInstanceAttribute>();
					foreach (GetInstanceAttribute attr in attributes)
					{
						if (_container.ContainsKey(curType))
						{
							_container[curType].Add(attr.TypeOf);
							if (!ServicesMapper.IsRegistered(attr.TypeOf))
								attr.TypeOf.BindToSelf();
						}
						else
						{
							SafeCollection<Type> holder = new SafeCollection<Type>();
							holder.Add(attr.TypeOf);
							_container.TryAdd(curType, holder);
							if (!ServicesMapper.IsRegistered(attr.TypeOf))
								attr.TypeOf.BindToSelf();

						}
					}
					if (!ServicesMapper.IsRegistered(curType))
						curType.BindToSelf();
				}
			}
		}

		public void ReplaceService(Type contract, object service)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null.");
			service.ThrowIfNull("service", "Parameter cannot be null.");
			if (_internalServices.ContainsKey(contract))
				_internalServices.TryUpdate(contract, service, GetInternalService(contract));
			else
				throw new ArgumentException(string.Format("Service {0} doesn't registered in system pipeline.", contract));
		}

		#region private methods
		private List<Type> GetInjectioneers(Assembly assembly)
		{
			var types = (from type in assembly.GetTypes()
						 where type != null && !type.IsInterface && !type.IsGenericType &&
						 !type.IsAbstract && type.IsPublic &&
						 Attribute.IsDefined(typeof(GetInstanceAttribute), type)
						 select type).ToList();
			return types;
		}

		private IEnumerable<KeyValuePair<Type, IEnumerable<T>>> GetCustomAttr<T>() where T : Attribute
		{
			var typesWithMyAttribute = from a in AppDomain.CurrentDomain.GetAssemblies()
									   from t in a.GetTypes()
									   let attributes = t.GetCustomAttributes(typeof(T), true)
									   where attributes != null && attributes.Length > 0
									   select new KeyValuePair<Type, IEnumerable<T>>(t, attributes.Cast<T>());
			return typesWithMyAttribute;
		}

		private IEnumerable<Type> GetTypesWithHelpAttribute<T>(Assembly assembly) where T : Attribute
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (type.GetCustomAttributes(typeof(T), true).Length > 0)
				{
					yield return type;
				}
			}
		}

		private object GetInternalService(Type contract)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null.");
			return _internalServices[contract];
		}

		private T GetInternalService<T>()
		{
			return (T)GetInternalService(typeof(T));
		} 
		#endregion

		#region Pipeline services
		public Logging.ILogAggregator LoggerAggregator
		{
			get { return GetInternalService<Logging.ILogAggregator>(); }
		}

		public IContractResolver ContracResolver
		{
			get
			{
				return GetInternalService<IContractResolver>();
			}
		} 
		#endregion
	}
}
