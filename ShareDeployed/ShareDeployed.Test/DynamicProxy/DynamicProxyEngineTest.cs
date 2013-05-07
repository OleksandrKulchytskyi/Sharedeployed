using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;
using System;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyEngineTest
	{
		public IPipeline  Pipeline { get; set; }

		[TestInitialize]
		public void OnInit()
		{
			Pipeline = DynamicProxyPipeline.Instance;
		}
		
		[TestMethod]
		public void InitializeEngineTest()
		{
			Pipeline.Initialize(true);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ComplexInitializeEngineTest()
		{
			Pipeline.Initialize();

			ExceptionInterceptor interceptor = Pipeline.ContracResolver.Resolve<ExceptionInterceptor>();

			Assert.IsTrue(TypeWithInjections.Instance.Contains(typeof(ExceptionInterceptor)));

			ExceptionInterceptor interceptor2 = Pipeline.ContracResolver.Resolve<ExceptionInterceptor>();
			//cause to invoke ArgumentNullException by reason of second parameter value is equals to null
			interceptor.Intercept(new MethodInvocation(this, null, null));
		}
	}
}
