using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	public sealed class DynamicProxyEngine : IEngine
	{
		private static Lazy<DynamicProxyEngine> _initializer;
		private Logging.ILoggerAggregator _logAggregator = null;
		private ConcurrentDictionary<Type, SafeCollection<Type>> _container;

		static DynamicProxyEngine()
		{
			_initializer = new Lazy<DynamicProxyEngine>(() => new DynamicProxyEngine(), true);
		}

		private DynamicProxyEngine()
		{
			_logAggregator = new Logging.LogAggregator();
			_logAggregator.AddLogger(new Logging.Log4netProvider());

			_container = new ConcurrentDictionary<Type, SafeCollection<Type>>();
		}

		public static DynamicProxyEngine Instance
		{
			get
			{
				return _initializer.Value;
			}
		}

		public void Initialize()
		{
			Assembly assembly = typeof(IEngine).Assembly;
			//var types = GetInjectioneers(assembly);
			var types = GetTypesWithHelpAttribute<GetInstanceAttribute>(assembly);
			if (types != null)
			{
				foreach (var type in types)
				{
					IEnumerable<GetInstanceAttribute> attributes = type.GetCustomAttributes(typeof(GetInstanceAttribute), false).Cast<GetInstanceAttribute>();
					foreach (GetInstanceAttribute attr in attributes)
					{
						if (_container.ContainsKey(attr.TypeOf))
							_container[attr.TypeOf].Add(type);
						else
						{
							SafeCollection<Type> holder = new SafeCollection<Type>();
							holder.Add(type);
							_container.TryAdd(attr.TypeOf, holder);
						}
					}
				}
			}
		}

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
		static IEnumerable<Type> GetTypesWithHelpAttribute<T>(Assembly assembly) where T : Attribute
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (type.GetCustomAttributes(typeof(T), true).Length > 0)
				{
					yield return type;
				}
			}
		}

		public Logging.ILoggerAggregator LoggerAggregator
		{
			get { return _logAggregator; }
		}
	}
}
