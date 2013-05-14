using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ShareDeployed.Proxy
{
	public sealed class TypeCtorsMapper
	{
		private static ConcurrentDictionary<int, SafeCollection<FastReflection.IDynamicConstructor>> _ctorContainer;
		private static Lazy<TypeCtorsMapper> _instance;

		#region ctors
		static TypeCtorsMapper()
		{
			_instance = new Lazy<TypeCtorsMapper>(() => new TypeCtorsMapper(), true);
			_ctorContainer = new ConcurrentDictionary<int, SafeCollection<FastReflection.IDynamicConstructor>>();
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

		public ICollection<FastReflection.IDynamicConstructor> Get(int contract)
		{
			SafeCollection<FastReflection.IDynamicConstructor> collection;
			_ctorContainer.TryGetValue(contract, out collection);
			return collection;
		}

		public void Add(int contract, FastReflection.IDynamicConstructor ctor)
		{
			ctor.ThrowIfNull("ctor", "Parameter cannot be null.");

			if (_ctorContainer.ContainsKey(contract))
			{
				_ctorContainer[contract].Add(ctor);
			}
			else
			{
				SafeCollection<FastReflection.IDynamicConstructor> collection = new SafeCollection<FastReflection.IDynamicConstructor>();
				collection.Add(ctor);
				_ctorContainer.TryAdd(contract, collection);
			}
		}

		public void Remove(int contract)
		{
			SafeCollection<FastReflection.IDynamicConstructor> col;
			_ctorContainer.TryRemove(contract, out col);
		}
		#endregion
	}
}
