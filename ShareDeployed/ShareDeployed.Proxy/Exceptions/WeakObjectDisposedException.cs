namespace ShareDeployed.Proxy
{
	using System;

	[Serializable]
	public sealed class WeakObjectDisposedException : Exception
	{
		public WeakObjectDisposedException()
			: base()
		{
		}

		public WeakObjectDisposedException(string message)
			: base(message)
		{
		}

		public WeakObjectDisposedException(string msg, Exception innerExc)
			: base(msg, innerExc)
		{
		}
	}
}