using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting.Common;
using Microsoft.AspNet.SignalR.Hubs;
using ShareDeployed.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.App_Start
{
	public static class SignalRConfig
	{
		public static void Register(IDependencyResolver resolver)
		{
			var host = new Host(resolver);
			host.Configuration.KeepAlive = TimeSpan.FromSeconds(30);
			// Make connections wait 50s maximum for any response. After
			// 50s are up, trigger a timeout command and make the client reconnect.
			host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(50);
			host.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(50);

			if (resolver == null)
			{
				GlobalHost.DependencyResolver.Register(typeof(IJavaScriptMinifier), () => new AjaxMinMinifier());
				System.Web.Routing.RouteTable.Routes.MapHubs();
			}
			else
			{
				GlobalHost.DependencyResolver = resolver;
				System.Web.Routing.RouteTable.Routes.MapHubs(resolver);
			}
		}
	}
}