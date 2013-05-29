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
		private dynamic _proxy;
		private volatile bool _exit = false;
		private System.Threading.Thread[] threads;

		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
			this.Closing += MainWindow_Closing;
		}

		void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_exit = true;
			if (threads != null)
			{
				for (int i = 0; i < threads.Length; i++)
				{
					threads[i].Join();
				}
			}
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

		private void Button_ClickName(object sender, RoutedEventArgs e)
		{
			Customer cust = _proxy.GetByName(cName.Text);
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

		private void Button_ClickFailResolve(object sender, RoutedEventArgs e)
		{
			DynamicProxyPipeline.Instance.ContracResolver.Resolve<MainWindow>();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if (threads != null)
				return;
			threads = new System.Threading.Thread[4];
			threads[0] = new System.Threading.Thread(Dowork1);
			threads[1] = new System.Threading.Thread(Dowork2);
			threads[2] = new System.Threading.Thread(Dowork3);
			threads[3] = new System.Threading.Thread(Dowork4);

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].IsBackground = true;
				threads[i].Start();
			}
		}

		private void Dowork1(object obj)
		{
			while (!_exit)
			{
				System.Threading.Thread.Sleep(100);
				IList<Customer> customers = _proxy.GetCustomers();
				if (customers == null)
					throw new ArgumentException();
			}
		}

		private void Dowork2(object obj)
		{
			while (!_exit)
			{
				System.Threading.Thread.Sleep(100);
				Customer cust = _proxy.GetById(1);
				if (cust == null)
					throw new ArgumentException();
			}
		}

		private void Dowork3(object obj)
		{
			while (!_exit)
			{
				System.Threading.Thread.Sleep(100);
				Customer cust = _proxy.GetByName("Alex");
				if (cust == null)
					throw new ArgumentException();
			}
		}

		private void Dowork4(object obj)
		{
			while (!_exit)
			{
				System.Threading.Thread.Sleep(100);
				Customer cust = _proxy.GetById(45);
				if (cust != null)
					throw new ArgumentException();
			}
		}
	}
}
