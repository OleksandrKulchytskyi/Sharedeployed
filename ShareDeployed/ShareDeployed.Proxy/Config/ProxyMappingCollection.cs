using System;
using System.Collections.Generic;
using System.Configuration;

namespace ShareDeployed.Proxy.Config
{
	public class ProxyMappingElement : ConfigurationElement
	{
		[ConfigurationProperty("id", IsRequired = true, IsKey = true)]
		public string Id
		{
			get { return this["id"] as string; }
		}

		[ConfigurationProperty("proxyType", IsRequired = true)]
		public string ProxyType
		{
			get { return this["proxyType"] as string; }
		}

		[ConfigurationProperty("targetType", IsRequired = true)]
		public string TargetType
		{
			get { return this["targetType"] as string; }
		}

		[ConfigurationProperty("isWeak", IsRequired = false, DefaultValue = false)]
		public bool IsWeak
		{
			get
			{
				object weak = this["isWeak"];
				return weak == null ? false : Convert.ToBoolean(weak);
			}
		}
	}

	public class ProxyMappingCollection : ConfigurationElementCollection
	{
		public ProxyMappingCollection()
		{
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get
			{
				return ConfigurationElementCollectionType.AddRemoveClearMap;
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new ProxyMappingElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ProxyMappingElement)element).Id;
		}

		public ProxyMappingElement this[int index]
		{
			get { return base.BaseGet(index) as ProxyMappingElement; }
			set
			{
				if (base.BaseGet(index) != null) base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}
	}
}