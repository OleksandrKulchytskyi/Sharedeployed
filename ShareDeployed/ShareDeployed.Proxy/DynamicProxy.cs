﻿using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace ShareDeployed.Common.Proxy
{
	public enum ExecutionInjectionMode
	{
		None = 0,
		Before,
		After,
		OnError
	}

	public class InterceptorInfo
	{
		public InterceptorInfo(Type interceptorType, ExecutionInjectionMode mode) :
			this(interceptorType, mode, false)
		{
		}

		public InterceptorInfo(Type interceptorType, ExecutionInjectionMode mode, bool eatException)
		{
			Interceptor = interceptorType;
			Mode = mode;
			EatException = eatException;
		}

		public Type Interceptor { get; private set; }
		public ExecutionInjectionMode Mode { get; private set; }
		public bool EatException { get; private set; }
	}

	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
	public sealed class InterceptorAttribute : Attribute
	{
		public Type InterceptorType { get; set; }
		public ExecutionInjectionMode Mode { get; set; }
		public bool EatException { get; set; }
	}

	public class DynamicProxy : DynamicObject
	{
		private readonly object _target;
		private GenericWeakReference<DynamicAttributesMapper> _weakMapper;
		private SafeCollection<InterceptorInfo> _interceptors;
		private Type _targerType;

		public DynamicProxy(object target)
		{
			if (target == null)
				throw new ArgumentNullException("Parameter target cannot be a null.");

			_weakMapper = new GenericWeakReference<DynamicAttributesMapper>(DynamicAttributesMapper.Instance);
			_target = target;
			_targerType = _target.GetType();

			InitMappings();
		}

		protected void InitMappings()
		{
			if (!_weakMapper.Target.Contains(_targerType))
			{
				object[] attributes = _targerType.GetCustomAttributes(typeof(InterceptorAttribute), false);
				if (attributes != null && attributes.Length > 0)
				{
					_interceptors = new SafeCollection<InterceptorInfo>(attributes.Length);

					for (int i = 0; i < attributes.Length; i++)
					{
						InterceptorAttribute attr = (attributes[i] as InterceptorAttribute);
						if (attr != null)
						{
							InterceptorInfo info = new InterceptorInfo(attr.InterceptorType, attr.Mode);
							_interceptors.Add(info);
						}
					}
					_weakMapper.Target.EmptyAndAddRange(_targerType, _interceptors);
				}
			}
			else
				_interceptors = _weakMapper.Target.GetInterceptions(_targerType);
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			var beforeInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.Before);
			Console.WriteLine("before invoking " + binder.Name);

			try
			{
				result = _targerType.InvokeMember(binder.Name, (BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance),
													null, _target, args);
			}
			catch (Exception ex)
			{
				var errorInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.OnError);
				if (errorInterceptors != null)
				{
					foreach (InterceptorInfo interceptor in errorInterceptors)
					{
						IInterceptor real = Activator.CreateInstance(interceptor.Interceptor) as IInterceptor;
						if (real != null)
						{
							real.Intercept(CastToExceptionalInvocation(binder, _target, args, ex));
						}
					}
				}
				throw;
			}

			var afterInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.After);
			Console.WriteLine("after invoking " + binder.Name);
			return true;
		}

		protected IInvocation CastToInvocation(InvokeMemberBinder binder, object _target, object[] args)
		{
			return new DefaultIInvocation(_target, binder, args);
		}

		protected IInvocation CastToExceptionalInvocation(InvokeMemberBinder binder, object _target, object[] args, Exception ex)
		{
			IInvocation invocator = new DefaultIInvocation(_target, binder, args);
			invocator.SetException(ex);
			return invocator;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = null;
			MemberInfo member = _targerType.GetMember(binder.Name).FirstOrDefault();
			switch (member.MemberType)
			{
				case MemberTypes.Field:
					result = ((FieldInfo)member).GetValue(_target);
					break;
				case MemberTypes.Property:
					result = ((PropertyInfo)member).GetValue(_target, null);
					break;
				default:
					throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo", "member");
			}

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			FieldInfo fieldInfo = _targerType.GetField(binder.Name);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(_target, value);
				return true;
			}
			else
			{
				PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
				if (pInfo != null)
				{
					pInfo.SetValue(_target, value, null);
					return true;
				}
			}
			return false;
		}
	}

	public static class DynamicProxyGeneratorDefault
	{
		public static T GetInstanceFor<T>()
		{
			Type typeOfT = typeof(T);
			var methodInfos = typeOfT.GetMethods();
			AssemblyName assName = new AssemblyName("DefaultProxyAssembly");
			assName.Version = new Version(1, 0, 0);
			var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assBuilder.DefineDynamicModule("DefaultProxyModule", "test.dll");
			var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "Proxy", TypeAttributes.Public);

			typeBuilder.AddInterfaceImplementation(typeOfT);
			var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
			var ilGenerator = ctorBuilder.GetILGenerator();
			ilGenerator.EmitWriteLine("Creating Proxy instance");
			ilGenerator.Emit(OpCodes.Ret);
			foreach (var methodInfo in methodInfos)
			{
				var methodBuilder = typeBuilder.DefineMethod(
					methodInfo.Name,
					MethodAttributes.Public | MethodAttributes.Virtual,
					methodInfo.ReturnType,
					methodInfo.GetParameters().Select(p => p.GetType()).ToArray()
					);
				var methodILGen = methodBuilder.GetILGenerator();
				if (methodInfo.ReturnType == typeof(void))
				{
					methodILGen.Emit(OpCodes.Ret);
				}
				else
				{
					if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum)
					{
						MethodInfo getMethod = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type) });
						LocalBuilder lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
						methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
						methodILGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
						methodILGen.Emit(OpCodes.Callvirt, getMethod);
						methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
					}
					else
					{
						methodILGen.Emit(OpCodes.Ldnull);
					}
					methodILGen.Emit(OpCodes.Ret);
				}
				typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
			}

			Type constructedType = typeBuilder.CreateType();
			var instance = Activator.CreateInstance(constructedType);
			return (T)instance;
		}
	}
}