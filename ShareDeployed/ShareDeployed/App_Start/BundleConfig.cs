using System.Web.Optimization;

namespace ShareDeployed
{
	public class BundleConfig
	{
		// For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Scripts/jquery-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/knockout").Include("~/Scripts/knockout-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include("~/Scripts/jquery-ui-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include("~/Scripts/jquery.unobtrusive*",
																		"~/Scripts/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include("~/Scripts/modernizr-*"));

			bundles.Add(new ScriptBundle("~/bundles/jquerybase64").Include("~/Scripts/base64/jquery.base64*"));

			bundles.Add(new ScriptBundle("~/bundles/app").Include("~/Scripts/app*"));
			bundles.Add(new ScriptBundle("~/bundles/singletonHub").Include(
										"~/Scripts/SingletonHub*",
										"~/Scripts/MessangerHub.js",
										"~/Scripts/MessangerVM.js"));

			bundles.Add(new ScriptBundle("~/messanger").Include("~/messanger.utils.js"));

			bundles.Add(new ScriptBundle("~/bundles/SignalR").Include("~/Scripts/jquery.signalR*"));
			bundles.Add(new ScriptBundle("~/bundles/cookie").Include("~/Scripts/jquery.cookie.js"));
			bundles.Add(new ScriptBundle("~/bundles/KoProtectedObservable").Include("~/Scripts/KO/ko-protected-observable.js"));

			bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/Site.css", "~/Content/items.css")); // ,"~/Content/jquery.mobile*"));

			bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
						"~/Content/themes/base/jquery.ui.core.css",
						"~/Content/themes/base/jquery.ui.resizable.css",
						"~/Content/themes/base/jquery.ui.selectable.css",
						"~/Content/themes/base/jquery.ui.accordion.css",
						"~/Content/themes/base/jquery.ui.autocomplete.css",
						"~/Content/themes/base/jquery.ui.button.css",
						"~/Content/themes/base/jquery.ui.dialog.css",
						"~/Content/themes/base/jquery.ui.slider.css",
						"~/Content/themes/base/jquery.ui.tabs.css",
						"~/Content/themes/base/jquery.ui.datepicker.css",
						"~/Content/themes/base/jquery.ui.progressbar.css",
						"~/Content/themes/base/jquery.ui.theme.css"));

			bundles.Add(new StyleBundle("~/Content/bootstrap").Include("~/Content/Bootstrap/bootstrap.min.css"));
			
			BundleTable.EnableOptimizations = true;
		}
	}
}