using Ninject;
using Ninject.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using System.Web.Http.Dependencies;

namespace ShareDeployed.DependencyResolvers
{
	public class NinjectDependencyScope : IDependencyScope
	{
		private IResolutionRoot resolver;
		bool _dispoded = false;

		internal NinjectDependencyScope(IResolutionRoot resolver)
		{
			Contract.Assert(resolver != null);

			this.resolver = resolver;
		}

		public object GetService(Type serviceType)
		{
			if (resolver == null)
				throw new ObjectDisposedException("this", "This scope has already been disposed");

			return resolver.TryGet(serviceType);
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			if (resolver == null)
				throw new ObjectDisposedException("this", "This scope has already been disposed");

			return resolver.GetAll(serviceType);
		}

		#region IDisposable
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_dispoded)
			{
				_dispoded = true;

				if (disposing)
				{
					IDisposable disposable = resolver as IDisposable;
					if (disposable != null && !_dispoded)
					{
						disposable.Dispose();
						resolver = null;
					}
				}
			}
		} 
		#endregion
	}

	public class NinjectWebApiDependencyResolver : NinjectDependencyScope, IDependencyResolver
	{
		private IKernel _kernel;

		public NinjectWebApiDependencyResolver(IKernel kernel)
			: base(kernel)
		{
			this._kernel = kernel;
		}

		public IDependencyScope BeginScope()
		{
			return new NinjectDependencyScope(_kernel.BeginBlock());
		}
	}
}