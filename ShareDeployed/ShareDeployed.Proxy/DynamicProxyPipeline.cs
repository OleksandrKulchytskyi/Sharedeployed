using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Proxy
{
	public sealed class DynamicProxyPipeline : IPipeline, IConfigurable
	{
		private ConcurrentDictionary<Type, object> _internalServices;
		private static Lazy<DynamicProxyPipeline> _initializer;
		private ConcurrentDictionary<Type, SafeCollection<Type>> _container;
		private int _initialized = -1;

		#region ctors
		static DynamicProxyPipeline()
		{
			_initializer = new Lazy<DynamicProxyPipeline>(() => new DynamicProxyPipeline(), true);
		}

		private DynamicProxyPipeline()
		{
			_container = new ConcurrentDictionary<Type, SafeCollection<Type>>();

			_internalServices = new ConcurrentDictionary<Type, object>();
			_internalServices.TryAdd(typeof(IContractResolver), new ServicesMapper());

			Type logAggrType = typeof(Logging.ILogAggregator);
			ServicesMapper.RegisterTypeWithAlias("logAggregator", logAggrType, typeof(Logging.LogAggregator)).InSingletonScope();

			_internalServices.TryAdd(logAggrType, ContracResolver.Resolve<Logging.ILogAggregator>());
			LoggerAggregator.AddLogger(new Logging.Log4netProvider());

			_internalServices.TryAdd(typeof(IDynamicProxyManager), new DynamicProxyManager());
		}
		#endregion

		public static DynamicProxyPipeline Instance
		{
			get { return _initializer.Value; }
		}

		public void Initialize(bool withinDomain = false)
		{
			if (System.Threading.Interlocked.Exchange(ref _initialized, 1) == 1)
				return;

			Assembly assembly = typeof(IPipeline).Assembly;
			//var types = GetInjectioneers(assembly);
			IEnumerable<Type> types = null;

			if (!withinDomain)
				types = GetTypesWithHelpAttribute<GetInstanceAttribute>(assembly);
			else
				types = DomainTypesWithAttribute<GetInstanceAttribute>();

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
							if (!ServicesMapper.CanBeResolved(attr.TypeOf))
								attr.TypeOf.BindToSelf();
						}
						else
						{
							SafeCollection<Type> holder = new SafeCollection<Type>();
							holder.Add(attr.TypeOf);
							_container.TryAdd(curType, holder);
							if (!ServicesMapper.CanBeResolved(attr.TypeOf))
								attr.TypeOf.BindToSelf();
						}
					}
					if (!ServicesMapper.CanBeResolved(curType))
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

		private IEnumerable<KeyValuePair<Type, IEnumerable<T>>> GetCustomAttributes<T>() where T : Attribute
		{
			var typesWithMyAttribute = from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
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

		private IEnumerable<Type> DomainTypesWithAttribute<T>() where T : Attribute
		{
			// Note the AsParallel here, this will parallelize everything after.
			return from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount - 1)
				   from t in a.GetTypes()
				   let attributes = t.GetCustomAttributes(typeof(T), true)
				   where t.IsPublic && attributes != null && attributes.Length > 0
				   select t;

			//return from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
			//	   from t in a.GetTypes()
			//	   let attributes = t.GetCustomAttributes(typeof(T), true)
			//	   where attributes != null && attributes.Length > 0
			//	   select t;

			//return (from assembly in AppDomain.CurrentDomain.GetAssemblies().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount - 1)
			//		from type in assembly.GetTypes()
			//		where type != null && Attribute.IsDefined(typeof(T), type)
			//		select type);

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

		#region Internal pipeline services
		public Logging.ILogAggregator LoggerAggregator
		{
			get { return GetInternalService<Logging.ILogAggregator>(); }
			set
			{
				if (value is Logging.ILogAggregator)
					ReplaceService(typeof(Logging.ILogAggregator), value);
			}
		}

		public IContractResolver ContracResolver
		{
			get
			{
				return GetInternalService<IContractResolver>();
			}
			set
			{
				if (value is IContractResolver)
					ReplaceService(typeof(IContractResolver), value);
			}
		}

		public IDynamicProxyManager DynamixProxyManager
		{
			get { return GetInternalService<IDynamicProxyManager>(); }
			set
			{
				if (value is IDynamicProxyManager)
					ReplaceService(typeof(IDynamicProxyManager), value);
			}
		}
		#endregion

		/// <summary>
		/// Invoke Configure method against all internal services.(Services only inherits from IConfigurable)
		/// </summary>
		public void Configure()
		{
			foreach (var item in _internalServices.Values)
			{
				if ((item as IConfigurable) != null)
				{
					(item as IConfigurable).Configure();
				}
			}
		}
	}
}
