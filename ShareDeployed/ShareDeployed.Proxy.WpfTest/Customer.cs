using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.WpfTest
{
	public class Customer
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public Customer()
		{
		}

		public Customer(int id, string name)
		{
			Id = id;
			Name = name;
		}

		public override string ToString()
		{
			return string.Format("{0}- {1}", Id, Name);
		}
	}
}
