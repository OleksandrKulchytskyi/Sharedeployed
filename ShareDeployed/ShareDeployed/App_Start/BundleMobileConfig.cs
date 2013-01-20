using System.Web.Optimization;

namespace ShareDeployed
{
	public class BundleMobileConfig
	{
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundles/jquerymobile").Include("~/Scripts/jquery.mobile-{version}.js"));

			bundles.Add(new StyleBundle("~/Content/Mobile/css").Include("~/Content/Site.Mobile.css"));

			bundles.Add(new StyleBundle("~/Content/jquerymobile/css").Include("~/Content/jquery.mobile-{version}.css"));

			// Datebox section
			bundles.Add(new StyleBundle("~/Content/Mobile/datebox").Include("~/Content/datebox/jqm-datebox-1.1.0.min.css"));

			bundles.Add(new ScriptBundle("~/jquerymobile/datebox").Include(
										"~/Scripts/datebox/jqm-datebox-1.1.0.core.js",
										"~/Scripts/datebox/jqm-datebox-1.1.0.mode.datebox.js"));

			BundleTable.EnableOptimizations = true;
		}
	}
}