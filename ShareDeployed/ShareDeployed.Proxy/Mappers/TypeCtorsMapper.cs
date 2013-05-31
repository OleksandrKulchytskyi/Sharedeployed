using ShareDeployed.Proxy.FastReflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ShareDeployed.Proxy
{
	public sealed class TypeCtorsMapper
	{
		private static ConcurrentDictionary<int, SafeCollection<IDynamicConstructor>> _ctorContainer;
		private static Lazy<TypeCtorsMapper> _instance;

		#region ctors
		static TypeCtorsMapper()
		{
			_instance = new Lazy<TypeCtorsMapper>(() => new TypeCtorsMapper(), true);
			_ctorContainer = new ConcurrentDictionary<int, SafeCollection<IDynamicConstructor>>();
		}

		private TypeCtorsMapper()
		{
		}
		#endregion

		public static TypeCtorsMapper Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		#region public methods
		public bool Contains(int contract)
		{
			return _ctorContainer.ContainsKey(contract);
		}

		public ICollection<IDynamicConstructor> Get(int contract)
		{
			SafeCollection<IDynamicConstructor> collection;
			_ctorContainer.TryGetValue(contract, out collection);
			return collection;
		}

		public void Add(int contract, IDynamicConstructor ctor)
		{
			ctor.ThrowIfNull("ctor", "Parameter cannot be a null.");

			if (_ctorContainer.ContainsKey(contract))
			{
				_ctorContainer[contract].Add(ctor);
			}
			else
			{
				SafeCollection<IDynamicConstructor> collection = new SafeCollection<IDynamicConstructor>();
				collection.Add(ctor);
				_ctorContainer.TryAdd(contract, collection);
			}
		}

		public void Remove(int contract)
		{
			SafeCollection<IDynamicConstructor> col;
			_ctorContainer.TryRemove(contract, out col);
		}
		#endregion
	}
}
