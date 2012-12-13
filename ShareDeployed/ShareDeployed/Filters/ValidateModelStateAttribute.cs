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

				var keys = actionContext.ModelState.Keys.Select(x => x);
				var errors = actionContext.ModelState.Values.Select(x => x.Errors);

				var combined = keys.Zip(errors, (keyName, error) =>
				{
					return string.Format("{0}: {1}", keyName, string.Join("; ", error.Select(x => x.ErrorMessage)));
				});

				//string messages = string.Join("; ", actionContext.ModelState.Values.SelectMany(x => x.Errors).
				//									Select(x => x.ErrorMessage));

				string allMsgs = string.Join(Environment.NewLine, combined);
				MvcApplication.Logger.Warn(String.Format("Error in ModelState {0} {1}", Environment.NewLine, allMsgs));
			}
		}
	}
}