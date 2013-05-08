using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ShareDeployed.Proxy.FastReflection
{
	public interface IDynamicField
	{
		object GetValue(object target);

		void SetValue(object target, object value);
	}

	/// <summary>
	/// Represents a Get method
	/// </summary>
	/// <param name="target">the target instance when calling an instance method</param>
	/// <returns>the value return by the Get method</returns>
	public delegate object FieldGetterDelegate(object target);

	/// <summary>
	/// Represents a Set method
	/// </summary>
	/// <param name="target">the target instance when calling an instance method</param>
	/// <param name="value">the value to be set</param>
	public delegate void FieldSetterDelegate(object target, object value);

	public class SafeFieldWrapper : IDynamicField
	{
		private readonly FieldInfo fieldInfo;

		#region Cache

		private static readonly IDictionary<FieldInfo, DynamicFieldInfo> fieldCache =
								new Dictionary<FieldInfo, DynamicFieldInfo>();

		/// <summary>
		/// Holds cached Getter/Setter delegates for a Field
		/// </summary>
		private class DynamicFieldInfo
		{
			public readonly FieldGetterDelegate Getter;
			public readonly FieldSetterDelegate Setter;

			public DynamicFieldInfo(FieldGetterDelegate getter, FieldSetterDelegate setter)
			{
				Getter = getter;
				Setter = setter;
			}
		}

		/// <summary>
		/// Obtains cached fieldInfo or creates a new entry, if none is found.
		/// </summary>
		private static DynamicFieldInfo GetOrCreateDynamicField(FieldInfo field)
		{
			DynamicFieldInfo fieldInfo;
			if (!fieldCache.TryGetValue(field, out fieldInfo))
			{
				fieldInfo = new DynamicFieldInfo(ILManager.CreateFieldGetter(field), ILManager.CreateFieldSetter(field));
				lock (fieldCache)
				{
					fieldCache[field] = fieldInfo;
				}
			}
			return fieldInfo;
		}

		#endregion

		private readonly FieldGetterDelegate getter;
		private readonly FieldSetterDelegate setter;

		/// <summary>
		/// Creates a new instance of the safe field wrapper.
		/// </summary>
		/// <param name="field">Field to wrap.</param>
		public SafeFieldWrapper(FieldInfo field)
		{
			field.ThrowIfNull("field", "You cannot create a dynamic field for a null value.");

			fieldInfo = field;
			DynamicFieldInfo fi = GetOrCreateDynamicField(field);
			getter = fi.Getter;
			setter = fi.Setter;
		}

		/// <summary>
		/// Gets the value of the dynamic field for the specified target object.
		/// </summary>
		/// <param name="target">
		/// Target object to get field value from.
		/// </param>
		/// <returns>
		/// A field value.
		/// </returns>
		public object GetValue(object target)
		{
			return getter(target);
		}

		/// <summary>
		/// Gets the value of the dynamic field for the specified target object.
		/// </summary>
		/// <param name="target">
		/// Target object to set field value on.
		/// </param>
		/// <param name="value">
		/// A new field value.
		/// </param>
		public void SetValue(object target, object value)
		{
			setter(target, value);
		}

		internal FieldInfo FieldInfo
		{
			get { return fieldInfo; }
		}
	}

	public class DynamicField
	{
		/// <summary>
		/// Creates dynamic field instance for the specified <see cref="FieldInfo"/>.
		/// </summary>
		/// <param name="field">Field info to create dynamic field for.</param>
		/// <returns>Dynamic field for the specified <see cref="FieldInfo"/>.</returns>
		public static IDynamicField Create(FieldInfo field)
		{
			field.ThrowIfNull("field", "You cannot create a dynamic field for a null value.");

			IDynamicField dynamicField = new SafeFieldWrapper(field);
			return dynamicField;
		}
	}
}
