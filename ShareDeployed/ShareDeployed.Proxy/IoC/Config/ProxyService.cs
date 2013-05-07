using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ProxyService : ConfigurationElement
	{
		private const string _cEmpty = "";

		[ConfigurationProperty("alias", IsRequired = true)]
		public string Alias
		{
			get { return this["alias"] as string; }
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public string Type
		{
			get { return this["type"] as string; }
		}

		[ConfigurationProperty("contract", IsRequired = false)]
		public string Contract
		{
			get { return this["contract"] as string; }
		}

		[ConfigurationProperty("scope", IsRequired = false)]
		public int Scope
		{
			get
			{
				object scope = this["scope"];
				return scope == null ? 0 : Convert.ToInt32(scope);
			}
		}

		[ConfigurationProperty(_cEmpty, IsDefaultCollection = true, IsKey = false, IsRequired = false)]
		public ServicePropertyCollection Properties
		{
			get
			{
				return base[_cEmpty] as ServicePropertyCollection;
			}

			set
			{
				base[_cEmpty] = value;
			}
		}
	}

	public class ServicePropertyElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name { get { return this["name"] as string; } }

		[ConfigurationProperty("alias", IsRequired = true)]
		public string Alias { get { return this["alias"] as string; } }

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

	[ConfigurationCollection(typeof(ServicePropertyElement), AddItemName = "property")]
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
