using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ShareDeployed.Common.Helper
{
	public class WebRequesthelper
	{
		public static  void Do(string url)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Headers.Add("uid", "admin");
			request.Headers.Add("pass", "vax804");
			request.Headers.Add("logonType", "0");

			request.CookieContainer = new CookieContainer();
			request.UserAgent = "IE 7.0";
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			CookieCollection ckc = response.Cookies;
			Cookie ck = ckc["ASP.NET_SessionId"];
			if (null != ck)
			{
				System.Diagnostics.Debug.Write(ck.Value);
			}

			Cookie ck2 = ckc["messanger.state"];
			if (null != ck2)
			{
				System.Diagnostics.Debug.Write(ck2.Value);
			}
		}
	}
}
