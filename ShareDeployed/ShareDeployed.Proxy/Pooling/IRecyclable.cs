using System;

namespace ShareDeployed.Common.Proxy.Pooling
{
	public delegate void ReleaseInstanceDelegate(IRecyclable @object);

	/// <summary>
	/// Defines the the IRecyclable contract, allowing the object instance to be reused.
	/// </summary>
	public interface IRecyclable : IDisposable
	{
		/// <summary>
		/// Recycles (resets) the object to the original state.
		/// </summary>
		void Recycle();

		/// <summary>
		/// Binds an <see cref="ReleaseInstanceDelegate"/> which releases the <see cref="IRecyclable"/> object
		/// instance back to the pool.
		/// </summary>
		/// <param name="releaser">The <see cref="ReleaseInstanceDelegate"/> delegate to bind.</param>
		void Bind(ReleaseInstanceDelegate releaser);

		/// <summary>
		/// Invoked when <see cref="IRecyclable"/> object is about to be acquired.
		/// </summary>
		void OnAcquire();
	}
}