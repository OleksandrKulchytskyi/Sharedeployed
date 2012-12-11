using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShareDeployed.Extension;

namespace ShareDeployed.Filters
{
	public class MultipleResponseFormatsAttribute : ActionFilterAttribute
	{

		public override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			var request = filterContext.HttpContext.Request;
			var viewResult = filterContext.Result as ViewResult;
			if (viewResult == null)
				return;
			if (request.IsAjaxRequest())
			{
				// Replace result with PartialViewResult
				filterContext.Result = new PartialViewResult
				{
					TempData = viewResult.TempData,
					ViewData = viewResult.ViewData,
					ViewName = viewResult.ViewName,
				};
			}
			else if (request.IsJsonRequest())
			{
				// Replace result with JsonResult
				filterContext.Result = new JsonResult
				{
					Data = viewResult.Model
				};
			}
		}

	}
}