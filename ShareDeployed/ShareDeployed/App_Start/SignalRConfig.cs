﻿using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Web.Routing;

namespace ShareDeployed.App_Start
{
	public static class SignalRConfig
	{
		public static void Register(IDependencyResolver resolver)
		{
			var hubConfig = new HubConfiguration
			{
				Resolver = resolver,
				EnableJavaScriptProxies = true
			};
			var configuration = resolver.Resolve<IConfigurationManager>();

			RouteTable.Routes.MapHubs(hubConfig);

			var pipeline = resolver.Resolve<IHubPipeline>();
			configuration.ConnectionTimeout = TimeSpan.FromSeconds(30);
			configuration.DisconnectTimeout = TimeSpan.FromSeconds(30);
			
			//this was disappear since v 1.0,this feature is turned on by defult in v1.0 >
			//pipeline.EnableAutoRejoiningGroups();
			//RouteTable.Routes.MapHubs();
		}
	}
}