using ShareDeployed.Proxy.Extensions;
using ShareDeployed.Proxy.IoC.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Service instance lifetime enumeration
	/// </summary>
	public enum ServiceLifetime
	{
		PerRequest = 0,
		Singleton,
		PerThread = 2
	}

	/// <summary>
	/// Service mapper
	/// </summary>
	public sealed class ServicesMapper : IContractResolver, IConfigurable
	{
		#region static fields
		// map between contracts -> concrete implementation classes
		private static bool _omitNotRegistered;
		private static IDictionary<Type, KeyValuePair<Type, ServiceLifetime>> _servicesMap;
		//type aliases
		private static ConcurrentDictionary<string, Type> _aliases;
		//locker object
		private static object _syncRoot;
		#endregion

		#region instance fields
		//singletons container
		private ConcurrentDictionary<Type, object> _singletonObjects;
		//per thread container
		private ConcurrentDictionary<ThreadTypeInfo, object> _perThreadObjects;

		private ConcurrentDictionary<Type, SafeCollection<IoC.Config.ServicePropertyElement>> _configProperties;
		private ConcurrentDictionary<Type, SafeCollection<IoC.Config.ServiceCtorArgumentElement>> _configCtorArgs;
		#endregion

		static ServicesMapper()
		{
			_syncRoot = new object();
			_servicesMap = new Dictionary<Type, KeyValuePair<Type, ServiceLifetime>>();
		}

		/// <summary>
		/// default ctor
		/// </summary>
		public ServicesMapper()
		{
			_singletonObjects = new ConcurrentDictionary<Type, object>();
			_perThreadObjects = new ConcurrentDictionary<ThreadTypeInfo, object>(new ThreadTypeInfoEqualityComparer());
			_aliases = new ConcurrentDictionary<string, Type>();

			_configCtorArgs = new ConcurrentDictionary<Type, SafeCollection<ServiceCtorArgumentElement>>();
			_configProperties = new ConcurrentDictionary<Type, SafeCollection<ServicePropertyElement>>();

			_invocations = new List<WeakEventHandler<ResolutionFailEventArgs>>();
		}

		#region Static methods

		/// <summary>
		/// check whether type is registered in services pipeline
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool CanBeResolved(Type type)
		{
			return _servicesMap.ContainsKey(type);
		}

		/// <summary>
		/// Check whether given alias type is registered in system.
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public static bool CanBeResolved(string alias)
		{
			return _aliases.ContainsKey(alias);
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

		public static Type RegisterTypeWithAlias(string alias, Type contract, Type impl, ServiceLifetime lifetime = ServiceLifetime.PerRequest, bool throwOnDuplicate = true)
		{
			alias.ThrowIfNull("alias", "Parameter cannot be a null.");
			Type resolved = RegisterTypes(contract, impl, lifetime, throwOnDuplicate);
			if (!_aliases.ContainsKey(alias))
				_aliases.TryAdd(alias, resolved);
			else
				throw new InvalidOperationException("Alias has been already registered.");
			return resolved;
		}

		public static Type Register<T>(ServiceLifetime lifetime = ServiceLifetime.PerRequest, bool throwOnDuplicate = true)
		{
			Type type = typeof(T);
			return RegisterTypes(type, type, lifetime, throwOnDuplicate);
		}

		public static Type Register<T>(string alias, ServiceLifetime lifetime = ServiceLifetime.PerRequest, bool throwOnDuplicate = true)
		{
			alias.ThrowIfNull("alias", "Parameter cannot be a null.");
			Type type = typeof(T);
			return RegisterTypeWithAlias(alias, type, type, lifetime, throwOnDuplicate);
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

		public static KeyValuePair<Type, ServiceLifetime> GetFullMappingInfo(Type contract, bool omitException = false)
		{
			KeyValuePair<Type, ServiceLifetime> existingImpl = default(KeyValuePair<Type, ServiceLifetime>);

			if (!_servicesMap.TryGetValue(contract, out existingImpl))
			{	// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (_servicesMap.TryGetValue(contract, out existingImpl))
						return existingImpl;
					else if (omitException)
						return existingImpl;
					else
					{
						if (_omitNotRegistered)
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

		public static Type GetImplementationForAlias(string alias)
		{
			alias.ThrowIfNull("alias", "Parameter cannot be a null.");
			Type existed;
			if (_aliases.TryGetValue(alias, out existed))
			{
				return GetImplementation(existed);
			}
			return default(Type);
		}
		#endregion

		#region IContractResolver members
		/// <summary>
		/// Get instance of specific service by its Type
		/// </summary>
		/// <param name="contract"></param>
		/// <returns></returns>
		public object Resolve(Type contract)
		{
			KeyValuePair<Type, ServiceLifetime> mapInfo = default(KeyValuePair<Type, ServiceLifetime>);
			if ((mapInfo = ServicesMapper.GetFullMappingInfo(contract, omitException: true)).Key == default(Type))
			{
				string message = string.Format("Mapping for type {0} doesn't exists.", contract);
				OnResolutionFailed(contract, message);
				if (!OmitNotRegistred)
					throw new InvalidOperationException(message);
				else return null;
			}
			else
			{
				CreateInstanceDelegate instanceDelegate;
				object instance;
				switch (mapInfo.Value)
				{
					case ServiceLifetime.Singleton:
						HandleSingleton(mapInfo, out instanceDelegate, out instance);
						break;
					case ServiceLifetime.PerRequest:
						HandlePerRequest(mapInfo, out instanceDelegate, out instance);
						break;
					case ServiceLifetime.PerThread:
						HandlePerThread(contract, mapInfo, out instanceDelegate, out instance);
						break;
					default:
						return null;
				}
				InitializeInternalMembers(contract, instance);
				InitializeInternalsFromConfiguration(contract, instance);
				return instance;
			}
		}

		private void HandlePerThread(Type contract, KeyValuePair<Type, ServiceLifetime> mapInfo, out CreateInstanceDelegate instanceDel, out object instance)
		{
			instance = null;
			instanceDel = null;

			int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			ThreadTypeInfo threadTypeInf = new ThreadTypeInfo(threadId, contract.GetHashCode());
			if (!_perThreadObjects.ContainsKey(threadTypeInf))
			{
				HandlePerRequest(mapInfo, out instanceDel, out instance);
				if (instance != null)
					_perThreadObjects.TryAdd(threadTypeInf, instance);
			}
			else
				_perThreadObjects.TryGetValue(threadTypeInf, out instance);
		}

		private void HandleSingleton(KeyValuePair<Type, ServiceLifetime> mapInfo, out CreateInstanceDelegate instanceDel, out object instance)
		{
			instanceDel = null;
			instance = null;
			if (_singletonObjects.ContainsKey(mapInfo.Key))
				instance = _singletonObjects[mapInfo.Key];
			else
			{
				HandlePerRequest(mapInfo, out instanceDel, out instance);
				if (instance != null)
					_singletonObjects.TryAdd(mapInfo.Key, instance);
			}
		}

		private void HandlePerRequest(KeyValuePair<Type, ServiceLifetime> mapInfo, out CreateInstanceDelegate instanceDel, out object instance)
		{
			instance = null;
			instanceDel = null;
			if (!mapInfo.Key.HasDefaultCtor())//check if type has default ctor
			{
				if (_configCtorArgs.ContainsKey(mapInfo.Key))
				{
					FastReflection.IDynamicConstructor dynCtor = null;
					int argCount = _configCtorArgs[mapInfo.Key].Count;
					if (TypeCtorsMapper.Instance.Contains(mapInfo.Key.GetHashCode()))
					{
						dynCtor = TypeCtorsMapper.Instance.Get(mapInfo.Key.GetHashCode()).FirstOrDefault(x => x.ParametersCount == argCount);
						if (dynCtor != null)
						{
							object[] parameters = InitCtorParameters(mapInfo.Key, argCount);
							instance = dynCtor.Invoke(parameters);//create new instance of an oject by invoking its ctor.
							return;
						}
					}
					else
					{	//TODO: Potential bug, there might be situation where type has many overloaded ctor's with diff parameters types
						ConstructorInfo ci = mapInfo.Key.GetConstructors(ReflectionUtils.PublicInstanceMembers).
														FirstOrDefault(ctor => ctor.GetParameters().Length == argCount);
						if (ci != null)
						{
							dynCtor = FastReflection.DynamicConstructor.Create(ci);
							TypeCtorsMapper.Instance.Add(mapInfo.Key.GetHashCode(), dynCtor);
							object[] parameters = InitCtorParameters(mapInfo.Key, argCount);
							instance = dynCtor.Invoke(parameters);
							return;
						}
						else
						{
							ConstructorMissingException ex = new ConstructorMissingException(mapInfo.Key, argCount);
							OnResolutionFailed(mapInfo.Key, "Fail to find ctor with specified arguments count.", ex);
							throw ex;
						}
					}
				}
			}
			else
			{	//create or retrieve CreateInstanceDelegate for type with default ctor
				instanceDel = ObjectCreatorHelper.ObjectInstantiater(mapInfo.Key);
				instance = instanceDel();
			}
		}

		private object[] InitCtorParameters(Type type, int argCount)
		{
			object[] data;
			SafeCollection<ServiceCtorArgumentElement> ctorArgs;
			if (!_configCtorArgs.TryGetValue(type, out ctorArgs))
				throw new InvalidOperationException(string.Format("Fail to retrieve ctor arguments info for type {0}", type));
			else
			{
				ServiceCtorArgumentElement[] ctorsArray = new ServiceCtorArgumentElement[ctorArgs.Count];
				data = new object[argCount];
				ctorArgs.CopyTo(ctorsArray, 0);
				Array.Reverse(ctorsArray);
				for (int i = 0; i < argCount; i++)
				{
					if (!string.IsNullOrEmpty(ctorsArray[i].Alias))// in case of filled alias parameter resolve object by its alias
						data[i] = Resolve(ctorsArray[i].Alias);
					else
						data[i] = ConvertHelper.CreateType(ctorsArray[i].ValueType, ctorsArray[i].Value);
				}
				return data;
			}
		}

		private void InitializeInternalMembers(Type contract, object instance)
		{
			if (TypesWithInjections.Instance.Contains(contract))
			{
				foreach (MemberMetadata metadata in TypesWithInjections.Instance.GetMetadataFor(contract))
				{
					switch (metadata.MemberType)
					{
						case MemberType.Field:
							(metadata.Member as FieldInfo).SetValue(instance, Resolve(metadata.Type));
							break;
						case MemberType.Property:
							metadata.FastProperty.Set(instance, Resolve(metadata.Type));
							break;
						default: break;
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
					MemberMetadata mMetadata = new MemberMetadata(mInfo);
					TypesWithInjections.Instance.Add(contract, ref mMetadata);
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

		private void InitializeInternalsFromConfiguration(Type contract, object instance)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be a null.");
			instance.ThrowIfNull("instance", "Parameter cannot be a null.");

			if (_configProperties.ContainsKey(contract))
			{
				foreach (ServicePropertyElement item in _configProperties[contract])
				{
					if (TypesWithInjections.Instance.Contains(contract))
					{
						MemberMetadata metadata = TypesWithInjections.Instance.GetMetadataFor(contract).FirstOrDefault(x => x.FastProperty.Property.Name.Equals(item.Name));
						if (metadata.FastProperty != null)
						{
							object value = string.IsNullOrEmpty(item.Alias) ? ConvertHelper.CreateType(item.ValueType, item.Value) : Resolve(item.Alias);
							metadata.FastProperty.Set(instance, value);
						}
					}
					else
					{
						PropertyInfo pi = instance.GetType().GetProperty(item.Name);
						MemberMetadata newMetadata = new MemberMetadata(pi);
						TypesWithInjections.Instance.Add(contract, ref newMetadata);
						object value = string.IsNullOrEmpty(item.Alias) ? ConvertHelper.CreateType(item.ValueType, item.Value) : Resolve(item.Alias);
						newMetadata.FastProperty.Set(instance, value);
					}
				}
			}
		}

		/// <summary>
		/// Get instance of specific service by its alias
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public object Resolve(string alias)
		{
			alias.ThrowIfNull("alias", "Parameter cannot be a null.");
			Type contract;
			if (_aliases.TryGetValue(alias, out contract))
				return Resolve(contract);
			else
				return null;
		}

		/// <summary>
		/// Get instance of specific service
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Resolve<T>()
		{
			return (T)Resolve(typeof(T));
		}

		/// <summary>
		/// Get instance of specific service by its alias
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="alias"></param>
		/// <returns></returns>
		public T Resolve<T>(string alias)
		{
			return (T)Resolve(alias);
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

		private List<WeakEventHandler<ResolutionFailEventArgs>> _invocations;
		public event EventHandler<ResolutionFailEventArgs> ResolveFailed
		{
			add
			{
				LockFreeExtension.LockFreeUpdate(ref _invocations, d => { d.Add(new WeakEventHandler<ResolutionFailEventArgs>(value)); return d; });
				//lock (_invocations)
				//{
				//	_invocations.Add(new WeakEventHandler<ResolutionFailEventArgs>(value));
				//}
			}
			remove
			{
				//TODO: some issue exists while removing event handler, obviously its related to the creational process fore new WeakEventHandler
				//To be honest the code above won't actually removed event handler.
				//Actually  this won't work anyway! so just perform cleanup via OnResolutionFailed
				//LockFreeExtension.LockFreeUpdate(ref _invocations, d => { d.Remove(new WeakEventHandler<ResolutionFailEventArgs>(value)); return d; });
				//lock (_invocations)
				//{
				//	_invocations.Remove(new WeakEventHandler<ResolutionFailEventArgs>(value));
				//}
			}
		}

		private void OnResolutionFailed(Type resolvingType, string message, Exception ex = null)
		{
			List<WeakEventHandler<ResolutionFailEventArgs>> itemsToRemove = new List<WeakEventHandler<ResolutionFailEventArgs>>();
			WeakEventHandler<ResolutionFailEventArgs>[] array;
			lock (_invocations)
			{
				array = _invocations.ToArray();
			}
			foreach (WeakEventHandler<ResolutionFailEventArgs> weak in array)
			{
				if (weak.IsAlive())
				{
					EventHandler<ResolutionFailEventArgs> handler = (EventHandler<ResolutionFailEventArgs>)weak;
					if (string.IsNullOrEmpty(message))
						handler(this, new ResolutionFailEventArgs(resolvingType, ex));
					else
						handler(this, new ResolutionFailEventArgs(resolvingType, message));
				}
				else
					itemsToRemove.Add(weak);
			}
			lock (_invocations)
			{
				foreach (var item in itemsToRemove)
				{
					_invocations.Remove(item);
				}
			}

			itemsToRemove.Clear();
		}

		#endregion

		#region IConfigurable members
		public void Configure()
		{
			ProxyServicesHandler handler = ProxyServicesHandler.GetConfig();
			if (handler != null)
			{
				foreach (ProxyServiceElement service in handler.Services)
				{
					if (!string.IsNullOrEmpty(service.Alias))
					{
						if (handler.OmitExisting && ServicesMapper.CanBeResolved(service.Alias))
							continue;
						else if (!handler.OmitExisting && ServicesMapper.CanBeResolved(service.Alias))
							throw new InvalidOperationException(string.Format("Alias for service: {0} has already been added.System unable to register alias: {1}",
								service.Type, service.Alias));
					}

					if (string.IsNullOrEmpty(service.Contract))
					{
						Type contract = Type.GetType(service.Type, false);
						if (contract != null)
						{
							contract.BindToSelfWithAliasInScope(string.IsNullOrEmpty(service.Alias) ? contract.FullName : service.Alias, (ServiceLifetime)service.Scope);
							InitializeInternals(contract, contract, service);
						}
						else
							throw new ServiceMapperConfigurationException(string.Format("Unable to resolve type {0}", service.Type));
					}
					else
					{
						Type contract = Type.GetType(service.Contract, false);
						Type expected = Type.GetType(service.Type, false);
						if (contract != null && expected != null)
						{
							ServicesMapper.RegisterTypeWithAlias(string.IsNullOrEmpty(service.Alias) ? contract.FullName : service.Alias,
																	contract, expected, (ServiceLifetime)service.Scope);
							InitializeInternals(contract, expected, service);
						}
						else
							throw new ServiceMapperConfigurationException(string.Format("Unable to resolve types: {0} , {1}", service.Contract, service.Type));
					}
				}
			}
		}

		private void InitializeInternals(Type contract, Type expected, ProxyServiceElement service)
		{
			service.ThrowIfNull("service", "Parameter cannot be a null.");

			if (service.ServiceProps != null && service.ServiceProps.Count > 0)
				InitializePropertyMappings(service.ServiceProps, contract);

			if (service.CtorArgs != null && service.CtorArgs.Count > 0)
				InitializeCtorArgsMappings(service.CtorArgs, contract);
		}

		private void InitializePropertyMappings(ServicePropertyCollection propertyCollection, Type contract)
		{
			if (_configProperties.ContainsKey(contract))
			{
				foreach (ServicePropertyElement item in propertyCollection)
				{
					_configProperties[contract].Add(item);
				}
			}
			else
			{
				SafeCollection<ServicePropertyElement> container = new SafeCollection<ServicePropertyElement>();
				container.AddRange(propertyCollection.Cast<ServicePropertyElement>().AsEnumerable());
				_configProperties.TryAdd(contract, container);
			}
		}

		private void InitializeCtorArgsMappings(ServiceCtorArgCollection ctorArgsCollection, Type contract)
		{
			if (_configCtorArgs.ContainsKey(contract))
			{
				foreach (ServiceCtorArgumentElement item in ctorArgsCollection)
				{
					_configCtorArgs[contract].Add(item);
				}
			}
			else
			{
				SafeCollection<ServiceCtorArgumentElement> container = new SafeCollection<ServiceCtorArgumentElement>();
				container.AddRange(ctorArgsCollection.Cast<ServiceCtorArgumentElement>().AsEnumerable());
				_configCtorArgs.TryAdd(contract, container);
			}
		}
		#endregion

	}

	/// <summary>
	/// Helper class for ServicesMapper
	/// </summary>
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

		/// <summary>
		/// Bind type resolving to self with given alias in default lifetime scope
		/// </summary>
		/// <param name="contract"></param>
		public static Type BindToSelfWithAlias(this Type contract, string alias)
		{
			ServicesMapper.RegisterTypeWithAlias(alias, contract, contract);
			return contract;
		}

		/// <summary>
		/// Bind type resolving to self with given alias and given lifetime scope
		/// </summary>
		/// <param name="contract"></param>
		public static Type BindToSelfWithAliasInScope(this Type contract, string alias, ServiceLifetime scope)
		{
			ServicesMapper.RegisterTypeWithAlias(alias, contract, contract, scope);
			return contract;
		}
	}
}
