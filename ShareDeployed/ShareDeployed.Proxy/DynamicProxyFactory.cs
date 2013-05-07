using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public sealed class DynamicProxyFactory
	{
		private DynamicProxyFactory()
		{
		}

		public static dynamic CreateDynamicProxy(Type type)
		{
			throw new NotImplementedException();
		}

		public static dynamic CreateDynamicProxy(object target)
		{
			return CreateDynamicProxy(target, false);
		}

		public static dynamic CreateDynamicProxy(object target, bool makeTargetWeak)
		{
			CreateInstanceDelegate del = Proxy.ObjectCreatorHelper.ObjectInstantiater(typeof(DynamicProxy));
			dynamic proxy=del();
			proxy.SetTargetObject(target, makeTargetWeak);
			return proxy;
		}
	}
}
