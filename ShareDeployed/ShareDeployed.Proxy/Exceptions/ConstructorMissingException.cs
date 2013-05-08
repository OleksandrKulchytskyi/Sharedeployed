using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public class ConstructorMissingException : ApplicationException
	{
		public Type Type { get; private set; }
		public int ParameterCount { get; private set; }

		public ConstructorMissingException(Type type)
			: base(string.Format("Default constructor not found for type '{0}'.", type.FullName))
		{
			Type = type;
			ParameterCount = 0;
		}

		public ConstructorMissingException(Type type, int parameterCount)
			: base(string.Format("Constructor with {1} parameter(s) not found for type '{0}'.", type.FullName, parameterCount))
		{
			Type = type;
			ParameterCount = parameterCount;
		}
	}
}
