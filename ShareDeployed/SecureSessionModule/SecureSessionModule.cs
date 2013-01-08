using System;
using System.Web;
using System.Web.Security;
using System.Configuration;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Globalization;
using System.Text;

namespace SecureSession
{
	public class SecureSessionModule : IHttpModule
	{
		private static string _ValidationKey = null;

		public void Dispose() { }

		public void Init(HttpApplication app)
		{
			// Initialize validation key if not already initialized 
			if (_ValidationKey == null)
				_ValidationKey = GetValidationKey();

			// Register handlers for BeginRequest and EndRequest events 
			app.BeginRequest += new EventHandler(OnBeginRequest);
			app.EndRequest += new EventHandler(OnEndRequest);
		}

		void OnBeginRequest(Object sender, EventArgs e)
		{
			// Look for an incoming cookie named "ASP.NET_SessionID" 
			HttpRequest request = ((HttpApplication)sender).Request;
			HttpCookie cookie = GetCookie(request, "ASP.NET_SessionId");

			if (cookie != null)
			{
				// Throw an exception if the cookie lacks a MAC 
				if (cookie.Value.Length <= 24)
					throw new InvalidSessionException("Access Denied"); // don't tell bad guys too much

				// Separate the session ID and the MAC 
				string id = cookie.Value.Substring(0, 24);
				string mac1 = cookie.Value.Substring(24);

				// Generate a new MAC from the session ID and requestor info 
				string mac2 = GetSessionIDMac(id, request.UserHostAddress, request.UserAgent, _ValidationKey);

				// Throw an exception if the MACs don't match 
				if (String.CompareOrdinal(mac1, mac2) != 0)
					throw new InvalidSessionException("Access Denied"); // don't tell bad guys too much

				// Strip the MAC from the cookie before ASP.NET sees it 
				cookie.Value = id;
			}
		}

		void OnEndRequest(Object sender, EventArgs e)
		{
			// Look for an outgoing cookie named "ASP.NET_SessionID" 
			HttpRequest request = ((HttpApplication)sender).Request;
			HttpCookie cookie = GetCookie(((HttpApplication)sender).Response, "ASP.NET_SessionId");

			if (cookie != null)
				// Add a MAC 
				cookie.Value += GetSessionIDMac(cookie.Value, request.UserHostAddress, request.UserAgent, _ValidationKey);
		}

		private string GetValidationKey()
		{
			string key = ConfigurationSettings.AppSettings["SessionValidationKey"];

			if (key == null || key == String.Empty)
				throw new InvalidSessionException("SessionValidationKey missing");
			return key;
		}

		private HttpCookie GetCookie(HttpRequest request, string name)
		{
			HttpCookieCollection cookies = request.Cookies;
			return FindCookie(cookies, name);
		}

		private HttpCookie GetCookie(HttpResponse response, string name)
		{
			HttpCookieCollection cookies = response.Cookies;
			return FindCookie(cookies, name);
		}

		private HttpCookie FindCookie(HttpCookieCollection cookies, string name)
		{
			int count = cookies.Count;

			for (int i = 0; i < count; i++)
			{
				if (String.Compare(cookies[i].Name, name, true, CultureInfo.InvariantCulture) == 0)
					return cookies[i];
			}

			return null;
		}

		private string GetSessionIDMac(string id, string ip, string agent, string key)
		{
			StringBuilder builder = new StringBuilder(id, 512);
			if (ip.Equals("::1"))
				ip = "127.0.0.1";
			builder.Append(ip.Substring(0, ip.IndexOf('.', ip.IndexOf('.') + 1)));
			builder.Append(agent);

			using (HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key)))
			{
				return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
			}
		}
	}

	[Serializable]
	public class InvalidSessionException : Exception
	{
		public InvalidSessionException() :
			base("Session cookie is invalid") { }

		public InvalidSessionException(string message) :
			base(message) { }

		public InvalidSessionException(string message,
			Exception inner)
			: base(message, inner) { }

		protected InvalidSessionException(SerializationInfo info,
			StreamingContext context)
			: base(info, context) { }
	}
}