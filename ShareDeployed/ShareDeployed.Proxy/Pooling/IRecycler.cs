namespace ShareDeployed.Common.Proxy.Pooling
{
	/// <summary>
	/// Defines a contract for an object pool.
	/// </summary>
	public interface IRecycler
	{
		/// <summary>
		/// Acquires an instance of a recyclable object
		/// </summary>
		/// <returns></returns>
		IRecyclable Acquire();

		/// <summary>
		/// Releases an instance of a recyclable object back to the pool.
		/// </summary>
		/// <param name="instance">The instance of IRecyclable to release.</param>
		void Release(IRecyclable instance);

		/// <summary>
		/// Gets the overall number of elements managed by this pool.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets the number of available elements currently contained in the pool.
		/// </summary>
		int AvailableCount { get; }

		/// <summary>
		/// Gets the number of elements currently in use and not available in this pool.
		/// </summary>
		int InUseCount { get; }
	}

	/// <summary>
	/// Defines a contract for an object pool.
	/// </summary>
	public interface IRecycler<T> : IRecycler
		where T : class, IRecyclable
	{
		/// <summary>
		/// Acquires an instance of a recyclable object
		/// </summary>
		/// <returns></returns>
		new T Acquire();

		/// <summary>
		/// Releases an instance of a recyclable object back to the pool.
		/// </summary>
		/// <param name="instance">The instance of IRecyclable to release.</param>
		void Release(T instance);
	}
}