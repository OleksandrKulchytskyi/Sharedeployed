using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy
{
	internal sealed class DynamicBuilder : DynamicObject
	{
		private readonly Dictionary<string, object> members = new Dictionary<string, object>();

		#region DynamicObject Overrides
		/// <summary>
		/// Assigns the given value to the specified member, overwriting any previous definition if one existed.
		/// </summary>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			members[binder.Name] = value;
			return true;
		}

		/// <summary>
		/// Gets the value of the specified member.
		/// </summary>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (members.ContainsKey(binder.Name))
			{
				result = members[binder.Name];
				return true;
			}
			return base.TryGetMember(binder, out result);
		}

		/// <summary>
		/// Invokes the specified member (if it is a delegate).
		/// </summary>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			object member;
			if (members.TryGetValue(binder.Name, out member))
			{
				var method = member as Delegate;
				if (method != null)
				{
					result = method.DynamicInvoke(args);
					return true;
				}
			}
			return base.TryInvokeMember(binder, args, out result);
		}

		/// <summary>
		/// Gets a list of all dynamically defined members.
		/// </summary>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return members.Keys;
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