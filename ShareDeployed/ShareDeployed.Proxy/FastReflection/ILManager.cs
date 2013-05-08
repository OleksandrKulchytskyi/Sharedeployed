using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using NetDynamicMethod = System.Reflection.Emit.DynamicMethod;

namespace ShareDeployed.Proxy.FastReflection
{
	public sealed class ILManager
	{
		private static OpCode[] LdArgOpCodes = { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2 };

		private delegate Type GetTypeFromHandleDelegate(RuntimeTypeHandle handle);
		private static readonly MethodInfo FnGetTypeFromHandle = new GetTypeFromHandleDelegate(Type.GetTypeFromHandle).Method;
		private delegate object ChangeTypeDelegate(object value, Type targetType, int argIndex);
		private static readonly MethodInfo FnConvertArgumentIfNecessary = new ChangeTypeDelegate(ConvertValueTypeArgumentIfNecessary).Method;

		private static readonly ConstructorInfo NewInvalidOperationException =
		   typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) });

		private ILManager()
		{
		}


		/// <summary>
		/// Converts <paramref name="value"/> to an instance of <paramref name="targetType"/> if necessary to 
		/// e.g. avoid e.g. double/int cast exceptions.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method mimics the behavior of the compiler that
		/// automatically performs casts like int to double in "Math.Sqrt(4)".<br/>
		/// See about implicit, widening type conversions on <a href="http://social.msdn.microsoft.com/Search/en-US/?query=type conversion tables">MSDN - Type Conversion Tables</a>
		/// </para>
		/// <para>
		/// Note: <paramref name="targetType"/> is expected to be a value type! 
		/// </para>
		/// </remarks>
		public static object ConvertValueTypeArgumentIfNecessary(object value, Type targetType, int argIndex)
		{
			if (value == null)
			{
				if (ReflectionUtils.IsNullableType(targetType))
				{
					return null;
				}
				throw new InvalidCastException(string.Format("Cannot convert NULL at position {0} to argument type {1}", argIndex, targetType.FullName));
			}

			Type valueType = value.GetType();

			if (ReflectionUtils.IsNullableType(targetType))
			{
				targetType = Nullable.GetUnderlyingType(targetType);
			}

			// no conversion necessary?
			if (valueType == targetType)
			{
				return value;
			}

			if (!valueType.IsValueType)
			{
				// we're facing a reftype/valuetype mix that never can convert
				throw new InvalidCastException(string.Format("Cannot convert value '{0}' of type {1} at position {2} to argument type {3}", value, valueType.FullName, argIndex, targetType.FullName));
			}

			// we're dealing only with ValueType's now - try to convert them
			try
			{
				// TODO: allow widening conversions only
				return Convert.ChangeType(value, targetType);
			}
			catch (Exception ex)
			{
				throw new InvalidCastException(string.Format("Cannot convert value '{0}' of type {1} at position {2} to argument type {3}", value, valueType.FullName, argIndex, targetType.FullName), ex);
			}
		}

		///<summary>
		/// Creates a new delegate for the specified constructor.
		///</summary>
		///<param name="constructorInfo">the constructor to create the delegate for</param>
		///<returns>delegate that can be used to invoke the constructor.</returns>
		public static ConstructorDelegate CreateConstructor(ConstructorInfo constructorInfo)
		{
			constructorInfo.ThrowIfNull("constructorInfo", "You cannot create a dynamic constructor for a null value.");

			bool skipVisibility = true; //!IsPublic(constructorInfo);
			System.Reflection.Emit.DynamicMethod dmGetter;
			Type[] argumentTypes = new Type[] { typeof(object[]) };
			dmGetter = CreateDynamicMethod(constructorInfo.Name, typeof(object), argumentTypes, constructorInfo, skipVisibility);
			ILGenerator il = dmGetter.GetILGenerator();
			EmitInvokeConstructor(il, constructorInfo, false);
			ConstructorDelegate ctor = (ConstructorDelegate)dmGetter.CreateDelegate(typeof(ConstructorDelegate));
			return ctor;
		}

		/// <summary>
		/// Creates a <see cref="System.Reflection.Emit.DynamicMethod"/> instance with the highest possible code access security.
		/// </summary>
		/// <remarks>
		/// If allowed by security policy, associates the method with the <paramref name="member"/>s declaring type. 
		/// Otherwise associates the dynamic method with <see cref="DynamicReflectionManager"/>.
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static NetDynamicMethod CreateDynamicMethod(string methodName, Type returnType, Type[] argumentTypes, MemberInfo member, bool skipVisibility)
		{
			NetDynamicMethod dmGetter = null;
			methodName = "_dynamic_" + member.DeclaringType.FullName + "." + methodName;
			try
			{
				new PermissionSet(PermissionState.Unrestricted).Demand();
				dmGetter = CreateDynamicMethodInternal(methodName, returnType, argumentTypes, member, skipVisibility);
			}
			catch (SecurityException)
			{
				dmGetter = CreateDynamicMethodInternal(methodName, returnType, argumentTypes, MethodBase.GetCurrentMethod(), false);
			}
			return dmGetter;
		}

		private static NetDynamicMethod CreateDynamicMethodInternal(string methodName, Type returnType, Type[] argumentTypes, MemberInfo member, bool skipVisibility)
		{
			NetDynamicMethod dm;
			dm = new NetDynamicMethod(methodName, returnType, argumentTypes, member.Module, skipVisibility);
			return dm;
		}

		internal static void EmitInvokeConstructor(ILGenerator il, ConstructorInfo constructor, bool isInstanceMethod)
		{
			int paramsArrayPosition = (isInstanceMethod) ? 1 : 0;
			ParameterInfo[] args = constructor.GetParameters();

			IDictionary outArgs = new Hashtable();
			for (int i = 0; i < args.Length; i++)
			{
				SetupOutputArgument(il, paramsArrayPosition, args[i], outArgs);
			}

			for (int i = 0; i < args.Length; i++)
			{
				SetupMethodArgument(il, paramsArrayPosition, args[i], null);
			}

			il.Emit(OpCodes.Newobj, constructor);

			for (int i = 0; i < args.Length; i++)
			{
				ProcessOutputArgument(il, paramsArrayPosition, args[i], outArgs);
			}

			EmitMethodReturn(il, constructor.DeclaringType);
		}
		
		internal static void EmitPropertyGetter(ILGenerator il, PropertyInfo propertyInfo, bool isInstanceMethod)
		{
			if (propertyInfo.CanRead)
			{
				MethodInfo getMethod = propertyInfo.GetGetMethod(true);
				EmitInvokeMethod(il, getMethod, isInstanceMethod);
			}
			else
			{
				EmitThrowInvalidOperationException(il, string.Format("Cannot read from write-only property '{0}.{1}'", propertyInfo.DeclaringType.FullName, propertyInfo.Name));
			}
		}

		internal static void EmitInvokeMethod(ILGenerator il, MethodInfo method, bool isInstanceMethod)
		{
			int paramsArrayPosition = (isInstanceMethod) ? 2 : 1;
			ParameterInfo[] args = method.GetParameters();
			IDictionary outArgs = new Hashtable();
			for (int i = 0; i < args.Length; i++)
			{
				SetupOutputArgument(il, paramsArrayPosition, args[i], outArgs);
			}

			if (!method.IsStatic)
			{
				EmitTarget(il, method.DeclaringType, isInstanceMethod);
			}

			for (int i = 0; i < args.Length; i++)
			{
				SetupMethodArgument(il, paramsArrayPosition, args[i], outArgs);
			}

			EmitCall(il, method);

			for (int i = 0; i < args.Length; i++)
			{
				ProcessOutputArgument(il, paramsArrayPosition, args[i], outArgs);
			}

			EmitMethodReturn(il, method.ReturnType);
		}

		private static void EmitTarget(ILGenerator il, Type targetType, bool isInstanceMethod)
		{
			il.Emit((isInstanceMethod) ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0);
			if (targetType.IsValueType)
			{
				LocalBuilder local = il.DeclareLocal(targetType);
				EmitUnbox(il, targetType);
				il.Emit(OpCodes.Stloc_0);
				il.Emit(OpCodes.Ldloca_S, 0);
			}
			else
			{
				il.Emit(OpCodes.Castclass, targetType);
			}
		}

		private static void EmitCall(ILGenerator il, MethodInfo method)
		{
			il.EmitCall((method.IsVirtual) ? OpCodes.Callvirt : OpCodes.Call, method, null);
		}

		private static void EmitUnbox(ILGenerator il, Type argumentType)
		{
			il.Emit(OpCodes.Unbox_Any, argumentType);
		}

		/// <summary>
		/// Generates code to process return value if necessary.
		/// </summary>
		/// <param name="il">IL generator to use.</param>
		/// <param name="returnValueType">Type of the return value.</param>
		private static void EmitMethodReturn(ILGenerator il, Type returnValueType)
		{
			if (returnValueType == typeof(void))
			{
				il.Emit(OpCodes.Ldnull);
			}
			else if (returnValueType.IsValueType)
			{
				il.Emit(OpCodes.Box, returnValueType);
			}
			il.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Generates code that throws <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="il">IL generator to use.</param>
		/// <param name="message">Error message to use.</param>
		private static void EmitThrowInvalidOperationException(ILGenerator il, string message)
		{
			il.Emit(OpCodes.Ldstr, message);
			il.Emit(OpCodes.Newobj, NewInvalidOperationException);
			il.Emit(OpCodes.Throw);
		}

		private static bool IsOutputOrRefArgument(ParameterInfo argInfo)
		{
			return argInfo.IsOut || argInfo.ParameterType.Name.EndsWith("&");
		}

		private static void SetupOutputArgument(ILGenerator il, int paramsArrayPosition, ParameterInfo argInfo, IDictionary outArgs)
		{
			if (!IsOutputOrRefArgument(argInfo))
				return;

			Type argType = argInfo.ParameterType.GetElementType();

			LocalBuilder lb = il.DeclareLocal(argType);
			if (!argInfo.IsOut)
			{
				PushParamsArgumentValue(il, paramsArrayPosition, argType, argInfo.Position);
				il.Emit(OpCodes.Stloc, lb);
			}
			outArgs[argInfo.Position] = lb;
		}

		private static void SetupMethodArgument(ILGenerator il, int paramsArrayPosition, ParameterInfo argInfo, IDictionary outArgs)
		{
			if (IsOutputOrRefArgument(argInfo))
			{
				il.Emit(OpCodes.Ldloca_S, (LocalBuilder)outArgs[argInfo.Position]);
			}
			else
			{
				PushParamsArgumentValue(il, paramsArrayPosition, argInfo.ParameterType, argInfo.Position);
			}
		}

		private static void PushParamsArgumentValue(ILGenerator il, int paramsArrayPosition, Type argumentType, int argumentPosition)
		{
			il.Emit(LdArgOpCodes[paramsArrayPosition]);
			il.Emit(OpCodes.Ldc_I4, argumentPosition);
			il.Emit(OpCodes.Ldelem_Ref);
			if (argumentType.IsValueType)
			{
				// call ConvertArgumentIfNecessary() to convert e.g. int32 to double if necessary
				il.Emit(OpCodes.Ldtoken, argumentType);
				EmitCall(il, FnGetTypeFromHandle);
				il.Emit(OpCodes.Ldc_I4, argumentPosition);
				EmitCall(il, FnConvertArgumentIfNecessary);
				EmitUnbox(il, argumentType);
			}
			else
			{
				il.Emit(OpCodes.Castclass, argumentType);
			}
		}

		

		private static void ProcessOutputArgument(ILGenerator il, int paramsArrayPosition, ParameterInfo argInfo, IDictionary outArgs)
		{
			if (!IsOutputOrRefArgument(argInfo))
				return;

			Type argType = argInfo.ParameterType.GetElementType();

			il.Emit(LdArgOpCodes[paramsArrayPosition]);
			il.Emit(OpCodes.Ldc_I4, argInfo.Position);
			il.Emit(OpCodes.Ldloc, (LocalBuilder)outArgs[argInfo.Position]);
			if (argType.IsValueType)
			{
				il.Emit(OpCodes.Box, argType);
			}
			il.Emit(OpCodes.Stelem_Ref);
		}
	}
}
