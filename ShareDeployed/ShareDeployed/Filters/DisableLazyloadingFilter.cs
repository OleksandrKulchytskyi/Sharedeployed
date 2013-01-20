using ShareDeployed.Repositories;
using System;
using System.Net.Http;
using System.Web.Http.Filters;

namespace ShareDeployed.Filters
{
	[AttributeUsage(AttributeTargets.Method, Inherited = true)]
	public class DisableLazyloadingFilter : ActionFilterAttribute
	{
		private IMessangerRepository _repository;

		public bool ProxyCreation { get; private set; }

		public bool DetectChanges { get; private set; }

		public DisableLazyloadingFilter()
			: base()
		{
			ProxyCreation = true;
			DetectChanges = true;
		}

		public DisableLazyloadingFilter(bool proxyCreation, bool detectChanges)
			: this()
		{
			this.DetectChanges = detectChanges;
			this.ProxyCreation = proxyCreation;
		}

		public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
		{
			if (actionContext.Request.GetDependencyScope() != null)
			{
				try
				{
					_repository = actionContext.Request.GetDependencyScope().GetService(typeof(IMessangerRepository)) as IMessangerRepository;
					if (_repository != null)
					{
						_repository.SetDbContextOprions(false, this.ProxyCreation, DetectChanges);
						System.Diagnostics.Trace.WriteLine("Scoped repository ID: " + (_repository.SessionId));
					}
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("DisableLazyloadingFilter", ex);
				}
			}
			base.OnActionExecuting(actionContext);
		}

		public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
		{
			if (_repository != null)
			{
				_repository.SetDbContextOprions(true);
			}
			base.OnActionExecuted(actionExecutedContext);
		}
	}
}