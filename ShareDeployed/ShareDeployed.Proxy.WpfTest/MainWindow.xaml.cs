using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShareDeployed.Proxy.WpfTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		dynamic _proxy;
		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
			this.Closing += MainWindow_Closing;
		}

		void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			(_proxy as DynamicProxy).Dispose();
		}

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_proxy = DynamicProxyPipeline.Instance.DynamixProxyManager.Get("customersProxy");
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<Customer> data = _proxy.GetCustomers();
			if (data != null)
			{
				dg1.ItemsSource = data;
			}
		}

		private void Button_ClickID(object sender, RoutedEventArgs e)
		{
			int id;
			if (int.TryParse(cId.Text, out id))
			{
				Customer cust = _proxy.GetById(id);
				if (cust != null)
				{
					dg1.ItemsSource = new List<Customer>() { cust };
				}
				else
				{
					dg1.ItemsSource = null;
					MessageBox.Show(this, "Fail to find such customer");
				}
			}
		}
	}
}
