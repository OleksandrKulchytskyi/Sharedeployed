/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:ShareDeployed.Mailgrabber"
                           x:Key="Locator" />
  </Application.Resources>
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using Microsoft.Practices.ServiceLocation;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	/// <summary>
	/// This class contains static references to all the view models in the
	/// application and provides an entry point for the bindings.
	/// </summary>
	public class ViewModelLocator
	{
		internal static ILog Logger = null;
		private bool wasSubscribedOnUnhandledEvent = false;

		/// <summary>
		/// Initializes a new instance of the ViewModelLocator class.
		/// </summary>
		public ViewModelLocator()
		{
			InitializeLogger();

			ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

			////if (ViewModelBase.IsInDesignModeStatic)
			////{
			////    // Create design time view services and models
			////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
			////}
			////else
			////{
			////    // Create run time view services and models
			////    SimpleIoc.Default.Register<IDataService, DataService>();
			////}

			SimpleIoc.Default.Register<MainViewModel>();
			SimpleIoc.Default.Register<LoginVM>();
			SimpleIoc.Default.Register<CreateGroupVM>();
			SimpleIoc.Default.Register<GroupManageVM>();
		}

		private void InitializeLogger()
		{
			Logger = LogManager.GetLogger(typeof(ViewModelLocator).FullName);
			log4net.Config.XmlConfigurator.Configure();
			Logger.Info("Application_Start()");

			App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
			wasSubscribedOnUnhandledEvent = true;
		}

		void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			Logger.Fatal("DispatcherUnhandledException", e.Exception);
		}

		public MainViewModel Main
		{
			get
			{
				return ServiceLocator.Current.GetInstance<MainViewModel>();
			}
		}

		public LoginVM LoginVM
		{
			get
			{
				return ServiceLocator.Current.GetInstance<LoginVM>();
			}
		}

		public CreateGroupVM CreateGroupVM
		{
			get
			{
				return ServiceLocator.Current.GetInstance<CreateGroupVM>();
			}
		}

		public GroupManageVM GroupManageVM 
		{
			get
			{
				return ServiceLocator.Current.GetInstance<GroupManageVM>();
			}
		}

		public void Cleanup()
		{
			if (wasSubscribedOnUnhandledEvent)
				App.Current.DispatcherUnhandledException -= Current_DispatcherUnhandledException;
			
			if (Logger != null)
			{
				Logger.Info("Application_End()");
				LogManager.Shutdown();
			}

			foreach (var prop in typeof(ViewModelLocator).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
			{
				if (prop.PropertyType.BaseType.Equals(typeof(ViewModelBase)))
				{
					object value = prop.GetValue(this, new object[] { });
					(value as ViewModelBase).Cleanup();
				}
			}
			SimpleIoc.Default.Reset();
		}
	}
}