using System;
using System.Configuration;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ServicePropertyElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsRequired = false, IsKey = false)]
		public string Name { get { return this["name"] as string; } }

		[ConfigurationProperty("alias", IsRequired = false)]
		public string Alias { get { return this["alias"] as string; } }

		[ConfigurationProperty("value", IsRequired = false)]
		public string Value { get { return this["value"] as string; } }

		[ConfigurationProperty("valueType", IsRequired = false)]
		public string ValueType { get { return this["valueType"] as string; } }

		[ConfigurationProperty("defaultIfMissed", IsRequired = false)]
		public bool DefaultIfMissed
		{
			get
			{
				object value = this["defaultIfMissed"];
				return value == null ? false : Convert.ToBoolean(value);
			}
		}
	}

	[ConfigurationCollection(typeof(ServicePropertyElement), AddItemName = "property", ClearItemsName = "clear",
		CollectionType = ConfigurationElementCollectionType.BasicMap)]
	public class ServicePropertyCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ServicePropertyElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ServicePropertyElement)element).Name;
		}

		public ServicePropertyElement this[int index]
		{
			get { return base.BaseGet(index) as ServicePropertyElement; }
			set
			{
				if (base.BaseGet(index) != null)
					base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}
	}
}