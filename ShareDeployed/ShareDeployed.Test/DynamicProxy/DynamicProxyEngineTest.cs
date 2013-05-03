using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy;
using System;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyEngineTest
	{
		[TestMethod]
		public void InitializeEngineTest()
		{
			IPipeline engine = DynamicProxyPipeline.Instance;
			engine.Initialize();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ComplexInitializeEngineTest()
		{
			IPipeline engine = DynamicProxyPipeline.Instance;
			engine.Initialize();

			ExceptionInterceptor interceptor = engine.ContracResolver.Resolve<ExceptionInterceptor>();

			Assert.IsTrue(TypeWithInjections.Instance.Contains(typeof(ExceptionInterceptor)));

			ExceptionInterceptor interceptor2 = engine.ContracResolver.Resolve<ExceptionInterceptor>();
			//cause to invoke ArgumentNullException by reason of second parameter value is equals to null
			interceptor.Intercept(new MethodInvocation(this, null, null));
		}
	}
}
