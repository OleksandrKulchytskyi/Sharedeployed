using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy.FastReflection
{
	public sealed class FastField
	{
		public FieldInfo Field { get; private set; }

		private Func<object, object> getDelegate;
		private Action<object, object> setDelegate;

		#region ctors
		public FastField(FieldInfo field)
			: this(field, false)
		{
		}

		public FastField(FieldInfo field, bool omitGetInitializer)
		{
			this.Field = field;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		}
		#endregion

		private void InitializeSet()
		{
			ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
			ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");

			UnaryExpression instanceCast = (!this.Field.DeclaringType.IsValueType) ?
				Expression.TypeAs(targetExp, this.Field.DeclaringType) : Expression.Convert(targetExp, this.Field.DeclaringType);
			UnaryExpression valueCast = (!this.Field.FieldType.IsValueType) ?
				Expression.TypeAs(valueExp, this.Field.FieldType) : Expression.Convert(valueExp, this.Field.FieldType);

			// Expression.Property can be used here as well
			MemberExpression fieldExp = Expression.Field(instanceCast, Field);
			BinaryExpression assignExp = Expression.Assign(fieldExp, valueCast);

			this.setDelegate = Expression.Lambda<Action<object, object>>(assignExp, targetExp, valueExp).Compile();
		}

		private void InitializeGet()
		{
			this.getDelegate = GetterValue_Delegate_ET();
		}

		//private Action<T, TValue> MakeSetter<T, TValue>(FieldInfo field)
		//{
		//	DynamicMethod m = new DynamicMethod("setter", typeof(void), new Type[] { typeof(T), typeof(TValue) }, typeof(Program));
		//	ILGenerator cg = m.GetILGenerator();

		//	// arg0.<field> = arg1
		//	cg.Emit(OpCodes.Ldarg_0);
		//	cg.Emit(OpCodes.Ldarg_1);
		//	cg.Emit(OpCodes.Stfld, field);
		//	cg.Emit(OpCodes.Ret);

		//	return (Action<T, TValue>)m.CreateDelegate(typeof(Action<T, TValue>));
		//}

		private Func<object, object> GetterValue_Delegate_ET()
		{
			var instance = Expression.Parameter(typeof(object), "i");
			var convertInstance = Expression.TypeAs(instance, Field.DeclaringType);
			var property = Expression.Field(convertInstance, Field);
			var convertProperty = Expression.TypeAs(property, typeof(object));
			return Expression.Lambda<Func<object, object>>(convertProperty, instance).Compile();
		}

		public object Get(object instance)
		{
			return this.getDelegate(instance);
		}

		public void Set(object instance, object value)
		{
			this.setDelegate(instance, value);
		}
	}

	public sealed class FastField<T>
	{
		public FieldInfo Field { get; private set; }

		private Func<T, object> getDelegate;
		private Action<T, object> setDelegate;

		#region ctors
		public FastField(FieldInfo field)
			: this(field, false)
		{
		}

		public FastField(FieldInfo field, bool omitGetInitializer)
		{
			this.Field = field;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		}
		#endregion

		private void InitializeSet()
		{
			ParameterExpression targetExp = Expression.Parameter(typeof(T), "target");
			ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");

			UnaryExpression valueCast = (!this.Field.FieldType.IsValueType) ?
				Expression.TypeAs(valueExp, this.Field.FieldType) : Expression.Convert(valueExp, this.Field.FieldType);

			// Expression.Property can be used here as well
			MemberExpression fieldExp = Expression.Field(targetExp, Field);
			BinaryExpression assignExp = Expression.Assign(fieldExp, valueCast);
			this.setDelegate = Expression.Lambda<Action<T, object>>(assignExp, targetExp, valueExp).Compile();
		}

		private void InitializeGet()
		{
			this.getDelegate = GetterValue_Delegate();
		}

		private Func<T, object> GetterValue_Delegate()
		{
			ParameterExpression instanceExp = Expression.Parameter(typeof(T), "i");
			var property = Expression.Field(instanceExp, Field);
			var convertProperty = Expression.TypeAs(property, typeof(object));
			return Expression.Lambda<Func<T, object>>(convertProperty, instanceExp).Compile();
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

	public sealed class FastField<T, P>
	{
		public FieldInfo Field { get; private set; }

		private Func<T, P> getDelegate;
		private Action<T, P> setDelegate;

		#region ctors
		public FastField(FieldInfo field)
			: this(field, false)
		{
		}

		public FastField(FieldInfo field, bool omitGetInitializer)
		{
			this.Field = field;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		}
		#endregion

		private void InitializeSet()
		{
			ParameterExpression targetExp = Expression.Parameter(typeof(T), "target");
			ParameterExpression valueExp = Expression.Parameter(typeof(P), "value");

			// Expression.Property can be used here as well
			MemberExpression fieldExp = Expression.Field(targetExp, Field);
			BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);

			this.setDelegate = Expression.Lambda<Action<T, P>>(assignExp, targetExp, valueExp).Compile();
		}

		private void InitializeGet()
		{
			this.getDelegate = GetterValue_Delegate();
		}

		private Func<T, P> GetterValue_Delegate()
		{
			ParameterExpression instanceExp = Expression.Parameter(typeof(T), "i");
			MemberExpression fieldExp = Expression.Field(instanceExp, Field);
			return Expression.Lambda<Func<T, P>>(fieldExp, instanceExp).Compile();
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
