using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	public sealed class ExceptionInterceptor : IInterceptor
	{
		private IInvocation _invocation;

		public void Intercept(IInvocation invocation)
		{
			_invocation = invocation;
			ProceedSteps();
		}

		private void ProceedSteps()
		{
			Exception cuurrentExc = _invocation.Exception;
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
