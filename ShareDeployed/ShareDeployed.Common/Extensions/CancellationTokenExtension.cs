using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Extensions
{
	public static class CancellationTokenExtension
	{
		public static IDisposable SafeReqister<T>(this CancellationToken cancelTkn, Action<T> callback, T state)
		{
			int callbackInvoked = 0;

			try
			{
				CancellationTokenRegistration registration = cancelTkn.Register(callbackState =>
					{
						if (Interlocked.Exchange(ref callbackInvoked, 1) == 0)
						{
							callback((T)callbackState);
						}
					}, state, useSynchronizationContext: false);

				return new DisposableAction(() =>
				{
					//this normally waits until the callback is finished invoked but we don't care
					if (Interlocked.Exchange(ref callbackInvoked, 1) == 0)
					{
						registration.DisposeExt();
					}
				});
			}
			catch (ObjectDisposedException)
			{
				if(Interlocked.Exchange(ref callbackInvoked, 1)==0)
				{
					callback(state);
				}
			}
			//Noop
			return new DisposableAction(() => { });
		}

	}
}
