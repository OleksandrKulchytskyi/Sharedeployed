using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.Infrastructure
{
	public static class Constants
	{
		public static readonly string UserTokenCookie = "msngr.userToken";
		public static readonly Version MessangeRVersion = typeof(Constants).Assembly.GetName().Version;
		public static readonly string MessangeRAuthType = "MsngR";
	}
}