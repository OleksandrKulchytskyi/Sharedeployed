using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ProxyServiceCollection : ConfigurationElementCollection
	{
		public ProxyServiceCollection()
		{
			//AddElementName = "proxyService";
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ProxyService();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ProxyService)element).Alias;
		}

		public ProxyService this[int index]
		{
			get { return base.BaseGet(index) as ProxyService; }
			set
			{
				if (base.BaseGet(index) != null)
					base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}
	}
}
