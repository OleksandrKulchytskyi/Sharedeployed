using System;
using System.Web;

namespace ShareDeployed.HttpModules
{
	public class MobileRouterModule : IHttpModule
	{
		private const String ForceFullSiteCookieName = "FullSiteMode";

		public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			context.BeginRequest += OnBeginRequest;
		}

		private static void OnBeginRequest(Object sender, EventArgs e)
		{
			var app = sender as HttpApplication;
			if (app == null)
				throw new ArgumentNullException("sender");

			// Check whether it is a mobile site
			var isMobileDevice = IsMobileUserAgent(app);

			// The mobile user confirmed to view the desktop site
			if (isMobileDevice && ForceFullSite(app))
			{
				app.Response.AppendCookie(new HttpCookie(ForceFullSiteCookieName));
				return;
			}

			// The mobile user is navigating through the desktop site
			if (isMobileDevice && HasFullSiteCookie(app))
				return;

			// The mobile user is attempting to view a desktop page
			if (isMobileDevice)
				ToMobileLandingPage(app);
		}

		public static bool IsMobileUserAgent(HttpApplication httpApp)
		{
			return httpApp.Request.Browser.IsMobileDevice;

			//	userAgent = userAgent.ToLower();

			//	return userAgent.Contains("iphone") |
			//		 userAgent.Contains("ppc") |
			//		 userAgent.Contains("windows ce") |
			//		 userAgent.Contains("blackberry") |
			//		 userAgent.Contains("opera mini") |
			//		 userAgent.Contains("mobile") |
			//		 userAgent.Contains("palm") |
			//		 userAgent.Contains("portable");
		}

		private static bool HasFullSiteCookie(HttpApplication app)
		{
			var cookie = app.Context.Request.Cookies["FullSiteModeCookie"];
			return cookie != null;
		}

		private static bool ForceFullSite(HttpApplication app)
		{
			var full = app.Context.Request.QueryString["mode"];
			if (!String.IsNullOrEmpty(full))
				return String.Equals(full, "full", StringComparison.InvariantCultureIgnoreCase);
			return false;
		}

		private static void ToMobileLandingPage(HttpApplication app)
		{
			var landingPage = System.Configuration.ConfigurationManager.AppSettings["MobileLandingPage"];
			if (!String.IsNullOrEmpty(landingPage))
				app.Context.Response.Redirect(landingPage);
		}

		private static bool HasAnyMobileKeyword(String userAgent)
		{
			string ua = userAgent.ToLower();
			return (ua.Contains("midp") ||
			  ua.Contains("mobile") ||
			  ua.Contains("android") ||
			  ua.Contains("samsung") ||
			  ua.Contains("nokia") ||
			  ua.Contains("phone") ||
			  ua.Contains("opera mini") ||
			  ua.Contains("opera mobi") ||
			  ua.Contains("blackberry") ||
			  ua.Contains("symbian") ||
			  ua.Contains("j2me") ||
			  ua.Contains("windows ce") ||
			  ua.Contains("vodafone") ||
			  ua.Contains("ipad;") ||
			  ua.Contains("maemo") ||
			  ua.Contains("palm") ||
			  ua.Contains("fennec") ||
			  ua.Contains("wireless") ||
			  ua.Contains("htc") ||
			  ua.Contains("nintendo") ||
			  ua.Contains("zunewp7") ||
			  ua.Contains("silk"));
		}
	}
}