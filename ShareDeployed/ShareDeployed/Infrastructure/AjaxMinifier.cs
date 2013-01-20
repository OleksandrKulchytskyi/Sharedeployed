using Microsoft.AspNet.SignalR.Hubs;

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