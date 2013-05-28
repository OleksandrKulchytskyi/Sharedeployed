using System.Windows;
using System.Windows.Threading;

namespace ShareDeployed.Proxy.WpfTest
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			DynamicProxyPipeline.Instance.Configure();
			DynamicProxyPipeline.Instance.ContracResolver.ResolveFailed+=ContracResolver_ResolveFailed;
		}

		private void ContracResolver_ResolveFailed(object sender, ResolutionFailEventArgs e)
		{
			DynamicProxyPipeline.Instance.LoggerAggregator.DoLog(Logging.LogSeverity.Fatal, e.ErrorMessage, e.ResolutionError);
		}

		void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			DynamicProxyPipeline.Instance.LoggerAggregator.DoLog(Logging.LogSeverity.Error, e.Exception.Message, e.Exception);
		}
	}
}