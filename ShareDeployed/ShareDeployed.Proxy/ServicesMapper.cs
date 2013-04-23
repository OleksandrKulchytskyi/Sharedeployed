using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	public sealed class ServicesMapper
	{
		// A MAP BETWEEN CONTRACTS -> CONCRETE IMPLEMENTATION CLASSES
		private static IDictionary<Type, Type> _servicesMap;
		private static object _syncRoot;

		static ServicesMapper()
		{
			_servicesMap = new Dictionary<Type, Type>();
			_syncRoot = new object();
		}

		private ServicesMapper()
		{
		}

		public static void RegisterTypes(Type abstraction, Type impl)
		{
			Type existingImpl;

			if (!_servicesMap.TryGetValue(abstraction, out existingImpl))
			{
				// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (!_servicesMap.TryGetValue(abstraction, out existingImpl))
					{
						_servicesMap.Add(abstraction, impl);
					}
					else
						throw new InvalidOperationException(string.Format("Map already exists between {0} and {1}", abstraction, impl));
				}
			}
		}

		public static Type GetImplementation(Type abstraction)
		{
			Type existingImpl = default(Type);

			if (!_servicesMap.TryGetValue(abstraction, out existingImpl))
			{
				// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					_servicesMap.TryGetValue(abstraction, out existingImpl);
				}
			}

			return existingImpl;
		}
	}
}
