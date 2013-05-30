using System;
using System.Threading;

namespace ShareDeployed.Proxy
{
	public static class LockFreeExtension
	{
		public static void LockFreeUpdate<T>(ref T field, Func<T, T> updateFunction) where T : class
		{
			var spinWait = new SpinWait();
			while (true)
			{
				T snapshot1 = field;
				T calc = updateFunction(snapshot1);
				T snapshot2 = Interlocked.CompareExchange(ref field, calc, snapshot1);
				if (snapshot1 == snapshot2) return;
				spinWait.SpinOnce();
			}
		}
	}
}