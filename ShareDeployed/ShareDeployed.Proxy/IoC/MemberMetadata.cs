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

	public struct MemberMetadata
	{
		public MemberMetadata(MemberInfo memberInfo)
		{
			_type = MemberType.None;
			_memberType = null;
			_mi = null;
			_fp = null;

			memberInfo.ThrowIfNull("memberInfo", "Parameter cannot be null.");
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

		FastReflection.FastProperty _fp;
		/// <summary>
		/// Fast property wrapper
		/// </summary>
		public FastReflection.FastProperty FastProperty
		{
			get { return _fp; }
		}

		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Member.GetHashCode();
		}
	}
}
