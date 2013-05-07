using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using ShareDeployed.Proxy.IoC.Config;
using System.Diagnostics;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class IoCConfigTest
	{
		[TestMethod]
		public void CheckIoCConfigTest()
		{
			ShareDeployed.Proxy.IoC.Config.ProxyServicesHandler sect = null;
			try
			{
				sect = ProxyServicesHandler.GetConfig();
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}

			if (sect != null)
			{
				Debug.WriteLine(sect.ToString());
			}
			else
				Assert.Fail();
		}

		[TestMethod]
		public void CheckIoCConfigPropertyTest()
		{
			ShareDeployed.Proxy.IoC.Config.ProxyServicesHandler sect = null;
			try
			{
				sect = ProxyServicesHandler.GetConfig();
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}

			if (sect != null)
			{
				Assert.IsTrue(sect.Services.Count == 2);
				Assert.IsTrue(sect.Services[1].Properties.Count == 1);
				Assert.IsTrue(sect.Services[1].Properties[0].DefaultIfMissed);
			}
			else
				Assert.Fail();
		}
	}
}
