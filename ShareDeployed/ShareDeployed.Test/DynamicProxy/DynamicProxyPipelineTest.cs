using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;
using System;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyPipelineTest
	{
		public IPipeline Pipeline { get; set; }

		[TestInitialize]
		public void OnInit()
		{
			Pipeline = DynamicProxyPipeline.Instance;
		}

		[TestMethod]
		public void InitializeEngineTest()
		{
			Pipeline.Initialize(true);
			(Pipeline as IConfigurable).Configure();
		}

		[TestMethod]
		public void InitializeEngineWithConfigWithResolveTest()
		{
			Pipeline.Initialize(true);
			(Pipeline as IConfigurable).Configure();
			object data = Pipeline.ContracResolver.Resolve("parameters");
			Assert.IsNotNull(data);
			Assert.IsInstanceOfType(data, typeof(Ioc.ClassWithParameters));

			var data2 = Pipeline.ContracResolver.Resolve<ShareDeployed.Test.Ioc.ClassWithParameters>();
			Assert.IsNotNull(data2);
			Assert.IsFalse(data2.IsLogAggNull());
			Assert.IsFalse(object.ReferenceEquals(data, data2));
		}

		[TestMethod]
		public void NullWhitoutConfigResolveByAliasTest()
		{
			Pipeline.Initialize(true);
			object data = Pipeline.ContracResolver.Resolve("parameters");
			Assert.IsNull(data);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void NullWhitoutConfigResolveByTypeTest()
		{
			Pipeline.Initialize(true);
			Pipeline.ContracResolver.Resolve<ShareDeployed.Test.Ioc.ClassWithParameters>();
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
