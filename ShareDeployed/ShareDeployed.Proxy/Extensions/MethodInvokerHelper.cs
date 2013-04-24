using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	public delegate object MethodInvoker(object target);

	public sealed class MethodInvokerHelper
	{
		private MethodInvokerHelper()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetType">Target type</param>
		/// <param name="methodName">Calling method name</param>
		/// <example>
		/// MethodInvoker invk=MethodInvokerHelper.CreateDelegate(someType,"GetData");
		/// string data=(string)invk.Invoke(someTypeInst);
		/// </example>
		/// <returns></returns>
		public static MethodInvoker CreateDelegate(Type targetType, string methodName)
		{
			DynamicMethod dynMethod = new DynamicMethod("invoke", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
														typeof(object), new Type[0], targetType, true);
			ILGenerator ilGen = dynMethod.GetILGenerator();
			MethodInfo mi = targetType.GetMethod(methodName, BindingFlags.Instance);
			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Castclass, targetType);
			ilGen.Emit(OpCodes.Callvirt, mi);
			if (mi.ReturnType == typeof(void))
				ilGen.Emit(OpCodes.Ldnull);
			else
			{
				if (mi.ReturnType.IsValueType)
					ilGen.Emit(OpCodes.Box, mi.ReturnType);
			}
			ilGen.Emit(OpCodes.Ret);
			return dynMethod.CreateDelegate(typeof(MethodInvoker)) as MethodInvoker;
		}

	}
}
