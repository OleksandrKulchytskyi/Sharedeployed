using ShareDeployed.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShareDeployed.Extension
{
	public class SingleSignOnAttribute : ActionFilterAttribute, IActionFilter
	{
		private const string _authToken = "AuthToken";

		public override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			//Verify security token and authenticate user
			base.OnActionExecuted(filterContext);
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			HttpContext ctx = HttpContext.Current;
			// If the browser session or authentication session has expired...
			if (ctx.Session["UserName"] == null || !filterContext.HttpContext.Request.IsAuthenticated)
			{
				if (filterContext.HttpContext.Request.IsAjaxRequest())
				{
					// For AJAX requests, we're overriding the returned JSON result with a simple string,
					// indicating to the calling JavaScript code that a redirect should be performed.
					filterContext.Result = new JsonResult { Data = "_Logon_" };
				}
				else
				{
					// For round-trip posts, we're forcing a redirect to Home/TimeoutRedirect/, which
					// simply displays a temporary 5 second notification that they have timed out, and will, in turn, redirect to the logon page.
					filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary {
																			{ "Controller", "Home" },
																			{ "Action", "TimeoutRedirect" }
																		});
				}
			}

			// Preprocessing code used to verify if security token exists
			if (string.IsNullOrEmpty(HttpContext.Current.Request.Headers[_authToken]))
			{
				throw new AuthTokenManagerException("authTokenSign doesn't exist in request headers");
			}
			string authToken = HttpContext.Current.Request.Headers[_authToken];

			AuthClientData data =WebHelper.GetClientIndetification();
			if (!AuthTokenManagerEx.Instance.CheckIfSessionIsAuthenticated(data))
			{
				throw new AuthTokenManagerException("Fail to retreive authentication data");
			}

			base.OnActionExecuting(filterContext);
		}

		public override void OnResultExecuted(ResultExecutedContext filterContext)
		{
			base.OnResultExecuted(filterContext);
		}
	}
}