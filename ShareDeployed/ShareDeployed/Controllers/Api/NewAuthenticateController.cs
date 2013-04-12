using Newtonsoft.Json.Linq;
using ShareDeployed.Common.Models;
using ShareDeployed.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	public class AuthenticateV2Controller : ApiController
	{
		private readonly IMembershipService _membershipService;
		private readonly IAuthenticationTokenService _tokenService;

		public AuthenticateV2Controller(IMembershipService membershipService, IAuthenticationTokenService tokenService)
		{
			_membershipService = membershipService;
			_tokenService = tokenService;
		}

		// POST  { username:, password: }
		[HttpPost]
		public HttpResponseMessage Post()
		{
			JObject body = null;
			try
			{
				body = Request.Content.ReadAsAsync<JObject>().Result;
				if (body == null)
					return Request.CreateResponse(HttpStatusCode.BadRequest);
			}
			catch
			{
				ShareDeployed.Common.Helper.OrderedLock lock1;
				return Request.CreateResponse(HttpStatusCode.BadRequest);
			}

			string username = body.Value<string>("username");
			string password = body.Value<string>("password");

			if (string.IsNullOrEmpty(username))
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing username");

			if (string.IsNullOrEmpty(password))
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing password");

			MessangerUser user = null;
			try
			{
				user = _membershipService.AuthenticateUser(username, password);
			}
			catch (Exception ex)
			{
				return Request.CreateErrorResponse(HttpStatusCode.Forbidden, ex.Message);
			}

			string token = _tokenService.GetAuthenticationToken(user);
			return Request.CreateResponse(HttpStatusCode.OK, token);
		}
	}
}