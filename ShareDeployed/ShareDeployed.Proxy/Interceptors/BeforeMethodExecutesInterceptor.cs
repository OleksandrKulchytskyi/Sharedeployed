﻿using System;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	[GetInstance(TypeOf = typeof(Logging.ILoggerAggregator), Alias = "single")]
	public class BeforeMethodExecutesInterceptor : IInterceptor
	{
		[Instantiate()]
		private Logging.ILoggerAggregator _aggregator;

		public virtual void Intercept(IInvocation invocation)
		{
			invocation.ThrowIfNull("invocation", "Parameter cannot be null.");

			if (_aggregator == null)
				return;

			StringBuilder sb = new StringBuilder();
			sb.Append("Action: before method executes, ");
			sb.Append(string.Format("Method name {0}{1}", invocation.MethodInvocationTarget.Name, Environment.NewLine));

			if (invocation.Arguments != null && invocation.Arguments.Length > 0)
			{
				var parameters = invocation.MethodInvocationTarget.GetParameters();
				for (int i = 0; i < parameters.Length; i++)
				{
					sb.AppendLine(string.Format("Parameter name: {0}, value: {1}", parameters[i].Name, invocation.Arguments[i]));
				}
			}
			else
				sb.AppendLine("Method is parameterless.");

			if (invocation.ReturnValueType != typeof(void))
				sb.AppendLine(string.Format("Return type is {0}", invocation.ReturnValue));

			_aggregator.DoLog(Logging.LogSeverity.Info, sb.ToString(), null);
			sb.Clear();
		}
	}
}
