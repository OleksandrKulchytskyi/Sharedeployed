using System;
using System.Diagnostics;

namespace ShareDeployed.Common.Proxy
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
			Debug.WriteLine(cuurrentExc.Message);
			if (cuurrentExc.InnerException != null)
			{
				Debug.WriteLine(cuurrentExc.InnerException.Message);
				if (cuurrentExc.InnerException.InnerException != null)
					Debug.WriteLine(cuurrentExc.InnerException.InnerException.Message);
			}

			if (LogAggregator != null)
				LogAggregator.DoLog(Logging.LogSeverity.Error, "ProceesException", cuurrentExc);
		}
	}
}
