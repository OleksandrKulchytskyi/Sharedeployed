using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace ShareDeployed.Mailgrabber
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string AppId;

		protected override void OnStartup(StartupEventArgs e)
		{
			string rootPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string appDataFile = System.IO.Path.Combine(rootPath, "appData.data");
			if (System.IO.File.Exists(appDataFile))
			{
				using(var sr=new System.IO.StreamReader(appDataFile))
				{
					string line;
					while((line=sr.ReadLine())!=null)
					{
						if (line.IndexOf("AppId",  StringComparison.OrdinalIgnoreCase)!=-1)
						{
							AppId = line.Split('=')[1];
						}
					}
				}
			}
			else
			{
				AppId=Guid.NewGuid().ToString("d");
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
			base.OnExit(e);
		}
	}
}
