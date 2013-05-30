using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;
using ShareDeployed.Proxy.IoC;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Diagnostics;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class IoCHarnessTest
	{
		private System.Threading.ManualResetEvent _event;

		[TestInitialize]
		public void OnInitialize()
		{
			_event = new System.Threading.ManualResetEvent(false);
			DynamicProxyPipeline.Instance.Initialize(true);
		}

		[TestCleanup]
		public void OnUninitialize()
		{
			_event.Dispose();
		}

		[TestMethod]
		public void HarnessTestMethod()
		{
			CancellationTokenSource cts = new CancellationTokenSource();

			Task[] tasks = new Task[4];
			tasks[0] = Task.Factory.StartNew(PerformAdd, cts.Token);
			tasks[1] = Task.Factory.StartNew(PerformRead, cts.Token);
			tasks[2] = Task.Factory.StartNew(RandomAccess, cts.Token);
			tasks[3] = Task.Factory.StartNew(PerformExit, cts, cts.Token);

			if (_event.WaitOne())
			{
				var objects = DynamicProxyPipeline.Instance.ContracResolver.ResolveAll(typeof(Customer));
				if (objects != null)
				{
					int len = objects.Count();
					Debug.WriteLine("Object count is:", len);
				}
			}
		}

		private void PerformAdd(object obj)
		{
			int count = 0;
			while (true)
			{
				typeof(Customer).BindToSelfWithAlias(count.ToString()).InPerThreadScope();
				Debug.WriteLine("Successfully added");

				Thread.Sleep(TimeSpan.FromSeconds(2));
				count++;
			}
		}

		private void PerformRead()
		{
			int count = 0;
			while (true)
			{
				Thread.Sleep(TimeSpan.FromSeconds(2.1));
				Customer cust = DynamicProxyPipeline.Instance.ContracResolver.Resolve<Customer>(count.ToString());
				if (cust != null)
				{
					cust.Id = count;
					cust.Name = count.ToString();
					Debug.WriteLine("Successfully retrieved");
					Debug.WriteLine(cust.Name);
				}
				else
				{
					Debug.WriteLine(string.Format("Fail to retrieve customer for alias {0}", count));
				}
				count++;
			}
		}

		private void RandomAccess()
		{
			Random r = new Random();
			while (true)
			{
				Thread.Sleep(TimeSpan.FromSeconds(2.02));
				int index = r.Next(0, 28);
				Customer cust = DynamicProxyPipeline.Instance.ContracResolver.Resolve<Customer>(index.ToString());
				if (cust == null)
					Debug.WriteLine(string.Format("[RandomAccess]Fail to retrieve customer by alias {0}", index));
			}
		}

		private void PerformExit(object data)
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			CancellationTokenSource cts = data as CancellationTokenSource;
			cts.Cancel();

			Thread.Sleep(TimeSpan.FromSeconds(3));
			_event.Set();
		}

		[TestMethod]
		public void IocHarnesTest2()
		{
			CancellationTokenSource cts = new CancellationTokenSource();
			typeof(Customer).BindToSelf().InPerThreadScope();

			Task[] tasks = new Task[3];
			tasks[0] = Task.Factory.StartNew(PerformGet, TaskCreationOptions.LongRunning);
			tasks[1] = Task.Factory.StartNew(PerformGet2, TaskCreationOptions.LongRunning);
			tasks[2] = Task.Factory.StartNew(PerformExit, cts);

			if (_event.WaitOne())
			{
				var objects = DynamicProxyPipeline.Instance.ContracResolver.ResolveAll(typeof(Customer));
				if (objects != null)
				{
				}
			}
		}

		private void PerformGet()
		{
			int i = 0;
			while (true)
			{
				Customer c = DynamicProxyPipeline.Instance.ContracResolver.Resolve<Customer>();
				if (c != null)
				{
					c.Id = i;
					c.Name = i.ToString();
					Debug.WriteLine(c.ToString());
				}
				else
					Debug.WriteLine("Fail to retrieve customer.");
				i++;
				Thread.Sleep(TimeSpan.FromMilliseconds(10));
			}
		}

		private void PerformGet2()
		{
			int i = 0;
			while (true)
			{
				Customer c = DynamicProxyPipeline.Instance.ContracResolver.Resolve<Customer>();
				if (c != null)
				{
					c.Id = i;
					c.Name = i.ToString();
					Debug.WriteLine(c.ToString());
				}
				else
					Debug.WriteLine("Fail to retrieve customer.");
				i++;
				Thread.Sleep(TimeSpan.FromMilliseconds(10));
			}
		}

		[TestMethod]
		public void ExpressionTest()
		{
			string url = DoTest(x => x.Name == "Alex");
			Assert.IsTrue(url.EndsWith("?Name=Alex"));

			Assert.IsTrue(DoTest(Do()).EndsWith("?Name=Alex"));

			string url2 = DoTest(x => x.Name == "Alex" && x.Id == 12);
			Assert.IsTrue(url.EndsWith("?Name=AlexId=12"));
		}

		private string DoTest(Expression<Func<Customer, bool>> expr)
		{
			string mainUrl = "http://somesite/customer.aspx?";
			BinaryExpression binExpr = (BinaryExpression)expr.Body;
			MemberExpression left = (MemberExpression)binExpr.Left;
			mainUrl += left.Member.Name;
			if (binExpr.NodeType == ExpressionType.Equal)
				mainUrl += "=";
			else
				throw new NotSupportedException("Only =");

			ConstantExpression cons = (ConstantExpression)binExpr.Right;
			mainUrl += cons.Value;
			return mainUrl;
		}

		private Expression<Func<Customer, bool>> Do()
		{
			ParameterExpression paramExp = Expression.Parameter(typeof(Customer), "x");
			MemberExpression leftExp = MemberExpression.Property(paramExp, "Name");
			Expression rightExpression = Expression.Constant("Alex");

			BinaryExpression exp = MemberExpression.Equal(leftExp, rightExpression);
			Expression<Func<Customer, bool>> lambdaExpr = Expression.Lambda<Func<Customer, bool>>(
															exp, new ParameterExpression[] { paramExp });
			return lambdaExpr;
		}
	}

	public class Customer
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public override string ToString()
		{
			return Id + Name ?? string.Empty;
		}
	}
}
