using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Web;

namespace ShareDeployed.Extension
{
	public static class JsonRequestExtensions
	{
		public static bool IsJsonRequest(this HttpRequestBase request)
		{
			return string.Equals(request["format"], "json");
		}
	}

	public static class WebHelper
	{
		internal static bool RequestIsLocal(HttpRequest request)
		{
			if (request.UserHostAddress == "127.0.0.1" ||
				request.UserHostAddress == request.ServerVariables["LOCAL_ADDR"])
				return true;
			else
				return false;
		}

		internal static string GetIpAddress()
		{
			string ipAddress = string.Empty;
			ipAddress = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ipAddress))
				ipAddress = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

			return ipAddress;
		}

		internal static bool DetermineCompName(string IP, out string clientName)
		{
			clientName = string.Empty;
			try
			{
				IPAddress myIP = IPAddress.Parse(IP);
				IPHostEntry GetIPHost = Dns.GetHostEntry(myIP);
				List<string> compName = GetIPHost.HostName.ToString().Split('.').ToList();
				clientName = compName.First();
				return true;
			}
			catch (Exception)
			{
			}
			return false;
		}

		internal static string DetermineNameFromHeaders()
		{
			return System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_HOST"];
		}

		internal static Authorization.AuthClientData GetClientIndetification()
		{
			string ip = Extension.WebHelper.GetIpAddress();
			string clientName;
			if (!Extension.WebHelper.DetermineCompName(ip, out clientName))
			{
				MvcApplication.Logger.Info("Fail to retrieve client name with help of DetermineCompName");
				clientName = Extension.WebHelper.DetermineNameFromHeaders();
				MvcApplication.Logger.InfoFormat("DetermineNameFromHeaders exucuted with result {0}", clientName);
			}
			var authClient = new ShareDeployed.Authorization.AuthClientData(ip, clientName);
			return authClient;
		}
	}

	public static class HttpRequestHeaderExtension
	{
		public static string GetCookie(this HttpRequestHeaders header, string name)
		{
			try
			{
				var cookies = GetCookies(header);
				return cookies.Select(cookie => cookie[name].Value).FirstOrDefault(value => value != null);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static IEnumerable<CookieHeaderValue> GetCookies(HttpRequestHeaders header)
		{
			var result = new System.Collections.ObjectModel.Collection<CookieHeaderValue>();
			IEnumerable<string> cookies;
			if (header.TryGetValues("Cookie", out cookies))
			{
				foreach (string cookie in cookies)
				{
					CookieHeaderValue cookieHeaderValue;
					if (CookieHeaderValue.TryParse(cookie, out cookieHeaderValue))
						result.Add(cookieHeaderValue);
				}
			}
			return result;
		}
	}
}