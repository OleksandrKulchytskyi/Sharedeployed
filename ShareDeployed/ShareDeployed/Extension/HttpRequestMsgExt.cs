using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace ShareDeployed.Extension
{
	public static class HttpRequestMsgExt
	{
		public static bool IsRequestLocal(this HttpRequestMessage msg)
		{
			var locFlag=msg.Properties["MS_IsLocal"] as Lazy<bool>;
			return (locFlag!=null && locFlag.Value);
		}
	}
}