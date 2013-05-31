using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Member type enumeration
	/// </summary>
	public enum MemberType
	{
		None = 0,
		Field,
		Property
	}

	public struct MemberMetadata : IEquatable<MemberMetadata>
	{
		public MemberMetadata(MemberInfo memberInfo)
		{
			_type = MemberType.None;
			_memberType = null;
			_mi = null;
			_fp = null;
			_hash = -1;

			memberInfo.ThrowIfNull("memberInfo", "Parameter cannot be a null.");
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Field:
					_type = MemberType.Field;
					_mi = memberInfo;
					_memberType = (memberInfo as FieldInfo).FieldType;
					break;
				case MemberTypes.Property:
					_type = MemberType.Property;
					_memberType = (memberInfo as PropertyInfo).PropertyType;
					_fp = new FastReflection.FastProperty((memberInfo as PropertyInfo), true);
					_mi = memberInfo;
					break;
				default:
					break;
			}
		}

		private MemberInfo _mi;
		/// <summary>
		/// Member info data
		/// </summary>
		public MemberInfo Member
		{
			get { return _mi; }
			set { _mi = value; }
		}

		private MemberType _type;
		/// <summary>
		/// Indicates whether member is field or property
		/// </summary>
		public MemberType MemberType
		{
			get { return _type; }
			set { if (_type != value)_type = value; }
		}

		private Type _memberType;
		/// <summary>
		/// Type of Member
		/// </summary>
		public Type Type
		{
			get { return _memberType; }
		}

		private FastReflection.FastProperty _fp;
		/// <summary>
		/// Fast property wrapper
		/// </summary>
		public FastReflection.FastProperty FastProperty
		{
			get { return _fp; }
		}

		private int _hash;
		public override int GetHashCode()
		{
			if (_hash == -1)
			{
				_hash = 17;
				_hash = _hash * 31 + Type.GetHashCode();
				_hash = _hash * 31 + Member.GetHashCode();
			}
			return _hash;
		}

		public override bool Equals(object obj)
		{
			return (obj is MemberMetadata) ? Equals((MemberMetadata)obj) : false;
		}

		public bool Equals(MemberMetadata compare)
		{
			return (this._type.Equals(compare._type) && _memberType.Equals(compare._memberType));
		}
	}
}
