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
				Assert.IsTrue(sect.Services.Count > 0);
				Assert.IsTrue(sect.Services[1].ServiceProps.Count == 1);
				Assert.IsTrue(sect.Services[1].ServiceProps[0].DefaultIfMissed);
				StringAssert.Contains(sect.Services[2].CtorArgs[0].Name, "maxSpeed");
			}
			else
				Assert.Fail();
		}
	}

	public class ClassWithParameters
	{
		private int _maxSpeed;
		private ShareDeployed.Proxy.Logging.ILogAggregator _agg;

		public ClassWithParameters(ShareDeployed.Proxy.Logging.ILogAggregator log, int maxSpeed)
		{
			_agg = log;
			_maxSpeed = maxSpeed;
		}

		public bool IsLogAggNull()
		{
			return _agg==null;
		}

		public ShareDeployed.Proxy.Logging.ILogAggregator LogAggregator { get { return _agg; } }
	}
}