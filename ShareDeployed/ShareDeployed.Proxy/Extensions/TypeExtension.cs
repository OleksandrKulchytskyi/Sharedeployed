using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Proxy
{
	public static class TypeExtension
	{
		//a thread-safe way to hold default instances created at run-time
		private static ConcurrentDictionary<Type, object> typeDefaults;
		private static ConcurrentDictionary<Type, Func<object>> typeDefaultsExpr;

		static TypeExtension()
		{
			typeDefaults = new ConcurrentDictionary<Type, object>();
			typeDefaultsExpr = new ConcurrentDictionary<Type, Func<object>>();
		}

		public static byte[] SerializeStructToBytes<T>(T structData) where T : struct
		{
			unsafe
			{
				// Allocate memory buffer.
				byte[] buffer = new byte[System.Runtime.InteropServices.Marshal.SizeOf(typeof(T))];
				fixed (byte* ptr = buffer)
				{
					// Serialize value.
					System.Runtime.InteropServices.Marshal.StructureToPtr(structData, new IntPtr(ptr), true);
				}
				return buffer;
			}
		}

		public static T DeserializeBytesToStruct<T>(byte[] buffer) where T : struct
		{
			unsafe
			{
				T data;
				fixed (byte* dataPtr = buffer)
				{	//deserialize bytes to struct
					data = (T)System.Runtime.InteropServices.Marshal.PtrToStructure(new IntPtr(dataPtr), typeof(T));
				}
				return data;
			}
		}

		public static unsafe TStruct BytesToStructure<TStruct>(this byte[] data) where TStruct : struct
		{
			fixed (byte* dataPtr = data)
				return (TStruct)System.Runtime.InteropServices.Marshal.PtrToStructure(new IntPtr(dataPtr), typeof(TStruct));
		}

		public static unsafe byte[] StructureToBytes<TStruct>(TStruct st) where TStruct : struct
		{
			var bytes = new byte[System.Runtime.InteropServices.Marshal.SizeOf(st)];
			fixed (byte* ptr = bytes) System.Runtime.InteropServices.Marshal.StructureToPtr(st, new IntPtr(ptr), true);
			return bytes;
		}

		public static Type GetTypeEx(string fullTypeName)
		{
			return Type.GetType(fullTypeName, false) ??
				   AppDomain.CurrentDomain.GetAssemblies().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount - 1)
							.Select(a => a.GetType(fullTypeName))
							.FirstOrDefault(t => t != null);
		}

		public static object GetDefaultValue(this Type type)
		{
			return type.IsValueType ? typeDefaults.GetOrAdd(type, t => Activator.CreateInstance(t)) : null;
		}

		public static object GetDefaultValueExp(this Type type)
		{
			// Validate parameters.
			if (type == null) throw new ArgumentNullException("type");

			Func<object> result;
			if (typeDefaultsExpr.TryGetValue(type, out result))
				return result();

			// We want an Func<object> which returns the default. Create that expression here.
			Expression<Func<object>> e = Expression.Lambda<Func<object>>(
				// Have to convert to object.
				Expression.Convert(// The default value, always get what the *code* tells us.
					Expression.Default(type), typeof(object))
			);
			// Compile and return the value.
			Func<object> func = e.Compile();
			if (typeDefaultsExpr.TryAdd(type, func))
				return func;
			else
				throw new InvalidOperationException(string.Format("Fail to add default vaue for type {0}", type));
		}

		public static bool IsDefault<T>(T value) where T : struct
		{
			return value.Equals(default(T));
		}

		public static bool IsNullableType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");

			Type generic = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			if (generic != null && generic.Equals(typeof(Nullable<>))) return true;

			if (type.IsClass) return true;
			if (type.IsValueType) return false;
			return false;
		}

		public static bool IsNullableType<T>()
		{
			return IsNullableType(typeof(T));
		}

		static Delegate CreateConverterDelegate(Type sourceType, Type targetType)
		{
			var input = Expression.Parameter(sourceType, "input");
			Expression body;
			try
			{
				body = Expression.Convert(input, targetType);
			}
			catch (InvalidOperationException)
			{
				var conversionType = Expression.Constant(targetType);
				body = Expression.Call(typeof(Convert), "ChangeType", null, input, conversionType);
			}
			var result = Expression.Lambda(body, input);
			return result.Compile();
		}

		public static object ConvertTo(object obj, Type targetType)
		{
			if (targetType == null) throw new ArgumentNullException("targetType", "Target type cannot be null.");

			if (obj == null)
			{
				if (IsNullableType(targetType)) return null;
				throw new InvalidOperationException(string.Format("Target type '{0}' does not accept null sources.", targetType.Name));
			}
			Delegate converter = CreateConverterDelegate(obj.GetType(), targetType);
			return converter.DynamicInvoke(obj);
		}

		public static T ConvertTo<T>(object obj)
		{
			return (T)ConvertTo(obj, typeof(T));
		}

		public static bool IsCloneableType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");
			if (type.GetInterface("ICloneable") != null) return true;

			MethodInfo method = type.GetMethod("Clone", Type.EmptyTypes);
			if (method == null) return false;
			if (method.ReturnType == typeof(void)) return false;
			return true;
		}
		//private object GetTypedNull(Type type)
		//{
		//	Delegate func;
		//	if (!lambdasMap.TryGetValue(type, out func))
		//	{
		//		var body = Expression.Default(type);
		//		var lambda = Expression.Lambda(body);
		//		func = lambda.Compile();
		//		lambdasMap[type] = func;
		//	}
		//	return func.DynamicInvoke();
		//}
	}
}
