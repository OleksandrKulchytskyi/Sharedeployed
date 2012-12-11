using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.Infrastructure
{
	public class AjaxMinMinifier : IJavaScriptMinifier
	{
		public string Minify(string source)
		{
			return new Microsoft.Ajax.Utilities.Minifier().MinifyJavaScript(source);
		}
	}
}