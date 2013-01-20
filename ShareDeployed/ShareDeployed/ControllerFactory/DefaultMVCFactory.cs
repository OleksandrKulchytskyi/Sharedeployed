﻿using System;
using System.Web.Mvc;

namespace ShareDeployed.ControllerFactory
{
	public class DefaultMVCFactory : DefaultControllerFactory
	{
		private static object locker;

		static DefaultMVCFactory()
		{
			locker = new object();
		}

		public override IController CreateController(System.Web.Routing.RequestContext requestContext, string controllerName)
		{
			lock (locker)
			{
				if (requestContext.RouteData.Values.Count == 2)
				{
					if ((requestContext.RouteData.Values["controller"] as string).Equals("signalr", StringComparison.OrdinalIgnoreCase))
						return null;
				}
				var controller = base.CreateController(requestContext, controllerName);
				return controller;
			}
		}
	}
}