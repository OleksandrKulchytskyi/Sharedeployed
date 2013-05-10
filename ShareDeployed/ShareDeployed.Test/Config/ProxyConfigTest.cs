using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ShareDeployed.Proxy.Config;

namespace ShareDeployed.Test
{
	[TestClass]
	public class ProxyConfigTest
	{
		[TestMethod]
		public void ProxyConfigHandlerGetConfigTest()
		{
			var config = ProxyConfigHandler.GetConfig();
			Assert.IsTrue(config.OmitExisting);
			Assert.IsTrue(config.Proxies.Count > 0);
		}

		[TestMethod]
		public void TestDynamicProxyPipelene()
		{
			Proxy.DynamicProxyPipeline.Instance.Initialize(true);

			Proxy.IPipeline pipe = Proxy.DynamicProxyPipeline.Instance;
			Assert.IsNotNull(pipe.DynamixProxyManager);
			Assert.IsTrue(pipe.DynamixProxyManager.Count == 0);

			Proxy.IConfigurable config = Proxy.DynamicProxyPipeline.Instance;
			Assert.IsNotNull(config);

			config.Configure();

			Assert.IsNotNull(pipe.DynamixProxyManager);
			Assert.IsTrue(pipe.DynamixProxyManager.Count > 0);
		}
	}
}
