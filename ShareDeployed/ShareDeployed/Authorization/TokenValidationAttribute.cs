using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Http.Controllers;
using ShareDeployed.Common.Crypt;
using System.Collections.Generic;

namespace ShareDeployed.Authorization
{
	public class CustomHttpsAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			if (!String.Equals(actionContext.Request.RequestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
			{
				actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
				{
					Content = new StringContent("HTTPS Required")
				};
				return;
			}
		}
	}

	public class TokenValidationAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			string token;

			try
			{
				token = actionContext.Request.Headers.GetValues("Authorization-Token").First();
			}
			catch (Exception)
			{
				actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
				{
					Content = new StringContent("Missing Authorization-Token")
				};
				return;
			}

			Repositories.IAspUserRepository repo = null;
			if (actionContext.Request.GetDependencyScope() != null)
				repo = actionContext.Request.GetDependencyScope().GetService(typeof(Repositories.IAspUserRepository)) as Repositories.IAspUserRepository;
			if (repo != null && repo.GetByName(RSAClass.Decrypt(token)) == null)
			{
				actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
				{
					Content = new StringContent("Unauthorized User")
				};
				return;
			}

			base.OnActionExecuting(actionContext);
		}
	}

	public class IPHostValidationAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(HttpActionContext actionContext)
		{

			var context = actionContext.Request.Properties["MS_HttpContext"] as System.Web.HttpContextBase;
			string userIP = context.Request.UserHostAddress;
			try
			{
				AuthorizedIPRepository.GetAuthorizedIPs().First(x => x == userIP);
			}
			catch (Exception)
			{
				actionContext.Response =
				   new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
				   {
					   Content = new StringContent("Unauthorized IP Address")
				   };
				return;
			}
		}
	}

	public class AuthorizedIPRepository
	{
		public static IQueryable<string> GetAuthorizedIPs()
		{
			var ips = new List<string>();

			ips.Add("127.0.0.1");
			ips.Add("::1");

			return ips.AsQueryable();
		}
	}
}