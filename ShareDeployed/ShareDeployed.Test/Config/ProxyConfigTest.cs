using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ShareDeployed.Proxy.Config;

namespace ShareDeployed.Test
{
	[TestClass]
	public class ProxyConfigTest
	{
		[TestMethod]
		public void MyTestMethod()
		{
			var config = ProxyConfigHandler.GetConfig();
			Assert.IsTrue(config.OmitExisting);
			Assert.IsTrue(config.Proxies.Count > 0);
		}
	}
}
