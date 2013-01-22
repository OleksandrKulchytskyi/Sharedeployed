using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Mailgrabber.Helpers;
using ShareDeployed.Common.Extensions;

namespace ShareDeployed.Test
{
	[TestClass]
	public class CookielessWebClientTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			string url = "http://localhost:1212/handlers/loginhandler.ashx";

			var before = System.IO.File.ReadAllText("d:\\cookie.txt");
			var after = before.CalculateSHA256Hash();
			if (before.Length < after.Length)
			{
			}

			//ShareDeployed.Common.Helper.WebRequesthelper.Do(url);

			WebClientWithCookies client = new WebClientWithCookies();

			client.Headers.Add("uid", "admin");
			client.Headers.Add("pass", "vax804");
			client.Headers.Add("logonType", "0");

			var data = client.DownloadString(url);
			if (data != null)
			{
			}

			if (client.GetCookies() != null)
			{
				Assert.IsTrue(client.GetCookies().Count > 0);

				try
				{
					var cookie2 = client.GetCookies().GetCookies(new Uri("//"));
					if (cookie2 != null)
					{
					}
				}
				catch (Exception)
				{

				}

				var cookie = client.GetCookies().GetCookies(new Uri(url));
				if (cookie != null)
				{
					Assert.IsNotNull(cookie["messanger.state"]);
					Assert.IsNotNull(cookie[".ASPXAUTH"]);

					string messanger_state = cookie["messanger.state"].Value;
					if (string.IsNullOrEmpty(messanger_state))
					{
					}
				}
			}


			client.Headers["logonType"] = "2";

			data = client.DownloadString(url);
			if (data != null)
			{ }

			client.Headers["logonType"] = "3";

			data = client.DownloadString(url);
			if (data != null)
			{
				if (client.GetCookies() != null)
				{
					var cookie = client.GetCookies().GetCookies(new Uri(url));
					if (cookie != null)
					{
					}
				}
			}
		}
	}
}
