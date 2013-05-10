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
			field.ThrowIfNull("field", "Paramater cannot be null.");
			this.Field = field;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		}
		#endregion

		private void InitializeSet()
		{
			//this.setDelegate = MakeFieldSetterUsingExpr();
			this.setDelegate = MakeFieldSetter(this.Field);
		}

		private Action<object, object> MakeFieldSetter(FieldInfo fieldInfo)
		{
			DynamicMethod method = new DynamicMethod("Set" + fieldInfo.Name, null, new Type[] { typeof(object), typeof(object) }, fieldInfo.Module, true);
			ILGenerator il = method.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0); // load the first argument onto the stack (source of type object)
			il.Emit(OpCodes.Castclass, fieldInfo.DeclaringType); // cast the parameter of type object to the type containing the field
			il.Emit(OpCodes.Ldarg_1); // push the second argument onto the stack (this is the value)

			if (fieldInfo.FieldType.IsValueType)
				il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType); // unbox the value parameter to the value-type
			else
				il.Emit(OpCodes.Castclass, fieldInfo.FieldType); // cast the value on the stack to the field type

			il.Emit(OpCodes.Stfld, fieldInfo); // store the value on stack in the field
			il.Emit(OpCodes.Ret); // emit return
			return (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
		}

		private Action<object, object> MakeFieldSetterUsingExpr()
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

			return Expression.Lambda<Action<object, object>>(assignExp, targetExp, valueExp).Compile();
		}

		private void InitializeGet()
		{
			//before
			//this.getDelegate = GetterValue_Delegate_ET();
			//new implementation
			this.getDelegate = MakeFieldGetter(Field);
		}

		private Func<object, object> MakeFieldGetter(FieldInfo field)
		{
			// create a method without a name, object as result type and one parameter of type object the last parameter is very import for accessing private fields
			DynamicMethod dm = new DynamicMethod("Get" + field.Name, typeof(object), new Type[] { typeof(object) }, field.Module, true);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);// Load the instance of the object (argument 0) onto the stack
			il.Emit(OpCodes.Castclass, field.DeclaringType); // cast the parameter of type object to the type containing the field
			// Load the value of the object's field (fi) onto the stack
			il.Emit(OpCodes.Ldfld, field);
			if (field.FieldType.IsValueType)
				il.Emit(OpCodes.Box, field.FieldType); // box the value type, so you will have an object on the stack

			il.Emit(OpCodes.Ret);

			return (Func<object, object>)dm.CreateDelegate(typeof(Func<object, object>));
		}

		private Func<object, object> GetterValue_Delegate()
		{
			var instance = Expression.Parameter(typeof(object), "instance");
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
			field.ThrowIfNull("field", "Paramater cannot be null.");
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
			field.ThrowIfNull("field", "Paramater cannot be null.");
			this.Field = field;
			if (!omitGetInitializer)
				InitializeGet();
			InitializeSet();
		}
		#endregion

		private void InitializeSet()
		{
			//this.setDelegate=MakeSetterUsingExpr();
			this.setDelegate = MakeFieldSetter<T, P>(Field);
		}

		private Action<T, P> MakeSetterUsingExpr()
		{
			ParameterExpression targetExp = Expression.Parameter(typeof(T), "target");
			ParameterExpression valueExp = Expression.Parameter(typeof(P), "value");

			// Expression.Property can be used here as well
			MemberExpression fieldExp = Expression.Field(targetExp, Field);
			BinaryExpression assignExp = Expression.Assign(fieldExp, valueExp);

			return Expression.Lambda<Action<T, P>>(assignExp, targetExp, valueExp).Compile();
		}

		private Action<T, TValue> MakeFieldSetter<T, TValue>(FieldInfo field)
		{
			DynamicMethod dm = new DynamicMethod("Set" + field.Name, typeof(void), new Type[] { typeof(T), typeof(TValue) }, this.GetType(), true);
			ILGenerator il = dm.GetILGenerator();

			// arg0.<field> = arg1
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, field);
			il.Emit(OpCodes.Ret);

			return (Action<T, TValue>)dm.CreateDelegate(typeof(Action<T, TValue>));
		}

		private void InitializeGet()
		{
			//this.getDelegate = GetterValue_DelegateExpr();
			this.getDelegate = MakeFieldGetter<T, P>(Field);
		}

		private Func<T, TValue> MakeFieldGetter<T, TValue>(FieldInfo field)
		{
			DynamicMethod dm = new DynamicMethod("Get" + field.Name, typeof(TValue), new Type[] { typeof(T) }, this.GetType(), true);
			ILGenerator il = dm.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, field);
			il.Emit(OpCodes.Ret);

			return (Func<T, TValue>)dm.CreateDelegate(typeof(Func<T, TValue>));
		}

		private Func<T, P> Getter_DelegateExpr()
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
