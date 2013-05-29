using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.WpfTest
{
	public interface ICustomerService
	{
		IEnumerable<Customer> GetCustomers();
		Customer GetById(int id);
		Customer GetByName(string name);
	}

	public sealed class CustomerService : ICustomerService
	{
		private static List<Customer> _customers;

		static CustomerService()
		{
			_customers = new List<Customer>();
			_customers.Add(new Customer(1, "Alex"));
			_customers.Add(new Customer(2, "Boris"));
			_customers.Add(new Customer(3, "Ivan"));
			_customers.Add(new Customer(4, "Delv"));
		}

		public CustomerService()
		{
		}

		[Interceptor(Mode = InterceptorMode.Before, InterceptorType = typeof(BeforeMethodExecutesInterceptor))]
		[Interceptor(EatException = true, Mode = InterceptorMode.OnError, InterceptorType = typeof(ExceptionInterceptor))]
		public IEnumerable<Customer> GetCustomers()
		{
			return _customers;
		}

		[Interceptor(EatException = true, Mode = InterceptorMode.OnError, InterceptorType = typeof(ExceptionInterceptor))]
		public Customer GetById(int id)
		{
			return (from x in _customers where x.Id == id select x).First();
		}
		[Interceptor(EatException = true, Mode = InterceptorMode.OnError, InterceptorType = typeof(ExceptionInterceptor))]
		public Customer GetByName(string name)
		{
			return (from x in _customers where x.Name.Equals(name) select x).First();
		}
	}
}
