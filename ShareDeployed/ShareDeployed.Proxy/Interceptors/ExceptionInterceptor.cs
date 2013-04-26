using System;
using System.Diagnostics;

namespace ShareDeployed.Common.Proxy
{
	public class ExceptionInterceptor : IInterceptor
	{
		private Logging.ILoggerProvider _logger;

		public virtual void Intercept(IInvocation invocation)
		{
			invocation.ThrowIfNull("invocation", "Parameter cannot be null.");
			ProceesException(invocation);
		}

		protected void ProceesException(IInvocation invocation)
		{
			Exception cuurrentExc = invocation.Exception;
			Debug.WriteLine(cuurrentExc.Message);
			if (cuurrentExc.InnerException != null)
			{
				Debug.WriteLine(cuurrentExc.InnerException.Message);
				if (cuurrentExc.InnerException.InnerException != null)
					Debug.WriteLine(cuurrentExc.InnerException.InnerException.Message);
			}
		}
	}
}
