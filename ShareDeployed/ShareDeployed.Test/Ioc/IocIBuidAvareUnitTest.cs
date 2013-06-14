using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class IocIBuidAvareUnitTest
	{
		[TestInitialize]
		public void Init()
		{
			DynamicProxyPipeline.Instance.Initialize(true);
		}

		[TestMethod]
		public void UnregisterTestMethod()
		{
			typeof(TypeForResolving).BindToSelfWithAliasInScope("1", ServiceLifetime.Singleton);

			TypeForResolving data = DynamicProxyPipeline.Instance.ContracResolver.Resolve("1") as TypeForResolving;
			if (data != null)
			{
				Assert.IsTrue(object.ReferenceEquals(data, DynamicProxyPipeline.Instance.ContracResolver.Resolve("1")));
				Assert.IsTrue(data.invoked);
			}

			DynamicProxyPipeline.Instance.ContracResolver.Unregister<TypeForResolving>();
		}
	}

	public class TypeForResolving : IBuildAware
	{
		public bool invoked = false;

		public void OnBuilt()
		{
			invoked = true;
		}
	}
}
