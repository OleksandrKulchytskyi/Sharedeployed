using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
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
					_fp = new FastReflection.FastProperty(memberInfo as PropertyInfo);
					break;
				default:
					break;
			}
		}

		private MemberInfo _mi;
		public MemberInfo Member
		{
			get { return _mi; }
			set { _mi = value; }
		}

		private MemberType _type;
		public MemberType MemberType
		{
			get { return _type; }
			set { if (_type != value)_type = value; }
		}

		private Type _memberType;
		public Type Type{
			get { return _memberType; }
		}

		FastReflection.FastProperty _fp;
		public FastReflection.FastProperty FastProperty
		{
			get { return _fp; }
		}
	}
}
