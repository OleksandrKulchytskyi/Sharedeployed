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
		}

		void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			DynamicProxyPipeline.Instance.LoggerAggregator.DoLog(Logging.LogSeverity.Error, e.Exception.Message, e.Exception);
		}
	}
}