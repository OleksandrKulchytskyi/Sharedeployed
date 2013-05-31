using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Container that holds types with its appropriate members marked with Instantiate attributes
	/// </summary>
	public sealed class TypesWithInjections
	{
		static Lazy<TypesWithInjections> _lazy = null;
		static TypesWithInjections()
		{
			_lazy = new Lazy<TypesWithInjections>(() => new TypesWithInjections(), true);
		}

		private ConcurrentDictionary<Type, SafeCollection<MemberMetadata>> _typeMetadata = null;
		private TypesWithInjections()
		{
			_typeMetadata = new ConcurrentDictionary<Type, SafeCollection<MemberMetadata>>();
		}

		/// <summary>
		/// Get singleton instance
		/// </summary>
		public static TypesWithInjections Instance
		{
			get { return _lazy.Value; ;}
		}

		public void Add(Type type, ref MemberMetadata metadata)
		{
			type.ThrowIfNull("type", "Parameter cannot be a null.");
			if (_typeMetadata.ContainsKey(type))
			{
				_typeMetadata[type].Add(metadata);
			}
			else
			{
				SafeCollection<MemberMetadata> holder = new SafeCollection<MemberMetadata>();
				holder.Add(metadata);
				_typeMetadata.TryAdd(type, holder);
			}
		}

		public void AddRange(Type type, IEnumerable<MemberMetadata> metadatas)
		{
			type.ThrowIfNull("type", "Parameter cannot be a null.");
			if (_typeMetadata.ContainsKey(type))
			{
				_typeMetadata[type].AddRange(metadatas);
			}
			else
			{
				SafeCollection<MemberMetadata> holder = new SafeCollection<MemberMetadata>();
				holder.AddRange(metadatas);
				_typeMetadata.TryAdd(type, holder);
			}
		}

		public bool Contains(Type contract)
		{
			contract.ThrowIfNull("contract", "Parameter cannot be null");
			return _typeMetadata.ContainsKey(contract);
		}

		public IEnumerable<MemberMetadata> GetMetadataFor(Type type)
		{
			SafeCollection<MemberMetadata> metadatas;
			_typeMetadata.TryGetValue(type, out metadatas);
			return metadatas;
		}

		public void Clear()
		{
			_typeMetadata.Clear();
		}
	}
}
