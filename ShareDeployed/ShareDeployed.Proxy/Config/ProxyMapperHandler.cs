using System.Configuration;

namespace ShareDeployed.Proxy.Config
{
	public class ProxyConfigHandler : ConfigurationSection
	{
		public static string proxyConfigHeader = "proxyConfig";

		public static ProxyConfigHandler GetConfig()
		{
			ProxyConfigHandler configInst = ConfigurationManager.GetSection(proxyConfigHeader) as ProxyConfigHandler;
			if (configInst != null)
				return configInst;

			return new ProxyConfigHandler();
		}

		public ProxyConfigHandler()
		{
			_omitExisting = new ConfigurationProperty("omitExisting", typeof(bool), false);
			Properties.Add(_omitExisting);
		}

		[ConfigurationProperty("proxies", IsDefaultCollection = false)]
		[ConfigurationCollection(typeof(ProxyMappingCollection), AddItemName = "proxy", ClearItemsName = "clear")]
		public ProxyMappingCollection Proxies
		{
			get
			{
				ProxyMappingCollection collection = (ProxyMappingCollection)base["proxies"];
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