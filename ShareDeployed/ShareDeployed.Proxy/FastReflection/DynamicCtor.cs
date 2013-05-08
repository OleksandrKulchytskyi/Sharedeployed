using System.Collections.Generic;
using System.Reflection;

namespace ShareDeployed.Proxy.FastReflection
{
	/// <summary>
	/// Represents a constructor
	/// </summary>
	/// <param name="args">arguments to be passed to the method</param>
	/// <returns>the new object instance</returns>
	public delegate object ConstructorDelegate(params object[] args);

	/// <summary>
	/// Defines constructors that dynamic constructor class has to implement.
	/// </summary>
	public interface IDynamicConstructor
	{
		int ParametersCount { get; }
		object Invoke(object[] arguments);
	}

	public class SafeConstructor : IDynamicConstructor
	{
		private ConstructorInfo constructorInfo;

		#region Generated Function Cache

		private static readonly IDictionary<ConstructorInfo, ConstructorDelegate> constructorCache = new Dictionary<ConstructorInfo, ConstructorDelegate>();

		/// <summary>
		/// Obtains cached constructor info or creates a new entry, if none is found.
		/// </summary>
		private static ConstructorDelegate GetOrCreateDynamicConstructor(ConstructorInfo constructorInfo)
		{
			ConstructorDelegate method;
			if (!constructorCache.TryGetValue(constructorInfo, out method))
			{
				method = ILManager.CreateConstructor(constructorInfo);
				lock (constructorCache)
				{
					constructorCache[constructorInfo] = method;
				}
			}
			return method;
		}

		#endregion Generated Function Cache

		private ConstructorDelegate constructor;

		/// <summary>
		/// Creates a new instance of the safe constructor wrapper.
		/// </summary>
		/// <param name="constructorInfo">Constructor to wrap.</param>
		public SafeConstructor(ConstructorInfo constructorInfo)
		{
			this.constructorInfo = constructorInfo;
			this.constructor = GetOrCreateDynamicConstructor(constructorInfo);
		}

		/// <summary>
		/// Invokes dynamic constructor.
		/// </summary>
		/// <param name="arguments">
		/// Constructor arguments.
		/// </param>
		/// <returns>
		/// A constructor value.
		/// </returns>
		public object Invoke(object[] arguments)
		{
			return constructor(arguments);
		}

		int paramCount = -1;
		public int ParametersCount
		{
			get
			{
				if(paramCount==-1)
				{
					paramCount = constructorInfo.GetParameters().Length;
				}
				return paramCount;
			}
		}
	}

	/// <summary>
	/// Factory class for dynamic constructors.
	/// </summary>
	/// <author>Aleksandar Seovic</author>
	public class DynamicConstructor
	{
		/// <summary>
		/// Creates dynamic constructor instance for the specified <see cref="ConstructorInfo"/>.
		/// </summary>
		/// <param name="constructorInfo">Constructor info to create dynamic constructor for.</param>
		/// <returns>Dynamic constructor for the specified <see cref="ConstructorInfo"/>.</returns>
		public static IDynamicConstructor Create(ConstructorInfo constructorInfo)
		{
			constructorInfo.ThrowIfNull("constructorInfo", "You cannot create a dynamic constructor for a null value.");
			return new SafeConstructor(constructorInfo);
		}
	}
}