using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Proxy
{
	public static class DynamicAssemblyBuilder
	{
		public static AssemblyBuilder Create(string assemblyName)
		{
			AssemblyName name = new AssemblyName(assemblyName);
			AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
			DynamicAssemblyCache.Add(assembly);
			return assembly;
		}

		/// Creates an assembly builder and saves the assembly to the passed in location.
		/// </summary>
		/// <param name="assemblyName">Name of the assembly.</param>
		/// <param name="filePath">The file path.</param>
		public static AssemblyBuilder Create(string assemblyName, string filePath)
		{
			AssemblyName name = new AssemblyName(assemblyName);
			AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave, filePath);
			DynamicAssemblyCache.Add(assembly);
			return assembly;
		}
	}

	/// <summary>
	/// Cache for storing the dynamic assembly builder.
	/// </summary>
	internal static class DynamicAssemblyCache
	{
		private static object syncRoot = new object();
		internal static AssemblyBuilder Cache = null;

		#region Adds a dynamic assembly to the cache.

		/// <summary>
		/// Adds a dynamic assembly builder to the cache.
		/// </summary>
		/// <param name="assemblyBuilder">The assembly builder.</param>
		public static void Add(AssemblyBuilder assemblyBuilder)
		{
			lock (syncRoot)
			{
				Cache = assemblyBuilder;
			}
		}

		#endregion Adds a dynamic assembly to the cache.

		#region Gets the cached assembly

		/// <summary>
		/// Gets the cached assembly builder.
		/// </summary>
		/// <returns></returns>
		public static AssemblyBuilder Get
		{
			get
			{
				lock (syncRoot)
				{
					if (Cache != null)
						return Cache;
				}

				throw new InvalidOperationException("There is no assembly in cache.");
			}
		}

		#endregion Gets the cached assembly
	}

	/// <summary>
	/// Class for creating a module builder.
	/// </summary>
	internal static class DynamicModuleBuilder
	{
		/// <summary>
		/// Creates a module builder using the cached assembly.
		/// </summary>
		public static ModuleBuilder Create()
		{
			string assemblyName = DynamicAssemblyCache.Get.GetName().Name;

			ModuleBuilder moduleBuilder = DynamicAssemblyCache.Get.DefineDynamicModule(assemblyName, string.Format("{0}.dll", assemblyName));

			DynamicModuleCache.Add(moduleBuilder);

			return moduleBuilder;
		}
	}

	/// <summary>
	/// Class for storing the module builder.
	/// </summary>
	internal static class DynamicModuleCache
	{
		private static object syncRoot = new object();
		internal static ModuleBuilder Cache = null;

		#region Add

		/// <summary>
		/// Adds a dynamic module builder to the cache.
		/// </summary>
		/// <param name="moduleBuilder">The module builder.</param>
		public static void Add(ModuleBuilder moduleBuilder)
		{
			lock (syncRoot)
			{
				Cache = moduleBuilder;
			}
		}

		#endregion Add

		#region Get

		/// <summary>
		/// Gets the cached module builder.
		/// </summary>
		/// <returns></returns>
		public static ModuleBuilder Get
		{
			get
			{
				lock (syncRoot)
				{
					if (Cache != null)
						return Cache;
				}

				throw new InvalidOperationException("There is no module in cache");
			}
		}

		#endregion Get
	}

	/// <summary>
	/// Cache for storing proxy types.
	/// </summary>
	internal static class DynamicTypeCache
	{
		private static object syncRoot = new object();
		public static Dictionary<string, Type> Cache = new Dictionary<string, Type>();

		/// <summary>
		/// Adds a proxy to the type cache.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="proxy">The proxy.</param>
		public static void AddProxyForType(Type type, Type proxy)
		{
			lock (syncRoot)
			{
				Cache.Add(GetHashCode(type.AssemblyQualifiedName), proxy);
			}
		}

		/// <summary>
		/// Tries the type of the get proxy for.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public static Type TryGetProxyForType(Type type)
		{
			lock (syncRoot)
			{
				Type proxyType;
				Cache.TryGetValue(GetHashCode(type.AssemblyQualifiedName), out proxyType);
				return proxyType;
			}
		}

		#region Private Methods

		private static string GetHashCode(string fullName)
		{
			using (SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider())
			{
				Byte[] buffer = Encoding.UTF8.GetBytes(fullName);
				Byte[] hash = provider.ComputeHash(buffer, 0, buffer.Length);
				return Convert.ToBase64String(hash);
			}
		}

		#endregion Private Methods
	}

	/// <summary>
	/// Class for creating the proxy constructors.
	/// </summary>
	internal static class DynamicConstructorBuilder
	{
		/// <summary>
		/// Builds the constructors.
		/// </summary>
		/// <typeparam name="TBase">The base type.</typeparam>
		/// <param name="typeBuilder">The type builder.</param>
		/// <param name="interceptorsField">The interceptors field.</param>
		public static void BuildConstructors<TBase>(TypeBuilder typeBuilder, FieldBuilder interceptorsField,
			MethodInfo addInterceptor) where TBase : class
		{
			ConstructorInfo interceptorsFieldConstructor = CreateInterceptorsFieldConstructor<TBase>();
			ConstructorInfo defaultInterceptorConstructor = CreateDefaultInterceptorConstructor<TBase>();
			ConstructorInfo[] constructors = typeof(TBase).GetConstructors();
			foreach (ConstructorInfo constructorInfo in constructors)
			{
				CreateConstructor<TBase>(typeBuilder,
						interceptorsField,
						interceptorsFieldConstructor,
						defaultInterceptorConstructor,
						addInterceptor,
						constructorInfo
					);
			}
		}

		#region Private Methods

		private static void CreateConstructor<TBase>(TypeBuilder typeBuilder,
				FieldBuilder interceptorsField,
				ConstructorInfo interceptorsFieldConstructor,
				ConstructorInfo defaultInterceptorConstructor,
				MethodInfo AddDefaultInterceptor,
				ConstructorInfo constructorInfo) where TBase : class
		{
			Type[] parameterTypes = GetParameterTypes(constructorInfo);
			ConstructorBuilder constructorBuilder = CreateConstructorBuilder(typeBuilder, parameterTypes);
			ILGenerator cIL = constructorBuilder.GetILGenerator();
			LocalBuilder defaultInterceptorMethodVariable =
				cIL.DeclareLocal(typeof(DefaultInterceptor<>).MakeGenericType(typeof(TBase)));
			ConstructInterceptorsField(interceptorsField, interceptorsFieldConstructor, cIL);
			ConstructDefaultInterceptor(defaultInterceptorConstructor, cIL, defaultInterceptorMethodVariable);
			AddDefaultInterceptorToInterceptorsList
				(
					interceptorsField,
					AddDefaultInterceptor,
					cIL,
					defaultInterceptorMethodVariable
				);
			CreateConstructor(constructorInfo, parameterTypes, cIL);
		}

		private static void CreateConstructor(ConstructorInfo constructorInfo, Type[] parameterTypes, ILGenerator cIL)
		{
			cIL.Emit(OpCodes.Ldarg_0);
			if (parameterTypes.Length > 0)
			{
				LoadParameterTypes(parameterTypes, cIL);
			}
			cIL.Emit(OpCodes.Call, constructorInfo);
			cIL.Emit(OpCodes.Ret);
		}

		private static void LoadParameterTypes(Type[] parameterTypes, ILGenerator cIL)
		{
			for (int i = 1; i <= parameterTypes.Length; i++)
			{
				cIL.Emit(OpCodes.Ldarg_S, i);
			}
		}

		private static void AddDefaultInterceptorToInterceptorsList(FieldBuilder interceptorsField,
				MethodInfo AddDefaultInterceptor, ILGenerator cIL,
				LocalBuilder defaultInterceptorMethodVariable)
		{
			cIL.Emit(OpCodes.Ldarg_0);
			cIL.Emit(OpCodes.Ldfld, interceptorsField);
			cIL.Emit(OpCodes.Ldloc, defaultInterceptorMethodVariable);
			cIL.Emit(OpCodes.Callvirt, AddDefaultInterceptor);
		}

		private static void ConstructDefaultInterceptor(ConstructorInfo defaultInterceptorConstructor,
				ILGenerator cIL, LocalBuilder defaultInterceptorMethodVariable)
		{
			cIL.Emit(OpCodes.Newobj, defaultInterceptorConstructor);
			cIL.Emit(OpCodes.Stloc, defaultInterceptorMethodVariable);
		}

		private static void ConstructInterceptorsField(FieldBuilder interceptorsField, ConstructorInfo interceptorsFieldConstructor,
				ILGenerator cIL)
		{
			cIL.Emit(OpCodes.Ldarg_0);
			cIL.Emit(OpCodes.Newobj, interceptorsFieldConstructor);
			cIL.Emit(OpCodes.Stfld, interceptorsField);
		}

		private static ConstructorBuilder CreateConstructorBuilder(TypeBuilder typeBuilder, Type[] parameterTypes)
		{
			return typeBuilder.DefineConstructor
				(
					MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName
					| MethodAttributes.HideBySig, CallingConventions.Standard, parameterTypes
				);
		}

		private static Type[] GetParameterTypes(ConstructorInfo constructorInfo)
		{
			ParameterInfo[] parameterInfoArray = constructorInfo.GetParameters();
			Type[] parameterTypes = new Type[parameterInfoArray.Length];
			for (int p = 0; p < parameterInfoArray.Length; p++)
			{
				parameterTypes[p] = parameterInfoArray[p].ParameterType;
			}
			return parameterTypes;
		}

		private static ConstructorInfo CreateInterceptorsFieldConstructor<TBase>() where TBase : class
		{
			//return ConstructorHelper.CreateGenericConstructorInfo
			//	(
			//		typeof(List<>),
			//		new Type[] { typeof(IInterceptor<TBase>) },
			//		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			//	);
			return null;
		}

		private static ConstructorInfo CreateDefaultInterceptorConstructor<TBase>() where TBase : class
		{
			//return ConstructorHelper.CreateGenericConstructorInfo
			//	(
			//		typeof(DefaultInterceptor<>),
			//		new Type[] { typeof(TBase) },
			//		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			//	);
			return null;
		}

		#endregion Private Methods
	}
}