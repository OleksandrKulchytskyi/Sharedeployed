using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using ShareDeployed.Mailgrabber.Helpers;
namespace ShareDeployed.Mailgrabber
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string AppId;
		public static string AppMsgsLinkPath;
		List<Exception> exList = null;

		public App()
		{
			exList = new List<Exception>();
			this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
		}

		void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			if (ViewModel.ViewModelLocator.Logger != null)
			{
				if (exList.Count > 0)
				{
					foreach (var exc in exList)
						ViewModel.ViewModelLocator.Logger.Fatal("DispatcherUnhandledException", e.Exception);
					exList.Clear();
				}

				ViewModel.ViewModelLocator.Logger.Fatal("DispatcherUnhandledException", e.Exception);
			}
			else
				exList.Add(e.Exception); ;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			string rootPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string appDataFile = System.IO.Path.Combine(rootPath, "appData.data");
			AppMsgsLinkPath = System.IO.Path.Combine(rootPath, "msgLinks.data");

			//load messages links 
			Infrastructure.LinkManager.Instance.LoadFromFileAsync(AppMsgsLinkPath);

			if (System.IO.File.Exists(appDataFile))
			{
				using (var sr = new System.IO.StreamReader(appDataFile))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						if (line.IndexOf("AppId", StringComparison.OrdinalIgnoreCase) != -1)
						{
							AppId = line.Split('=')[1];
							break;
						}
					}
				}
			}
			else
			{
				AppId = Guid.NewGuid().ToString("d");
				System.IO.File.WriteAllText(appDataFile, string.Format("AppId={0}", AppId));
			}

			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			object data = this.FindResource("Locator");
			if (data != null)
			{
				(data as ViewModel.ViewModelLocator).Cleanup();
			}

			exList.Clear();
			exList = null;

			this.DispatcherUnhandledException -= App_DispatcherUnhandledException;
			base.OnExit(e);
		}
	}
}
