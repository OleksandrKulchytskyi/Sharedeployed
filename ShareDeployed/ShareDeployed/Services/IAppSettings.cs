using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ShareDeployed.Services
{
	public interface IAppSettings
	{
		string AuthApiKey { get; }

		string DefaultAdminUserName { get; }

		string DefaultAdminPassword { get; }
		string AuthAppId { get; }
	}

	public class AppSettings : IAppSettings
	{
		public string AuthApiKey
		{
			get
			{
				return ConfigurationManager.AppSettings["auth.apiKey"];
			}
		}

		public string DefaultAdminUserName
		{
			get
			{
				return ConfigurationManager.AppSettings["defaultAdminUserName"];
			}
		}

		public string DefaultAdminPassword
		{
			get
			{
				return ConfigurationManager.AppSettings["defaultAdminPassword"];
			}
		}


		public string AuthAppId
		{
			get
			{
				return ConfigurationManager.AppSettings["auth.appId"];
			}
		}
	}
}