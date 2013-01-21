using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Mailgrabber.Helpers;

namespace ShareDeployed.Test
{
	[TestClass]
	public class CookielessWebClientTest
	{
		readonly string url = "http://localhost:1212/handlers/loginhandler.ashx";

		[TestMethod]
		public void TestMethod1()
		{
			//ShareDeployed.Common.Helper.WebRequesthelper.Do(url);

			string json = "{\"userId\":\"e6057d27-1567-4073-9a6b-b6187cd76988\",\"aspUserId\":1,\"userName\":\"admin\",\"hash\":\"3ad4343af2de4ae5c8277fd1f5c81b57\",\"tokenId\":\"b0fc100a-bd7b-4c25-a635-91c3d3131aba\"}";
			byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
			string base64 = System.Convert.ToBase64String(jsonBytes);
			if (base64.Length < 0){ }

			var guid = Guid.NewGuid();
			System.Diagnostics.Debug.WriteLine(guid.ToString("N"));
			System.Diagnostics.Debug.WriteLine(guid.ToString("D"));
			System.Diagnostics.Debug.WriteLine(guid.ToString("B"));

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
