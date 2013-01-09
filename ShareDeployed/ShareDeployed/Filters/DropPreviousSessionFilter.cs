using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShareDeployed.Filters
{
	public class DropPreviousSessionFilter : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);

			if (filterContext.HttpContext == null || filterContext.HttpContext.User == null)
				return;

			if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
			{
				if (filterContext.HttpContext.Request.Cookies["ASP.NET_SessionId"] != null)
				{
					filterContext.HttpContext.Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddYears(-30);
				}

				if (filterContext.HttpContext.Session != null)
					filterContext.HttpContext.Session.Abandon();
			}
		}
	}
}