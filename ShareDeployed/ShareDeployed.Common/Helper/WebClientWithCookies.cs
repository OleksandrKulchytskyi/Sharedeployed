using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public class WebClientWithCookies : WebClient
	{
		private readonly Lazy<CookieContainer> _container;

		public WebClientWithCookies()
		{
			_container = new Lazy<CookieContainer>(() => new CookieContainer());
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
			request.Headers.Add("UserAgent", "Mozilla/5.0 (Windows; U; Windows NT 5.1;) Firefox/2.0.0.7");

			if (request is HttpWebRequest)
			{
				(request as HttpWebRequest).CookieContainer = _container.Value;
			}
			return request;
		}

		public CookieContainer GetCookies()
		{
			return _container.Value;
		}
	}
}
