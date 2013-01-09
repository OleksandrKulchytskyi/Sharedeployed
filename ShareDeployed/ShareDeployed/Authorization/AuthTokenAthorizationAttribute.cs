using System;
using System.Configuration;
using System.Linq;
using log4net;

using System.Text;
using System.Web;
using System.Web.Http;
using System.Security.Principal;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using System.Globalization;

namespace ShareDeployed.Authorization
{
	public class AuthTokenAthorizationAttribute : AuthorizeAttribute
	{
		private const string TimestampHeaderName = "Timestamp";
		private const string AuthTokenHeaderName = "AuthToken";

		private bool requireSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["RequireSsl"]);
		public bool RequireSsl
		{
			get { return requireSsl; }
			set { requireSsl = value; }
		}

		bool requireToken = true;
		public bool RequireToken
		{
			get { return requireToken; }
			set { requireToken = value; }
		}

		bool requireTimestamp = false;
		public bool RequireTimestamp
		{
			get { return requireTimestamp; }
			set { requireTimestamp = value; }
		}

		/// <summary>
		/// For logging with log4net.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(typeof(AuthTokenAthorizationAttribute));

		public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
		{
			if ((HttpContext.Current != null && HttpContext.Current.Request.IsAuthenticated)
				 || (CheckToken(actionContext) || !RequireToken))
			{
				return;
			}
			else
			{
				HandleUnauthorizedRequest(actionContext);
			}
		}

		private bool CheckToken(System.Web.Http.Controllers.HttpActionContext actionContext)
		{
			if (RequireSsl && !HttpContext.Current.Request.IsSecureConnection && !HttpContext.Current.Request.IsLocal)
			{
				log.Error("Failed to login: SSL:" + HttpContext.Current.Request.IsSecureConnection);
				return false;
			}

			if (!HttpContext.Current.Request.Headers.AllKeys.Contains(AuthTokenHeaderName))
				return false;

			if (RequireTimestamp)
			{
				string timeStampString = GetHttpRequestHeader(actionContext.Request.Headers, TimestampHeaderName);
				if (!IsDateValidated(timeStampString))
					return false;
			}

			string authToken = HttpContext.Current.Request.Headers[AuthTokenHeaderName];

			//IPrincipal principal;
			if (VerifyAuthToken(authToken))//TryGetPrincipal(authKey, out principal))
			{
				//HttpContext.Current.User = principal;
				return true;
			}
			return false;
		}

		protected override void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext actionContext)
		{
			var challengeMessage = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
			challengeMessage.Headers.Add("WWW-Authenticate", "Basic");
			throw new HttpResponseException(challengeMessage);
		}

		private bool VerifyAuthToken(string authToken)
		{
			// Check this is a Basic Auth header
			if (string.IsNullOrEmpty(authToken))
				return false;

			var client = WebHelper.GetClientIndetification();

			int userId;
			if (Authorization.AuthTokenManagerEx.Instance.CheckIfSessionIsAuthenticated(client, authToken, out userId))
			{
				HttpContext.Current.Request.Cookies.Add(new HttpCookie("UserId", userId.ToString()));
				string val = HttpContext.Current.Request.Headers[AuthTokenHeaderName];
				HttpContext.Current.Request.Headers[AuthTokenHeaderName] = string.Format("{0}:{1}", val, userId);
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Validate timestamp string
		/// </summary>
		/// <param name="timestampString"></param>
		/// <returns></returns>
		private bool IsDateValidated(string timestampString)
		{
			DateTime timestamp;
			if (!DateTime.TryParseExact(timestampString, "U", null, DateTimeStyles.AdjustToUniversal, out timestamp))
				return false;

			var now = DateTime.UtcNow;

			// TimeStamp should not be in 3 minutes behind
			if (timestamp < now.AddMinutes(-3))
				return false;

			if (timestamp > now.AddMinutes(3))
				return false;

			return true;
		}

		private static string GetHttpRequestHeader(System.Net.Http.Headers.HttpHeaders headers, string headerName)
		{
			if (!headers.Contains(headerName))
				return string.Empty;

			return headers.GetValues(headerName).SingleOrDefault();
		}

		private bool TryGetPrincipal(string username, string password, out IPrincipal principal)
		{
			// this is the method that does the authentication users often add a copy/paste space at the end of the username
			username = username.Trim();
			password = password.Trim();

			User user = new User() { LogonName = username };//AccountManagement.ApiLogin(username, password);
			if (user != null)
			{
				// once the user is verified, assign it to an IPrincipal with the identity name and applicable roles
				principal = new GenericPrincipal(new GenericIdentity(username), System.Web.Security.Roles.GetRolesForUser(username));
				return true;
			}
			else
			{
				if (!String.IsNullOrWhiteSpace(username))
				{
					log.Error("Failed to login: username=" + username + "; password=" + password);
				}
				principal = null;
				return false;
			}
		}
	}
}