using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	/// <summary>
	/// Thrown when an attempt is made to create an object of a type that is not an interface
	/// </summary>
	public class TypeIsNotAnInterface : Exception
	{
		internal TypeIsNotAnInterface(Type type)
			: base(@"The InterfaceObjectFactory only works with interfaces.
 An attempt was made to create an object for the following type, which is not an interface: " + type.FullName)
		{ }
	}
}
