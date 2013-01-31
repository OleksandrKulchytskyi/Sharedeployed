using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShareDeployed.Common.Extensions
{
	public class DisposableAction : IDisposable
	{
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Server project use this.")]
		public static DisposableAction Empty = new DisposableAction(() => { });

		private Action _action;

		public DisposableAction(Action action)
		{
			_action = action;
		}


		#region IDisposable Members

		/// <summary>
		/// Internal variable which checks if Dispose has already been called
		/// </summary>
		private Boolean disposed;

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(Boolean disposing)
		{
			if (disposed)
			{
				return;
			}

			if (disposing)
			{
				Interlocked.Exchange(ref _action, () => { }).Invoke();
			}
			disposed = true;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// Call the private Dispose(bool) helper and indicate  that we are explicitly disposing
			this.Dispose(true);

			// Tell the garbage collector that the object doesn't require any cleanup when collected since Dispose was called explicitly.
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
