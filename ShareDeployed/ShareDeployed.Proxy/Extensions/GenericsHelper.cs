using System;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Helper class for generic types and methods.
	/// </summary>
	internal static class GenericsHelper
	{
		/// <summary>
		/// Makes the typeBuilder a generic.
		/// </summary>
		/// <param name="concrete">The concrete.</param>
		/// <param name="typeBuilder">The type builder.</param>
		public static void MakeGenericType(Type baseType, TypeBuilder typeBuilder)
		{
			Type[] genericArguments = baseType.GetGenericArguments();
			string[] genericArgumentNames = GetArgumentNames(genericArguments);
			GenericTypeParameterBuilder[] genericTypeParameterBuilder
				= typeBuilder.DefineGenericParameters(genericArgumentNames);
			typeBuilder.MakeGenericType(genericTypeParameterBuilder);
		}

		/// <summary>
		/// Gets the argument names from an array of generic argument types.
		/// </summary>
		/// <param name="genericArguments">The generic arguments.</param>
		public static string[] GetArgumentNames(Type[] genericArguments)
		{
			string[] genericArgumentNames = new string[genericArguments.Length];
			for (int i = 0; i < genericArguments.Length; i++)
			{
				genericArgumentNames[i] = genericArguments[i].Name;
			}
			return genericArgumentNames;
		}
	}
}