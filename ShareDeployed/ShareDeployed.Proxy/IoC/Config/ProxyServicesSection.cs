using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ProxyServicesHandler : ConfigurationSection
	{
		public static string proxyServicesHeader = "proxyServices";

		public static ProxyServicesHandler GetConfig()
		{
			ProxyServicesHandler config = ConfigurationManager.GetSection(proxyServicesHeader) as ProxyServicesHandler;
			if (config != null)
				return config;

			return new ProxyServicesHandler();
		}

		[ConfigurationProperty("services", IsDefaultCollection = false)]
		[ConfigurationCollection(typeof(ProxyServiceCollection), AddItemName = "service", ClearItemsName = "clear")]
		public ProxyServiceCollection Services
		{
			get
			{
				ProxyServiceCollection collection = (ProxyServiceCollection)base["services"];
				return collection;
			}
		}

	}
}
