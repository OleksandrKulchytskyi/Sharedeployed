using System.Configuration;

namespace ShareDeployed.Proxy.IoC.Config
{
	public class ServiceCtorArgumentElement : ConfigurationElement
	{
		[ConfigurationProperty("name", IsRequired = false, IsKey = false)]
		public string Name { get { return this["name"] as string; } }

		[ConfigurationProperty("alias", IsRequired = false)]
		public string Alias { get { return this["alias"] as string; } }

		[ConfigurationProperty("value", IsRequired = false)]
		public string Value { get { return this["value"] as string; } }

		[ConfigurationProperty("valueType", IsRequired = false)]
		public string ValueType { get { return this["valueType"] as string; } }
	}


	[ConfigurationCollection(typeof(ServiceCtorArgumentElement), AddItemName = "ctor-arg", ClearItemsName = "clear",
		CollectionType=ConfigurationElementCollectionType.BasicMap)]
	public class ServiceCtorArgCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new ServiceCtorArgumentElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ServiceCtorArgumentElement)element).Name;
		}

		public ServiceCtorArgumentElement this[int index]
		{
			get { return base.BaseGet(index) as ServiceCtorArgumentElement; }
			set
			{
				if (base.BaseGet(index) != null)
					base.BaseRemoveAt(index);
				base.BaseAdd(index, value);
			}
		}
	}
}