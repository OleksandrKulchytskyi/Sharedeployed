using System.Configuration;

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

		public ProxyServicesHandler()
		{
			_omitExisting = new ConfigurationProperty("omitExisting", typeof(bool), false);
			Properties.Add(_omitExisting);
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

		private readonly ConfigurationProperty _omitExisting;
		[ConfigurationProperty("omitExisting")]
		public bool OmitExisting
		{
			get { return (bool)base[_omitExisting]; }
			set { base[_omitExisting] = value; }
		}
	}
}