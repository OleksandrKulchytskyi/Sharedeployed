using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ShareDeployed.Common.Infrastructure
{
	public class CookieAwareWebClient : WebClient
	{

		public CookieContainer CookieContainer { get; private set; }

		public CookieAwareWebClient()
			: this(new CookieContainer())
		{
		}

		public CookieAwareWebClient(CookieContainer cookieContainer)
		{
			if (cookieContainer == null) throw new ArgumentNullException("cookieContainer");
			this.CookieContainer = cookieContainer;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			if (this.CookieContainer == null)
			{
				throw new InvalidOperationException("CookieContainer is null");
			}
			var request = base.GetWebRequest(address);
			if (request is HttpWebRequest)
			{
				(request as HttpWebRequest).CookieContainer = this.CookieContainer;
			} return request;
		}
	}
}
