using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ShareDeployed.Filters
{
	public class ValidateModelStateAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			if (!actionContext.ModelState.IsValid)
			{
				actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, actionContext.ModelState);

				string messages = string.Join("; ", actionContext.ModelState.Values
										.SelectMany(x => x.Errors)
										.Select(x => x.ErrorMessage));

				MvcApplication.Logger.Warn(String.Format("{0} {1} {2}", "Error in ModelState", Environment.NewLine, messages));
			}
		}
	}
}