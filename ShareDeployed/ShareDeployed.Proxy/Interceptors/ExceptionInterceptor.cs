using System;
using System.Diagnostics;

namespace ShareDeployed.Proxy
{
	[GetInstance(TypeOf = typeof(Logging.ILogAggregator), Alias = "single")]
	public class ExceptionInterceptor : IInterceptor
	{
		[Instantiate()]
		public Logging.ILogAggregator LogAggregator
		{
			get;
			set;
		}

		public virtual void Intercept(IInvocation invocation)
		{
			invocation.ThrowIfNull("invocation", "Parameter cannot be null.");
			ProceesException(invocation);
		}

		protected void ProceesException(IInvocation invocation)
		{
			Exception cuurrentExc = invocation.Exception;
			if (cuurrentExc == null) return;
#if DEBUG
			Debug.WriteLine(cuurrentExc.Message);
#endif
			if (cuurrentExc.InnerException != null)
			{
#if DEBUG
				Debug.WriteLine(cuurrentExc.InnerException.Message);
#endif
				if (cuurrentExc.InnerException.InnerException != null)
#if DEBUG
					Debug.WriteLine(cuurrentExc.InnerException.InnerException.Message);
#endif
			}

			if (LogAggregator != null)
				LogAggregator.DoLog(Logging.LogSeverity.Error, "ProceesException", cuurrentExc);
		}
	}
}
