using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Dependencies;
using Ninject.Syntax;
using Ninject.Activation;
using System.Diagnostics;

namespace ShareDeployed.App_Start
{
	public class NinjectScope : IDependencyScope
	{
		protected IResolutionRoot resolutionRoot;
		bool _disposed = false;

		public NinjectScope(IResolutionRoot kernel)
		{
			resolutionRoot = kernel;
		}

		public object GetService(Type serviceType)
		{
			IRequest request = resolutionRoot.CreateRequest(serviceType, null, new Ninject.Parameters.Parameter[0], true, true);
			return resolutionRoot.Resolve(request).SingleOrDefault();
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			IRequest request = resolutionRoot.CreateRequest(serviceType, null, new Ninject.Parameters.Parameter[0], true, true);
			return resolutionRoot.Resolve(request).ToList();
		}

		[DebuggerStepThrough]
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					IDisposable disposable = (IDisposable)resolutionRoot;
					if (disposable != null)
						disposable.Dispose();

					resolutionRoot = null;
					GC.Collect();
				}
				_disposed = true;
			}
		}
	}
}