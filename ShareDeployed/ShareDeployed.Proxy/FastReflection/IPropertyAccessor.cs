using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.FastReflection
{
	public interface IPropertyAccessor
	{
		object Get(object target);
		void Set(object target, object value);
	}
}
