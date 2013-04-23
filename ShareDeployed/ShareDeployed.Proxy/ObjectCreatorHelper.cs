using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ShareDeployed.Common.Proxy
{
	// Delegate for holding object instantiator method
	public delegate object CreateInstanceDelegate();

	public sealed class ObjectCreatorHelper
	{
		private static IDictionary<Type, CreateInstanceDelegate> _createInstanceDelegateList;
		private static object _syncRoot;

		static ObjectCreatorHelper()
		{
			_createInstanceDelegateList = new Dictionary<Type, CreateInstanceDelegate>();
			_syncRoot = new object();
		}

		private ObjectCreatorHelper()
		{
		}

		// Function that creates the method dynamically for creating the instance of a given class type
		public static CreateInstanceDelegate ObjectInstantiater(Type objectType)
		{
			CreateInstanceDelegate createInstanceDelegate;

			if (!_createInstanceDelegateList.TryGetValue(objectType, out createInstanceDelegate))
			{
				// double check to ensure that an instance is not created before this lock
				lock (_syncRoot)
				{
					if (!_createInstanceDelegateList.TryGetValue(objectType, out createInstanceDelegate))
					{
						// Create a new method.
						DynamicMethod dynamicMethod = new DynamicMethod("Create_" + objectType.Name, objectType, new Type[0]);
						// Get the default constructor of the plugin type
						ConstructorInfo ctor = objectType.GetConstructor(new Type[0]);
						// Generate the intermediate language.       
						ILGenerator ilgen = dynamicMethod.GetILGenerator();
						ilgen.Emit(OpCodes.Newobj, ctor);
						ilgen.Emit(OpCodes.Ret);
						// Create new delegate and store it in the dictionary
						createInstanceDelegate = (CreateInstanceDelegate)dynamicMethod.CreateDelegate(typeof(CreateInstanceDelegate));
						_createInstanceDelegateList[objectType] = createInstanceDelegate;
					}
				}
			}
			return createInstanceDelegate; // return the object instantiator delegate
		}

		// Function that creates the method dynamically for creating the instance of a given class type
		public static CreateInstanceDelegate ObjectInstantiater(Type objectType, bool isInterface)
		{
			if (isInterface)
			{
				Type implType = ServicesMapper.GetImplementation(objectType);
				if (implType != null)
				{
					return ObjectInstantiater(implType);
				}
				else
					throw new InvalidOperationException(string.Format("Mapping for abstraction is not registered in the system.{0}{1}", Environment.NewLine, objectType));
			}
			return ObjectInstantiater(objectType);
		}
	}
}
