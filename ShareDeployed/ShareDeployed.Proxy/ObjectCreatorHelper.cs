using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Delegate for storing object instantiator method
	/// </summary>
	/// <returns></returns>
	public delegate object CreateInstanceDelegate();

	public sealed class ObjectCreatorHelper
	{
		private static IDictionary<Type, CreateInstanceDelegate> _createInstanceDelegateList;
		private static int _count;
		private static object _syncRoot;

		#region ctors
		static ObjectCreatorHelper()
		{
			_createInstanceDelegateList = new Dictionary<Type, CreateInstanceDelegate>();
			_syncRoot = new object();
			_count = 0;
		}

		private ObjectCreatorHelper()
		{
		}
		#endregion

		// Function that creates the method dynamically for creating the instance of a given class type
		public static CreateInstanceDelegate ObjectInstantiater(Type objectType)
		{
			CreateInstanceDelegate createInstanceDelegate;
			if (!objectType.HasDefaultCtor())
				throw new ConstructorMissingException(objectType);

			if (!_createInstanceDelegateList.TryGetValue(objectType, out createInstanceDelegate))
			{	// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (!_createInstanceDelegateList.TryGetValue(objectType, out createInstanceDelegate))
					{
						// Create a new method.
						DynamicMethod dynamicMethod = new DynamicMethod("Create_" + objectType.Name, objectType, new Type[0]);
						ConstructorInfo ctor = objectType.GetConstructor(Type.EmptyTypes);// Get the default constructor of the plugin type
						ILGenerator ilgen = dynamicMethod.GetILGenerator();// Generate the intermediate language.
						ilgen.Emit(OpCodes.Newobj, ctor);
						ilgen.Emit(OpCodes.Ret);
						// Create new delegate and store it in the dictionary
						createInstanceDelegate = (CreateInstanceDelegate)dynamicMethod.CreateDelegate(typeof(CreateInstanceDelegate));
						_createInstanceDelegateList[objectType] = createInstanceDelegate;
						_count++;
					}
				}
			}
			return createInstanceDelegate; // return the object instantiator delegate
		}

		// Function that creates the method dynamically for creating the instance of a given class type
		public static CreateInstanceDelegate ObjectInstantiater(Type objectType, bool isInterface)
		{
			if (!isInterface)
				return ObjectInstantiater(objectType);

			Type implType = ServicesMapper.GetImplementation(objectType);
			if (implType != null)
				return ObjectInstantiater(implType);
			else
				throw new InvalidOperationException(string.Format("Mapping for abstraction {0} is not registered in the system.", objectType));
		}

		public static void ClearCache()
		{
			lock (_syncRoot)
			{
				if (_count > 0)
					_createInstanceDelegateList.Clear();
			}
		}
	}
}
