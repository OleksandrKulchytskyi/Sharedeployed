using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[WebAPI.Hmac.Filters.AuthenticateWithTimeStamp]
	public class PingController : ApiController
	{
		[HttpGet]
		public HttpResponseMessage Ping()
		{
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content =
					new System.Net.Http.StringContent(DateTime.UtcNow.ToString())
			};
		}

	}
}
