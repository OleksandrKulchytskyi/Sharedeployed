using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public sealed class TypeCtorsMapper
	{
		private static ConcurrentDictionary<Type, SafeCollection<FastReflection.IDynamicConstructor>> _ctors;
		private static Lazy<TypeCtorsMapper> _instance;

		#region ctors
		static TypeCtorsMapper()
		{
			_instance = new Lazy<TypeCtorsMapper>(() => new TypeCtorsMapper(), true);
			_ctors = new ConcurrentDictionary<Type, SafeCollection<FastReflection.IDynamicConstructor>>();
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
		public bool Contains(Type contract)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null.");
			return _ctors.ContainsKey(contract);
		}

		public ICollection<FastReflection.IDynamicConstructor> Get(Type contract)
		{
			SafeCollection<FastReflection.IDynamicConstructor> collection;
			_ctors.TryGetValue(contract, out collection);
			return collection;
		}

		public void Add(Type contract, FastReflection.IDynamicConstructor ctor)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null.");
			ctor.ThrowIfNull("ctor", "Parameter cannot be null.");

			if (_ctors.ContainsKey(contract))
			{
				_ctors[contract].Add(ctor);
			}
			else
			{
				SafeCollection<FastReflection.IDynamicConstructor> collection = new SafeCollection<FastReflection.IDynamicConstructor>();
				collection.Add(ctor);
				_ctors.TryAdd(contract, collection);
			}
		}

		public void Remove(Type contract)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null.");
			SafeCollection<FastReflection.IDynamicConstructor> col;
			_ctors.TryRemove(contract, out col);
		} 
		#endregion
	}
}
