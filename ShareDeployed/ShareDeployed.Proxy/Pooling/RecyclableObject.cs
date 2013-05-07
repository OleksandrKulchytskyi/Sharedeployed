using System;

namespace ShareDeployed.Proxy.Pooling
{
	/// <summary>
	/// Represents an object that implements IRecyclable contract, allowing the object instance to be reused.
	/// </summary>
	public abstract class RecyclableObject : IRecyclable, IDisposable
	{
		#region IRecyclable Members

		private ReleaseInstanceDelegate Release = null;

		/// <summary>
		/// A fild that contains the value representing whether the object is acquired or not.
		/// </summary>
		protected bool ObjectAcquired = false;

		/// <summary>
		/// Recycles (resets) the object to the original state.
		/// </summary>
		public abstract void Recycle();

		/// <summary>
		/// Binds an <see cref="ReleaseInstanceDelegate"/> delegate which releases the <see cref="IRecyclable"/> object
		/// instance back to the pool.
		/// </summary>
		/// <param name="releaser">The <see cref="ReleaseInstanceDelegate"/> delegate to bind.</param>
		public void Bind(ReleaseInstanceDelegate releaser)
		{
			this.Release = releaser;
		}

		/// <summary>
		/// Invoked when a pool acquires the instance.
		/// </summary>
		public void OnAcquire()
		{
			// Flag this as acquired
			this.ObjectAcquired = true;
		}

		/// <summary>
		/// Gets whether this <see cref="RecyclableObject"/> object is pooled or not.
		/// </summary>
		public bool IsPooled
		{
			get { return Release != null; }
		}

		#endregion IRecyclable Members

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// Object is still registered for finalization
			if (Release != null && this.ObjectAcquired)
			{
				// Release back to the pool.
				this.ObjectAcquired = false;
				this.Release(this);
				GC.SuppressFinalize(this);
				GC.ReRegisterForFinalize(this);
			}
			else
			{
				// Otherwise, the object is actually going to die.
				Dispose(true);

				// No need to finalize it
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Attempts to release this instance back to the pool. If the instance is not pooled, nothing will be done.
		/// </summary>
		public void TryRelease()
		{
			// Release back to the pool.
			if (Release != null && this.ObjectAcquired)
			{
				this.ObjectAcquired = false;
				this.Release(this);
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by the object and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">
		/// If set to true, release both managed and unmanaged resources, othewise release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
		}

		/// <summary>
		/// Finalizer for the recyclable object.
		/// </summary>
		~RecyclableObject()
		{
			//Console.WriteLine("Finalize()");
			if (Release != null && this.ObjectAcquired)
			{
				// Release back to the pool and register back to the finalizer thread.
				this.ObjectAcquired = false;
				this.Release(this);
				GC.ReRegisterForFinalize(this);
			}
			else
			{
				// Otherwise, the object is actually going to die.
				Dispose(false);
			}
		}

		#endregion IDisposable Members
	}
}