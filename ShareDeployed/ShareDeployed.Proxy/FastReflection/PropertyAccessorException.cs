using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.FastReflection
{
	[Serializable]
	public class PropertyAccessorException : Exception
	{
		public PropertyAccessorException(string message)
			: base(message)
		{
		}
	}
}
