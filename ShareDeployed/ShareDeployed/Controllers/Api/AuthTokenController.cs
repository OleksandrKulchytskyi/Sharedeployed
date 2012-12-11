using ShareDeployed.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[Filters.InitializeSimpleMembershipAttribute()]
	public class AuthTokenController : ApiController
	{
		[HttpGet]
		public string GetAuthKey(string authData)
		{
			string[] credentials = ParseAuthHeaders(authData);
			if (credentials == null && credentials.Length <= 1)
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotAcceptable));

			if (!WebMatrix.WebData.WebSecurity.UserExists(credentials[0]))
				throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

			if (WebMatrix.WebData.WebSecurity.Login(credentials[0], credentials[1]))
			{
				if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Session != null
					&& System.Web.HttpContext.Current.Session["_MyAppSession"] != null)
				{
					WebMatrix.WebData.SimpleRoleProvider provider = new WebMatrix.WebData.SimpleRoleProvider();

					int userId = -1;
					if (WebMatrix.WebData.WebSecurity.IsAuthenticated)
						userId = WebMatrix.WebData.WebSecurity.GetUserId(WebMatrix.WebData.WebSecurity.CurrentUserName);
					else
						userId = WebMatrix.WebData.WebSecurity.GetUserId(credentials[0]);

					AuthClientData client = new AuthClientData();
					client.IpAddress = GetIpAddress();
					client.MachineName = DetermineCompName(client.IpAddress);
					string token = AuthTokenManagerEx.Instance.Generate(client);
					AuthTokenManagerEx.Instance[client].UserId = userId;

					return token;
				}
				else
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
			}

			throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		}

		[HttpPost]
		[Filters.ValidateModelState()]
		public string Authenticate([FromBody]Common.Models.AuthTokenCredential cred)
		{
			if (cred != null)
			{
				if (!WebMatrix.WebData.WebSecurity.UserExists(cred.UserName))
					throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

				if (WebMatrix.WebData.WebSecurity.Login(cred.UserName, cred.Password))
				{
					if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Session != null
						&& System.Web.HttpContext.Current.Session["_MyAppSession"] != null)
					{
						int userId = -1;
						string token = null;

						if (WebMatrix.WebData.WebSecurity.IsAuthenticated)
							userId = WebMatrix.WebData.WebSecurity.GetUserId(WebMatrix.WebData.WebSecurity.CurrentUserName);
						else
							userId = WebMatrix.WebData.WebSecurity.GetUserId(cred.UserName);

						AuthClientData client = new AuthClientData();
						client.IpAddress = GetIpAddress();
						client.MachineName = DetermineCompName(client.IpAddress);
						if (AuthTokenManagerEx.Instance[client] == null)
						{
							token = AuthTokenManagerEx.Instance.Generate(client);
							AuthTokenManagerEx.Instance[client].UserId = userId;
						}

						return token;
					}
					else
						throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
				}
			}

			throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest));
		}

		[HttpGet]
		public bool IsAuthenticated()
		{
			AuthClientData client = new AuthClientData();
			client.IpAddress = GetIpAddress();
			client.MachineName = DetermineCompName(client.IpAddress);

			return (AuthTokenManagerEx.Instance[client] != null &&
					AuthTokenManagerEx.Instance[client].GuidKey != null);
		}

		#region helpers

		[NonAction]
		private string[] ParseAuthHeaders(string data)
		{
			if (string.IsNullOrEmpty(data))
				return null;

			string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(data)).Split(new char[] { ':' });
			return credentials;
		}

		[NonAction]
		private string GetIpAddress()
		{
			string ip = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip))
			{
				ip = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
			}
			return ip;
		}

		[NonAction()]
		private string DetermineCompName(string IP)
		{
			IPAddress myIP = IPAddress.Parse(IP);
			IPHostEntry GetIPHost = Dns.GetHostEntry(myIP);
			List<string> compName = GetIPHost.HostName.ToString().Split('.').ToList();
			return compName.First();
		}
		#endregion
	}
}
