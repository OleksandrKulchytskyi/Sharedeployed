using ShareDeployed.Extension;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[WebAPI.Hmac.Filters.AuthenticateWithTimeStamp]
	public class PingController : ApiController
	{
		private string sessionTokenId = string.Empty;

		[HttpGet]
		public HttpResponseMessage Ping()
		{
			UpdateActivity();

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new System.Net.Http.StringContent(DateTime.UtcNow.ToString())
			};
		}

		private void UpdateActivity()
		{
			if (Request.Headers.GetCookie("messanger.state") != null)
			{
				var state = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Request.Headers.GetCookie("messanger.state"));
				sessionTokenId = state.tokenId;
			}
		}
	}
}