using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class IoCHarnessTest
	{
		private System.Threading.ManualResetEvent _event;

		[TestInitialize]
		public void OnInitialize()
		{
			_event = new System.Threading.ManualResetEvent(false);
			DynamicProxyPipeline.Instance.Initialize(true);
		}

		[TestCleanup]
		public void OnUninitialize()
		{
			_event.Dispose();
		}

		[TestMethod]
		public void HarnessTestMethod()
		{

		}
	}
}
