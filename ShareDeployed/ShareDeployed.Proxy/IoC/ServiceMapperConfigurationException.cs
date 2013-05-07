using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	[Serializable]
	public class ServiceMapperConfigurationException : Exception
	{
		public ServiceMapperConfigurationException(string message)
			: base(message)
		{
		}

		public ServiceMapperConfigurationException(string message, Exception InnerException)
			: base(message, InnerException)
		{
		}
	}
}
