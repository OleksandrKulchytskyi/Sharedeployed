using Newtonsoft.Json;
using Ninject;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using ShareDeployed.Infrastructure;
using ShareDeployed.Services;
using System;
using System.Web;

namespace ShareDeployed.Handlers
{
	public class LoginHandler : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			string uid = null;
			string pass = string.Empty;
			string authToken = string.Empty;
			string response = string.Empty;

			var settings = ShareDeployed.Infrastructure.Bootstrapper.Kernel.Get<IAppSettings>();
			string apiKey = settings.AuthApiKey;

			if (string.IsNullOrEmpty(apiKey))
			{
				response = JsonConvert.SerializeObject(new { error = "ApiKey has not specified", errorId = 2 });
				context.Response.Write(response);
				context.ApplicationInstance.CompleteRequest();
				return;
			}

			string logonType = context.Request.Headers.Get("logonType");

			switch (logonType)
			{
				case "0":
					uid = context.Request.Headers.Get("uid");
					pass = context.Request.Headers.Get("pass");

					if (string.IsNullOrEmpty(uid) && string.IsNullOrEmpty(pass))
					{
						response = JsonConvert.SerializeObject(new { error = "Not all required fields are submitted", errorId = 1 });
						context.Response.Write(response);
						context.ApplicationInstance.CompleteRequest();
						return;
					}

					string token = context.Request.Form["token"];

					var repository = Bootstrapper.Kernel.Get<Repositories.IMessangerRepository>();
					var messangerService = Bootstrapper.Kernel.Get<Services.IMessangerService>();

					string hash = context.Request.QueryString["hash"];

					int userId;
					if (!repository.AuthenticateUserShared(uid, pass, out userId))
					{
						throw new InvalidOperationException("User doesn't exist in application context.");
					}

					var authClient = WebHelper.GetClientIndetification();

					authToken = Authorization.AuthTokenManagerEx.Instance.Generate(authClient);
					if (Authorization.AuthTokenManagerEx.Instance[authClient] != null)
					{
						Authorization.AuthTokenManagerEx.Instance[authClient].UserId = userId;
						Authorization.AuthTokenManagerEx.Instance[authClient].UserName = uid;

						var cInfo = new Authorization.ClientInfo() { Id = userId, UserName = uid };
						Authorization.AuthTokenManagerEx.Instance.AddClientInfo(cInfo, authToken);
					}

					var state = JsonConvert.SerializeObject(new { userId = userId });
					var cookie = new HttpCookie("messanger.state", state);
					cookie.Expires = DateTime.Now.AddDays(30);
					context.Response.Cookies.Add(cookie);

					response = JsonConvert.SerializeObject(new
					{
						userId = userId,
						authToken = authToken,
						errorId = 0,
						userIdentity = string.Format("{0}_{1}", uid, userId)
					});

					context.Response.Write(response);
					context.ApplicationInstance.CompleteRequest();
					break;

				case "1":
					uid = context.Request.Headers.Get("uid");
					string usrId = context.Request.Headers.Get("userId");
					authToken = context.Request.Headers.Get("authToken");

					if (Authorization.AuthTokenManagerEx.Instance.CheckClientToken(authToken))
					{
						var loggedInfo = new Authorization.ClientInfo() { Id = int.Parse(usrId), UserName = uid };
						if (Authorization.AuthTokenManagerEx.Instance[loggedInfo] != null &&
							Authorization.AuthTokenManagerEx.Instance[loggedInfo].Equals(authToken, StringComparison.OrdinalIgnoreCase))
						{
							response = JsonConvert.SerializeObject(new { errorId = 0, error = "User has been successfully logged in." });
							context.Response.Write(response);
							context.ApplicationInstance.CompleteRequest();
						}
					}
					else
					{
						response = JsonConvert.SerializeObject(new { error = "Access token verification was failed.", errorId = 2 });

						context.Response.Write(response);
						context.ApplicationInstance.CompleteRequest();
					}
					break;

				default:
					response = JsonConvert.SerializeObject(new { error = "logon type has not specified.", errorId = 1 });

					context.Response.Write(response);
					context.ApplicationInstance.CompleteRequest();
					break;
			}
		}

		private ClientState GetClientState(HttpContext context)
		{
			var messangerState = GetCookieValue(context, "messanger.state");// New client state

			ClientState clientState = null;
			if (String.IsNullOrEmpty(messangerState))
				clientState = new ClientState();

			else
				clientState = JsonConvert.DeserializeObject<ClientState>(messangerState);

			return clientState;
		}

		private string GetCookieValue(HttpContext context, string key)
		{
			HttpCookie cookie = context.Request.Cookies[key];
			return cookie != null ? HttpUtility.UrlDecode(cookie.Value) : null;
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}