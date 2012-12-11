using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Mvc;

namespace ShareDeployed.Extension
{
	public class HandledErrorLoggerFilter : System.Web.Mvc.IExceptionFilter
	{
		public void OnException(ExceptionContext filterContext)
		{

		}
	}

	public class MyCustomHandleError : HandleErrorAttribute
	{
		public override void OnException(ExceptionContext filterContext)
		{
			if (filterContext == null)
				base.OnException(filterContext);

			LogException(filterContext.Exception);

			if (filterContext.HttpContext.IsCustomErrorEnabled)
			{
				filterContext.ExceptionHandled = true;
				base.OnException(filterContext);
			}
		}

		private void LogException(Exception ex)
		{
			if (MvcApplication.Logger != null)
			{
				MvcApplication.Logger.Error("MyCustomHandleError ", ex);
			}
		}
	}

	public class ExceptionHandlingAttribute : ExceptionFilterAttribute
	{
		public override void OnException(HttpActionExecutedContext context)
		{
			if (MvcApplication.Logger != null && context.Exception != null)
			{
				MvcApplication.Logger.Error("ExceptionHandlingAttribute", context.Exception);
			}

			if (context.Exception is System.Data.DataException)
			{
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = new StringContent(context.Exception.Message),
					ReasonPhrase = "Exception"
				});

			}

			//Log Critical errors
			Debug.WriteLine(context.Exception);

			throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
			{
				Content = new StringContent("An error occurred, please try again or contact the administrator."),
				ReasonPhrase = "Critical Exception"
			});
		}
	}
}