using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	[GetInstance(TypeOf=typeof(Logging.ILogAggregator),Alias="single")]
	public sealed class MethodInterceptor : IInterceptor
	{
		private readonly Delegate _impl;
		
		[Instantiate]
		public Logging.LogAggregator LogAggregator { get; set; }

		public MethodInterceptor(Delegate @delegate)
		{
			this._impl = @delegate;
		}

		public void Intercept(IInvocation invocation)
		{
			var result = this._impl.DynamicInvoke(invocation.Arguments);
			invocation.ReturnValue = result;
		}
	}
}
