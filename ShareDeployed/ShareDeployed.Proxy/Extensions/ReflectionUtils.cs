using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	internal static class ReflectionUtils
	{
		public static readonly BindingFlags PublicInstanceStaticMembers = (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		public static readonly BindingFlags PublicInstanceMembers = (BindingFlags.Public | BindingFlags.Instance);
		public static readonly BindingFlags PublicInstanceInvoke = (BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance);


		public static Type GetMemberType(this MemberInfo member)
		{
			if (member is PropertyInfo)
				return (member as PropertyInfo).PropertyType;
			if (member is FieldInfo)
				return (member as FieldInfo).FieldType;

			throw new NotImplementedException();
		}

		public static bool IsVirtual(this PropertyInfo propertyInfo)
		{
			MethodInfo m = propertyInfo.GetGetMethod();
			if (m != null && m.IsVirtual)
				return true;

			m = propertyInfo.GetSetMethod();
			if (m != null && m.IsVirtual)
				return true;

			return false;
		}

		public static MethodInfo GetBaseDefinition(this PropertyInfo propertyInfo)
		{
			MethodInfo m = propertyInfo.GetGetMethod();
			if (m != null)
				return m.GetBaseDefinition();

			m = propertyInfo.GetSetMethod();
			if (m != null)
				return m.GetBaseDefinition();

			return null;
		}

		public static bool HasDefaultConstructor(Type t, bool nonPublic)
		{
			if (t.IsValueType())
				return true;

			return (GetDefaultConstructor(t, nonPublic) != null);
		}

		public static ConstructorInfo GetDefaultConstructor(Type t)
		{
			return GetDefaultConstructor(t, false);
		}

		public static ConstructorInfo GetDefaultConstructor(Type t, bool nonPublic)
		{
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
			if (nonPublic)
				bindingFlags = bindingFlags | BindingFlags.NonPublic;

			return t.GetConstructors(bindingFlags).SingleOrDefault(c => !c.GetParameters().Any());
		}

		public static bool IsValueType(this Type type)
		{
			return type.IsValueType;
		}

		public static bool IsGenericType(this Type type)
		{
			return type.IsGenericType;
		}

		public static bool IsNullable(Type t)
		{

			if (t.IsValueType())
				return IsNullableType(t);

			return true;
		}

		public static bool IsNullableType(Type t)
		{
			return (t.IsGenericType() && t.GetGenericTypeDefinition() == typeof(Nullable<>));
		}

		public static bool IsInterface(this Type t)
		{
			return (t.IsInterface);
		}

		public static bool IsGenericDefinition(this Type type, Type genericInterfaceDefinition)
		{
			if (!type.IsGenericType())
				return false;

			Type t = type.GetGenericTypeDefinition();
			return (t == genericInterfaceDefinition);
		}

		public static bool IsGenericTypeDefinition(this Type type)
		{
			return (type.IsGenericTypeDefinition && type.IsGenericType);
		}

		public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition, out Type implementingType)
		{
			if (!genericInterfaceDefinition.IsInterface() || !genericInterfaceDefinition.IsGenericTypeDefinition())
				throw new ArgumentNullException("'{0}' is not a generic interface definition.".FormatWith(CultureInfo.InvariantCulture, genericInterfaceDefinition));

			if (type.IsInterface())
			{
				if (type.IsGenericType())
				{
					Type interfaceDefinition = type.GetGenericTypeDefinition();

					if (genericInterfaceDefinition == interfaceDefinition)
					{
						implementingType = type;
						return true;
					}
				}
			}

			foreach (Type i in type.GetInterfaces())
			{
				if (i.IsGenericType())
				{
					Type interfaceDefinition = i.GetGenericTypeDefinition();

					if (genericInterfaceDefinition == interfaceDefinition)
					{
						implementingType = i;
						return true;
					}
				}
			}

			implementingType = null;
			return false;
		}

		/// <summary>
		/// Gets the type of the typed collection's items.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The type of the typed collection's items.</returns>
		public static Type GetCollectionItemType(this Type type)
		{
			Type genericListType;

			if (type.IsArray)
			{
				return type.GetElementType();
			}
			else if (ImplementsGenericDefinition(type, typeof(IEnumerable<>), out genericListType))
			{
				if (genericListType.IsGenericTypeDefinition())
					throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));

				return genericListType.GetGenericArguments()[0];
			}
			else if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				return null;
			}
			else
			{
				throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));
			}
		}

		/// <summary>
		/// Determines whether the member is an indexed property.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <returns>
		/// <c>true</c> if the member is an indexed property; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsIndexedProperty(MemberInfo member)
		{
			PropertyInfo propertyInfo = member as PropertyInfo;

			if (propertyInfo != null)
				return IsIndexedProperty(propertyInfo);
			else
				return false;
		}

		/// <summary>
		/// Determines whether the property is an indexed property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		/// <c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsIndexedProperty(PropertyInfo property)
		{
			return (property.GetIndexParameters().Length > 0);
		}

		public static bool IsMethodOverridden(Type currentType, Type methodDeclaringType, string method)
		{
			bool isMethodOverriden = currentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			  .Any(info => info.Name == method &&
				  // check that the method overrides the original on DynamicObjectProxy
							info.DeclaringType != methodDeclaringType &&
							info.GetBaseDefinition().DeclaringType == methodDeclaringType);

			return isMethodOverriden;
		}

	}
}
