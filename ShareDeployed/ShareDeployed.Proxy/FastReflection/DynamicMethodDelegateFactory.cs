using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ShareDeployed.Proxy.FastReflection
{
	public delegate object DynamicMethodDelegate(object target, object[] args);

	internal class DynamicMethodDelegateFactory
	{
		/// <summary>
		/// Generates a DynamicMethodDelegate delegate from a MethodInfo object.
		/// </summary>
		public static DynamicMethodDelegate Create(MethodInfo method)
		{
			method.ThrowIfNull("method", "Parameter cannot be a null.");
			ParameterInfo[] parms = method.GetParameters();
			int numparams = parms.Length;

			Type[] argsTypes = { typeof(object), typeof(object[]) };

			// Create dynamic method and obtain its IL generator to inject code.
			DynamicMethod dynMthd = new DynamicMethod(string.Empty, typeof(object), argsTypes, typeof(DynamicMethodDelegateFactory));
			ILGenerator ilGen = dynMthd.GetILGenerator();

			#region IL generation

			#region Argument count check
			// Define a label for succesfull argument count checking.(in IL there are no for, while ,foreach loop
			Label argsOK = ilGen.DefineLabel();

			// Check input argument count.
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Ldlen);
			ilGen.Emit(OpCodes.Ldc_I4, numparams);
			ilGen.Emit(OpCodes.Beq, argsOK);

			// Argument count was wrong, throw TargetParameterCountException.
			ilGen.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes));
			ilGen.Emit(OpCodes.Throw);

			// Mark IL with argsOK label.
			ilGen.MarkLabel(argsOK);
			#endregion

			#region Instance push
			// If method isn't static push target instance on top of stack.
			if (!method.IsStatic)
				// Argument 0 of dynamic method is target instance.
				ilGen.Emit(OpCodes.Ldarg_0);
			#endregion

			#region Standard argument layout
			// Lay out args array onto stack.
			int i = 0;
			while (i < numparams)
			{
				// Push args array reference onto the stack, followed by the current argument index (i). The Ldelem_Ref opcode will resolve them to args[i].
				// Argument 1 of dynamic method is argument array.
				ilGen.Emit(OpCodes.Ldarg_1);
				ilGen.Emit(OpCodes.Ldc_I4, i);
				ilGen.Emit(OpCodes.Ldelem_Ref);

				// If parameter [i] is a value type perform an unboxing.
				Type parmType = parms[i].ParameterType;
				if (parmType.IsValueType)
					ilGen.Emit(OpCodes.Unbox_Any, parmType);
				i++;
			}
			#endregion

			#region Method call
			// Perform actual call.
			// If method is not final a callvirt is required, otherwise a normal call will be emitted.
			if (method.IsFinal)
				ilGen.Emit(OpCodes.Call, method);
			else
				ilGen.Emit(OpCodes.Callvirt, method);

			if (method.ReturnType != typeof(void))
			{	// If result is of value type it needs to be boxed
				if (method.ReturnType.IsValueType)
					ilGen.Emit(OpCodes.Box, method.ReturnType);
			}
			else
			{
				ilGen.Emit(OpCodes.Ldnull);
			}

			// Emit return opcode.
			ilGen.Emit(OpCodes.Ret);
			#endregion
			#endregion

			return (DynamicMethodDelegate)dynMthd.CreateDelegate(typeof(DynamicMethodDelegate));
		}
	}
}
