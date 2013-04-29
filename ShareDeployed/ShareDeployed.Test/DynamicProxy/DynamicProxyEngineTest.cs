using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyEngineTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			IEngine engine = DynamicProxyEngine.Instance;
			engine.Initialize();
		}
	}
}
