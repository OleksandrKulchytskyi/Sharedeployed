using System;
using System.Collections.Generic;
using System.Configuration;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ProxyServiceElement : ConfigurationElement
	{
		private const string _cEmpty = "";

		[ConfigurationProperty("alias", IsRequired = true, IsKey = true)]
		public string Alias
		{
			get { return this["alias"] as string; }
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public string Type
		{
			get { return this["type"] as string; }
		}

		[ConfigurationProperty("contract", IsRequired = false, DefaultValue = _cEmpty)]
		public string Contract
		{
			get { return this["contract"] as string; }
		}

		[ConfigurationProperty("scope", IsRequired = false, DefaultValue = 0)]
		public int Scope
		{
			get
			{
				object scope = this["scope"];
				return scope == null ? 0 : Convert.ToInt32(scope);
			}
		}

		private const string _propSection = "properties";
		[ConfigurationProperty(_propSection, IsDefaultCollection = true, IsKey = false, IsRequired = false, DefaultValue = null)]
		public ServicePropertyCollection ServiceProps
		{
			get { return base[_propSection] as ServicePropertyCollection; }

			set { base[_propSection] = value; }
		}

		private const string _ctorSection = "ctors";
		[ConfigurationProperty(_ctorSection, IsDefaultCollection = true, IsKey = false, IsRequired = false, DefaultValue = null)]
		public ServiceCtorArgCollection CtorArgs
		{
			get { return base[_ctorSection] as ServiceCtorArgCollection; }

			set { base[_ctorSection] = value; }
		}

		public T GetInternal<T>() where T : ConfigurationElementCollection
		{
			return base[_cEmpty] as T;
		}
	}

	public class ProxyServiceCollection : ConfigurationElementCollection
	{
		public ProxyServiceCollection()
		{
			//AddElementName = "proxyService";
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
			return new ProxyServiceElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ProxyServiceElement)element).Alias;
		}

		public ProxyServiceElement this[int index]
		{
			get { return base.BaseGet(index) as ProxyServiceElement; }
			set
			{
				if (base.BaseGet(index) != null)
					base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}
	}
}