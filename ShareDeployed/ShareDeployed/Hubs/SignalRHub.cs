using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.Hubs
{
	public abstract class SignalRBase<T> where T : Hub
	{
		private Lazy<IHubContext> hubLazy = new Lazy<IHubContext>(() =>
												Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<T>());

		public IHubContext HubInstance
		{
			get { return hubLazy.Value; }
		}
	}
}