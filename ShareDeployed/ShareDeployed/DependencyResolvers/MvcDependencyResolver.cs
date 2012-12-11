using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.DependencyResolvers
{
	internal class MvcDependencyResolver : System.Web.Mvc.IDependencyResolver
	{
		IKernel _kernel;

		public MvcDependencyResolver(IKernel kernel)
		{
			_kernel = kernel;
		}

		public object GetService(Type serviceType)
		{
			return _kernel.TryGet(serviceType, new Ninject.Parameters.IParameter[0]);
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			return _kernel.GetAll(serviceType, new Ninject.Parameters.IParameter[0]);
		}
	}
}