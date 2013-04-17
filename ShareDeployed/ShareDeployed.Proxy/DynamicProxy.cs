using System;
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

	public interface IInvokeable
	{
	}

	public sealed class DynamicProxy : DynamicObject
	{
		private readonly object _target;

		private GenericWeakReference<DynamicProxyMapper> _weakMapper;

		private InterceptionsInfo _intercInfo;

		public DynamicProxy(object target)
		{
			if (target == null)
				throw new ArgumentNullException("Parameter target cannot be a null.");

			_weakMapper = new GenericWeakReference<DynamicProxyMapper>(DynamicProxyMapper.Instance);
			_target = target;

			InitMappings();
		}

		private void InitMappings()
		{
			Type targetType = _target.GetType();
			if (!_weakMapper.Target.Contains(targetType))
			{
				object[] attributes = targetType.GetCustomAttributes(false);
				if (attributes != null && attributes.Length > 0)
				{
					var items = attributes.Where(x => x.GetType().IsAssignableFrom(typeof(IInvokeable))).Select(x => x.GetType()).ToArray();
					int len = items.Length;
					_intercInfo = new InterceptionsInfo(new Type[len]);

					for (int i = 0; i < len; i++)
					{
						_intercInfo.Interceptors[i] = items[i];
					}

					_weakMapper.Target.Add(targetType, _intercInfo);
				}
			}

		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			Console.WriteLine("before invoking " + binder.Name);

			result = _target.GetType().InvokeMember(binder.Name, BindingFlags.InvokeMethod, null, _target, args);

			Console.WriteLine("after invoking " + binder.Name);
			return true;
		}
	}

	public static class DynamicProxyGeneratorDefault
	{
		public static T GetInstanceFor<T>()
		{
			Type typeOfT = typeof(T);
			var methodInfos = typeOfT.GetMethods();
			AssemblyName assName = new AssemblyName("testAssembly");
			var assBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assBuilder.DefineDynamicModule("testModule", "test.dll");
			var typeBuilder = moduleBuilder.DefineType(typeOfT.Name + "Proxy", TypeAttributes.Public);

			typeBuilder.AddInterfaceImplementation(typeOfT);
			var ctorBuilder = typeBuilder.DefineConstructor(
					  MethodAttributes.Public,
					  CallingConventions.Standard,
					  new Type[] { });
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