using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ShareDeployed.Common.Proxy.FastReflection
{
	public sealed class FastProperty
	{
		public PropertyInfo Property { get; set; }

		private Func<object, object> GetDelegate;
		private Action<object, object> SetDelegate;

		#region ctors
		public FastProperty(PropertyInfo property)
			: this(property, false)
		{
		}

		public FastProperty(PropertyInfo property, bool omitGetInitializer)
		{
			this.Property = property;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		} 
		#endregion

		private void InitializeSet()
		{
			var instance = Expression.Parameter(typeof(object), "instance");
			var value = Expression.Parameter(typeof(object), "value");

			// value as T is slightly faster than (T)value, so if it's not a value type, use that
			UnaryExpression instanceCast = (!this.Property.DeclaringType.IsValueType) ?
				Expression.TypeAs(instance, this.Property.DeclaringType) : Expression.Convert(instance, this.Property.DeclaringType);
			UnaryExpression valueCast = (!this.Property.PropertyType.IsValueType) ?
				Expression.TypeAs(value, this.Property.PropertyType) : Expression.Convert(value, this.Property.PropertyType);
			this.SetDelegate = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, this.Property.GetSetMethod(), valueCast), new ParameterExpression[] { instance, value }).Compile();
		}

		private void InitializeGet()
		{
			var instance = Expression.Parameter(typeof(object), "instance");
			UnaryExpression instanceCast = (!this.Property.DeclaringType.IsValueType) ?
				Expression.TypeAs(instance, this.Property.DeclaringType) : Expression.Convert(instance, this.Property.DeclaringType);
			this.GetDelegate = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, this.Property.GetGetMethod()), typeof(object)), instance).Compile();
		}

		public object Get(object instance)
		{
			return this.GetDelegate(instance);
		}

		public void Set(object instance, object value)
		{
			this.SetDelegate(instance, value);
		}
	}
}
