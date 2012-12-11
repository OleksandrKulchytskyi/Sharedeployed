using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareDeployed.RoutingHelper
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class HttpRouteAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the URI template for the route.
		/// For example, api/events/{eventId}/speakers.
		/// </summary>
		public string UriTemplate { get; set; }

		/// <summary>
		/// Gets or sets whether the routing engine should route existing files through the routing system.
		/// When you set this flag to false, the existing files are served as-is. Otherwise the regular
		/// routes will be handled first and the file may never be served at all.
		/// </summary>
		public bool RouteExistingFiles { get; set; }
	}
}
