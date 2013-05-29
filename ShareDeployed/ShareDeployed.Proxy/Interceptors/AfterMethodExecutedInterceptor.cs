using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	[GetInstance(TypeOf = typeof(Logging.ILogAggregator), Alias = "single")]
	public class AfterMethodExecutedInterceptor : IInterceptor
	{
		[Instantiate()]
		public Logging.ILogAggregator LogAggregator
		{
			get;
			set;
		}

		public virtual void Intercept(IInvocation invocation)
		{
			LogAggregator.DoLog(Logging.LogSeverity.Info, "After invoke " + invocation.MethodInvocationTarget.ToString(), null);
		}
	}
}
