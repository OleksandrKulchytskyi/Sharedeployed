using System;
namespace ShareDeployed.Common.Helper
{
	[Serializable]
	public class LazyWeakReference<T> where T : class
	{
		private WeakReference reference;
		private Func<T> constructor = null;
		private int reinitializingCounter = 0;

		public LazyWeakReference(T anObject, Func<T> aConstructor = null)
		{
			reference = new WeakReference(anObject);
			constructor = aConstructor;
		}

		public int Reinitialized { get { return reinitializingCounter; } }

		public T Target
		{
			get
			{
				object target = reference.Target;
				if (target == null)
				{
					if (constructor == null)
						return null;
					T newObject = constructor();
					reinitializingCounter++;
					reference = new WeakReference(newObject);
					return newObject;
				}
				return (target as T);
			}
		}
	}
}