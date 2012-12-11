using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Ninject;
using System.Web.Http.Dependencies;

namespace ShareDeployed.App_Start
{
	public class NinjectResolver : NinjectScope, IDependencyResolver
	{
		private IKernel _kernel;

		public NinjectResolver(IKernel kernel)
			: base(kernel)
		{
			_kernel = kernel;
		}

		public IDependencyScope BeginScope()
		{
			return new NinjectScope(_kernel.BeginBlock());
		}

		protected override void Dispose(bool disposing)
		{
			if (_kernel != null)
			{
				_kernel.Dispose();
				GC.Collect();
			}
			base.Dispose(disposing);
		}
	}
}