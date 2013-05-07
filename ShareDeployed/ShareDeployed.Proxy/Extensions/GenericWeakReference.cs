using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ShareDeployed.Proxy
{
	public class GenericWeakReference<T> : IDisposable where T : class
	{
		private bool wasDisposed = false;
		private GCHandle gcHandle;
		private bool trackResurrection;

		public GenericWeakReference(T target)
			: this(target, false)
		{
		}

		public GenericWeakReference(T target, bool trackResurrection)
		{
			this.trackResurrection = trackResurrection;
			this.Target = target;
		}

		~GenericWeakReference()
		{
			Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (wasDisposed)
				return;

			wasDisposed = true;

			if (disposing)
			{
				if (Target != null && (Target is IDisposable))
					((IDisposable)Target).Dispose();
				gcHandle.Free();
			}
		}

		public virtual bool IsAlive
		{
			get { return (gcHandle.Target != null); }
		}

		public virtual bool TrackResurrection
		{
			get { return this.trackResurrection; }
		}

		public virtual T Target
		{
			get
			{
				object obj = gcHandle.Target;
				if ((obj == null) || (!(obj is T)))
					return default(T);
				else
					return (obj as T);
			}
			set
			{
				gcHandle = GCHandle.Alloc(value, this.trackResurrection
										? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
			}
		}
	}
}
