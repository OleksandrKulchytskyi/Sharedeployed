using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	/// <summary>
	/// Service instance lifetime enumeration
	/// </summary>
	public enum ServiceLifetime
	{
		Singleton = 0,
		PerRequest,
		PerThread
	}

	public sealed class ServicesMapper : IContractResolver
	{
		// map between contracts -> concrete implementation classes
		private static bool _omitNotRegistered;
		private static IDictionary<Type, KeyValuePair<Type, ServiceLifetime>> _servicesMap;
		//locker object
		private static object _syncRoot;

		static ServicesMapper()
		{
			_servicesMap = new Dictionary<Type, KeyValuePair<Type, ServiceLifetime>>();
			_syncRoot = new object();
		}

		//singletons container
		private ConcurrentDictionary<Type, object> _singletonObjects;
		//per thread container
		private ConcurrentDictionary<ThreadTypeInfo, object> _perThreadObjects;

		/// <summary>
		/// default ctor
		/// </summary>
		public ServicesMapper()
		{
			_singletonObjects = new ConcurrentDictionary<Type, object>();
			_perThreadObjects = new ConcurrentDictionary<ThreadTypeInfo, object>(new ThreadTypeInfoEqualityComparer());
		}

		//static methods 

		/// <summary>
		/// check whether type is registered in services pipeline
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool CanBeResolved(Type type)
		{
			return _servicesMap.ContainsKey(type);
		}

		public static Type RegisterTypes(Type contract, Type impl, ServiceLifetime lifetime = ServiceLifetime.PerRequest, bool throwOnDuplicate = true)
		{
			KeyValuePair<Type, ServiceLifetime> existingImpl;

			if (!_servicesMap.TryGetValue(contract, out existingImpl))
			{
				// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (!_servicesMap.TryGetValue(contract, out existingImpl))
						_servicesMap.Add(contract, new KeyValuePair<Type, ServiceLifetime>(impl, lifetime));
					else if (throwOnDuplicate)
						throw new InvalidOperationException(string.Format("Map already exists between {0} and {1}", contract, impl));
				}
			}

			return contract;
		}

		public static Type Register<T>(ServiceLifetime lifetime = ServiceLifetime.PerRequest, bool throwOnDuplicate = true)
		{
			Type type = typeof(T);
			return RegisterTypes(type, type, lifetime, throwOnDuplicate);
		}

		public static Type ChangeLifetime(Type contract, ServiceLifetime lifetime)
		{
			KeyValuePair<Type, ServiceLifetime> existingImpl;

			if (!_servicesMap.TryGetValue(contract, out existingImpl))
			{	// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (_servicesMap.TryGetValue(contract, out existingImpl))
						_servicesMap[contract] = new KeyValuePair<Type, ServiceLifetime>(existingImpl.Key, lifetime);
					else
						throw new InvalidOperationException(string.Format("Fail to detect mapping for contract {0}.", contract));
				}
			}
			else
				_servicesMap[contract] = new KeyValuePair<Type, ServiceLifetime>(existingImpl.Key, lifetime);

			return contract;
		}

		public static KeyValuePair<Type, ServiceLifetime> GetFullMappingInfo(Type contract)
		{
			KeyValuePair<Type, ServiceLifetime> existingImpl = default(KeyValuePair<Type, ServiceLifetime>);

			if (!_servicesMap.TryGetValue(contract, out existingImpl))
			{	// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (_servicesMap.TryGetValue(contract, out existingImpl))
						return existingImpl;
					else
					{
						if (!_omitNotRegistered)
							throw new InvalidOperationException(string.Format("Mapping for type {0} doesn't exists.", contract));
						if (contract.IsInterface)
							throw new InvalidOperationException(string.Format("Unable to create instance of interface {0}.Mapping doesn't exists.", contract));
						else
							contract.BindToSelf();
						return GetFullMappingInfo(contract);
					}
				}
			}
			else return existingImpl;
		}

		public static Type GetImplementation(Type contract)
		{
			return GetFullMappingInfo(contract).Key;
		}

		//instance methods

		public object Resolve(Type contract)
		{
			KeyValuePair<Type, ServiceLifetime> mapInfo = default(KeyValuePair<Type, ServiceLifetime>);
			if ((mapInfo = ServicesMapper.GetFullMappingInfo(contract)).Key == default(Type))
			{
				if (!OmitNotRegistred)
					throw new InvalidOperationException(string.Format("Mapping for type {0} doesn't exists.", contract));
				else return null;
			}
			else
			{
				CreateInstanceDelegate instanceDelegate;
				object instance;
				switch (mapInfo.Value)
				{
					case ServiceLifetime.Singleton:
						if (_singletonObjects.ContainsKey(mapInfo.Key))
							instance = _singletonObjects[mapInfo.Key];
						else
						{
							instanceDelegate = ObjectCreatorHelper.ObjectInstantiater(mapInfo.Key);
							instance = instanceDelegate();
							if (_singletonObjects.TryAdd(mapInfo.Key, instance))
								break;
							else
								throw new InvalidOperationException("Fail to initialize object in singleton scope.");
						}
						break;
					case ServiceLifetime.PerRequest:
						instanceDelegate = ObjectCreatorHelper.ObjectInstantiater(mapInfo.Key);
						instance = instanceDelegate();
						break;
					case ServiceLifetime.PerThread:
						int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
						ThreadTypeInfo threadTypeInf = new ThreadTypeInfo(threadId, contract);
						if (!_perThreadObjects.ContainsKey(threadTypeInf))
						{
							instanceDelegate = ObjectCreatorHelper.ObjectInstantiater(mapInfo.Key);
							instance = instanceDelegate();
							_perThreadObjects.TryAdd(threadTypeInf, instance);
						}
						else
							_perThreadObjects.TryGetValue(threadTypeInf, out instance);
						break;
					default:
						return null;
				}
				InitializeMembers(contract, instance);
				return instance;
			}
		}

		private void InitializeMembers(Type contract, object instance)
		{
			if (TypeWithInjections.Instance.Contains(contract))
			{
				foreach (MemberMetadata metadata in TypeWithInjections.Instance.GetMetadataFor(contract))
				{
					switch (metadata.MemberType)
					{
						case MemberType.Field:
							(metadata.Member as FieldInfo).SetValue(instance, Resolve(metadata.Type));
							break;
						case MemberType.Property:
							metadata.FastProperty.Set(instance, Resolve(metadata.Type));
							break;
						default:
							break;
					}
				}
				//omit code below
				return;
			}

			IList<MemberInfo> members = contract.GetMembers(ReflectionUtils.PublicInstanceStaticMembers)
						.Where(prop => Attribute.IsDefined(prop, typeof(InstantiateAttribute))).ToList();
			if (members.Count > 0)
			{
				foreach (System.Reflection.MemberInfo mInfo in members)
				{
					TypeWithInjections.Instance.Add(contract, new MemberMetadata(mInfo));
					switch (mInfo.MemberType)
					{
						case System.Reflection.MemberTypes.Field:
							(mInfo as FieldInfo).SetValue(instance, Resolve((mInfo as FieldInfo).FieldType));
							break;

						case System.Reflection.MemberTypes.Property:
							if ((mInfo as PropertyInfo).CanWrite)
								(mInfo as PropertyInfo).SetValue(instance, Resolve((mInfo as PropertyInfo).PropertyType), null);
							break;

						default:
							break;
					}
				}
			}
		}

		public T Resolve<T>()
		{
			return (T)Resolve(typeof(T));
		}

		public IEnumerable<object> ResolveAll(params Type[] types)
		{
			return from t in types
				   where t != null
				   select this.Resolve(t);
		}

		public bool OmitNotRegistred
		{
			get { return _omitNotRegistered; }
			set
			{
				if (_omitNotRegistered != value)
					_omitNotRegistered = value;
			}
		}
	}

	public static class ContractResolverExtension
	{
		/// <summary>
		/// Declare service resolving stratergy in singleton scope
		/// </summary>
		/// <param name="contract"></param>
		/// <returns></returns>
		public static Type InSingletonScope(this Type contract)
		{
			return ServicesMapper.ChangeLifetime(contract, ServiceLifetime.Singleton);
		}

		/// <summary>
		/// Declare service resolving stratergy in once per thread scope
		/// </summary>
		/// <param name="contract"></param>
		/// <returns></returns>
		public static Type InPerThreadScope(this Type contract)
		{
			return ServicesMapper.ChangeLifetime(contract, ServiceLifetime.PerThread);
		}

		/// <summary>
		/// Bing type resolving to self
		/// </summary>
		/// <param name="contract"></param>
		public static Type BindToSelf(this Type contract)
		{
			ServicesMapper.RegisterTypes(contract, contract);
			return contract;
		}

		/// <summary>
		/// Bind type to self in service resolving pipeline
		/// </summary>
		/// <param name="contract"></param>
		/// <param name="excludeException">Omit exception in case when type is already registered</param>
		/// <remarks>Must be first in case of type registration</remarks>
		public static Type BindToSelf(this Type contract, bool excludeException)
		{
			ServicesMapper.RegisterTypes(contract, contract, throwOnDuplicate: false);
			return contract;
		}
	}
}
