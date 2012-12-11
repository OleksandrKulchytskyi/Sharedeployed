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
		protected override void OnExit(ExitEventArgs e)
		{
			object data = this.FindResource("Locator");
			if(data!=null)
			{
				(data as ViewModel.ViewModelLocator).Cleanup();
			}
			base.OnExit(e);
		}
	}
}
