using ShareDeployed.Proxy.FastReflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ShareDeployed.Proxy
{
	internal class InjectionCtorInfo
	{
		public InjectionCtorInfo(IDynamicConstructor ctor, IList<Type> paramsTypes)
		{
			_ctor = ctor;
			_parametersType = paramsTypes;
		}

		IDynamicConstructor _ctor;
		public IDynamicConstructor DynamicCtor
		{
			get { return _ctor; }
		}

		IList<Type> _parametersType;
		public IList<Type> ParametersTypes
		{
			get { return _parametersType; }
		}
	}

	internal sealed class InjectionCtorMapper
	{
		private static ConcurrentDictionary<int, InjectionCtorInfo> _typeInjectionCtor;
		private static Lazy<InjectionCtorMapper> _instance;

		#region ctors
		static InjectionCtorMapper()
		{
			_instance = new Lazy<InjectionCtorMapper>(() => new InjectionCtorMapper(), true);
			_typeInjectionCtor = new ConcurrentDictionary<int, InjectionCtorInfo>();
		}

		private InjectionCtorMapper()
		{
		}
		#endregion

		public static InjectionCtorMapper Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		#region public methods
		public bool Contains(int contract)
		{
			return _typeInjectionCtor.ContainsKey(contract);
		}

		public InjectionCtorInfo Get(int contract)
		{
			InjectionCtorInfo ctorInfo;
			_typeInjectionCtor.TryGetValue(contract, out ctorInfo);
			return ctorInfo;
		}

		public void Add(int contract, InjectionCtorInfo ctorInfo)
		{
			ctorInfo.ThrowIfNull("ctorInfo", "Parameter cannot be a null.");
			_typeInjectionCtor.TryAdd(contract, ctorInfo);
		}

		public void Remove(int contract)
		{
			InjectionCtorInfo ctor;
			_typeInjectionCtor.TryRemove(contract, out ctor);
		}
		#endregion
	}
}
