using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ShareDeployed.Proxy.Config;
using System.Diagnostics;

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
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Proxy.DynamicProxyPipeline.Instance.Initialize(true);

			Proxy.IPipeline pipe = Proxy.DynamicProxyPipeline.Instance;
			Assert.IsNotNull(pipe.DynamixProxyManager);
			Assert.IsTrue(pipe.DynamixProxyManager.Count == 0);

			Proxy.IConfigurable config = Proxy.DynamicProxyPipeline.Instance;
			Assert.IsNotNull(config);

			config.Configure();

			Assert.IsNotNull(pipe.DynamixProxyManager);
			Assert.IsTrue(pipe.DynamixProxyManager.Count > 0);

			dynamic propertyProxy = pipe.DynamixProxyManager.Get("propertyHolder");
			Assert.IsTrue(propertyProxy.Id == 0);
			propertyProxy.Name = "Hello world";
			StringAssert.Contains(propertyProxy.GetName(), "Hello world");
			sw.Stop();
			long elapsedMs = sw.ElapsedMilliseconds;
			Debug.WriteLine("Elapsed time {0}, ms", elapsedMs);
		}
	}
}
