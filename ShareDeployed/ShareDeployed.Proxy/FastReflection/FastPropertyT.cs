using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ShareDeployed.Proxy.FastReflection
{
	public class FastProperty<T>
	{
		public PropertyInfo Property { get; private set; }

		public Func<T, object> getDelegate;
		public Action<T, object> setDelegate;

		public FastProperty(PropertyInfo property)
		{
			this.Property = property;
			InitializeGet();
			InitializeSet();
		}

		private void InitializeSet()
		{
			var instance = Expression.Parameter(typeof(T), "instance");
			var value = Expression.Parameter(typeof(object), "value");
			UnaryExpression valueCast = (!this.Property.PropertyType.IsValueType) ?
				Expression.TypeAs(value, this.Property.PropertyType) : Expression.Convert(value, this.Property.PropertyType);
			this.setDelegate = Expression.Lambda<Action<T, object>>(
							Expression.Call(instance, this.Property.GetSetMethod(), valueCast), new ParameterExpression[] { instance, value }).Compile();
		}

		private void InitializeGet()
		{
			var instance = Expression.Parameter(typeof(T), "instance");
			this.getDelegate = Expression.Lambda<Func<T, object>>(Expression.TypeAs(
								Expression.Call(instance, this.Property.GetGetMethod()), typeof(object)), instance).Compile();
		}

		public object Get(T instance)
		{
			return this.getDelegate(instance);
		}

		public void Set(T instance, object value)
		{
			this.setDelegate(instance, value);
		}
	}

	/// <summary>
	/// Fast property enhancement class
	/// </summary>
	/// <typeparam name="T">Instance type</typeparam>
	/// <typeparam name="P">Return value type</typeparam>
	public class FastProperty<T,P>
	{
		public PropertyInfo Property { get; private set; }

		public Func<T, P> getDelegate;
		public Action<T, P> setDelegate;

		public FastProperty(PropertyInfo property)
		{
			this.Property = property;
			InitializeGet();
			InitializeSet();
		}

		private void InitializeSet()
		{
			ParameterExpression instanceExp = Expression.Parameter(typeof(T), "instance");
			ParameterExpression valueExp = Expression.Parameter(typeof(P), "value");
			this.setDelegate = Expression.Lambda<Action<T, P>>(Expression.Call(instanceExp, this.Property.GetSetMethod(), valueExp), 
								new ParameterExpression[] { instanceExp, valueExp }).Compile();
		}

		private void InitializeGet()
		{
			var instance = Expression.Parameter(typeof(T), "instance");
			this.getDelegate = Expression.Lambda<Func<T, P>>(Expression.Call(instance, Property.GetGetMethod()), instance).Compile();
		}

		public P Get(T instance)
		{
			return this.getDelegate(instance);
		}

		public void Set(T instance, P value)
		{
			this.setDelegate(instance, value);
		}
	}
}