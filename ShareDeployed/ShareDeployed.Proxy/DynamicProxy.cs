using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Common.Proxy
{
	public class DynamicProxy : DynamicObject
	{
		private readonly object _target;
		private GenericWeakReference<TypeAttributesMapper> _weakMapper;
		private SafeCollection<InterceptorInfo> _interceptors;
		private Type _targerType;

		public DynamicProxy(object target)
		{
			if (target == null)
				throw new ArgumentNullException("Parameter target cannot be a null.");

			_weakMapper = new GenericWeakReference<TypeAttributesMapper>(TypeAttributesMapper.Instance);
			_target = target;
			_targerType = _target.GetType();

			InitMappings();
		}

		#region protected methods
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
		#endregion

		#region DynamicObject overrides
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			result = null;
			var beforeInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.Before);
			Console.WriteLine("before invoking " + binder.Name);

			try
			{
				MethodCallInfo mci = new MethodCallInfo(binder.Name, binder.CallInfo.ArgumentCount, binder.CallInfo.ArgumentNames);
				MethodInfo mi = null;
				if ((mi = TypeMethodMapper.Instance.Get(_targerType, mci)) != null)
					result = mi.Invoke(_target, args);
				else
				{
					mi = _targerType.GetMethod(binder.Name, (BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance));
					if (mi != null)
					{
						TypeMethodMapper.Instance.Add(_targerType, mci, mi);
						result = mi.Invoke(_target, args);
					}
				}
			}
			catch (Exception ex)
			{
				var errorInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.OnError);
				if (errorInterceptors != null)
				{
					foreach (InterceptorInfo interceptor in errorInterceptors)
					{
						//old way
						//IInterceptor real = Activator.CreateInstance(interceptor.Interceptor) as IInterceptor;
						//new way
						var instDel = ObjectCreatorHelper.ObjectInstantiater(interceptor.Interceptor, false);
						IInterceptor real = instDel() as IInterceptor;
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
				if (pInfo != null && pInfo.CanWrite)
				{
					pInfo.SetValue(_target, value, null);
					return true;
				}
			}
			return false;
		}
		#endregion
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