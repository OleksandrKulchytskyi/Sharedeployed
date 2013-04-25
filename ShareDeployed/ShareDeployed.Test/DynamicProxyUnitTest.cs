using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy;
using System.Diagnostics;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyUnitTest
	{
		private int disposed = -1;

		private class ErrorProneForProxy
		{
			public int DefaultData { get; set; }

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorInjectionMode.Before)]
			public int DoWorkNonError(int add)
			{
				int i;
				if (DefaultData == 0)
					i = 5;
				else
					i = DefaultData;
				i = i + add;
				return i;
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = false, Mode = InterceptorInjectionMode.OnError)]
			public int DoWorkReturnError(int add)
			{
				int i = 5;
				i = i + add;
				throw new InvalidOperationException("Something is going wrong.");
				return i;
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = false, Mode = InterceptorInjectionMode.OnError)]
			public void DoWorkError(int add)
			{
				int i = 5;
				i = i + add;
				Console.WriteLine(i);
				throw new InvalidOperationException("Something is going wrong.");
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorInjectionMode.OnError)]
			public int DoWorkErrorNoThrow(int add)
			{
				int i = 5;
				i = i + add;
				Console.WriteLine(i);
				throw new InvalidOperationException("Something is going wrong.");
				return i;
			}
		}

		[TestMethod]
		[ExpectedException(typeof(System.Reflection.TargetInvocationException))]
		public void TestMethodThrowError()
		{
			var real = new ErrorProneForProxy();
			dynamic proxy = new DynamicProxy(real);
			int data = proxy.DoWorkNonError(15);
			Assert.IsTrue(data == 20);

			proxy.DefaultData = 15;
			data = proxy.DoWorkNonError(15);
			Assert.IsTrue(data == 30);

			data = proxy.DoWorkReturnError(15);
			Assert.IsTrue(data == 20);
		}

		[TestMethod]
		public void TestMethodDoNotThrowError()
		{
			var real = new ErrorProneForProxy();
			dynamic proxy = new DynamicProxy(real);
			int data = proxy.DoWorkNonError(15);
			Assert.IsTrue(data == 20);

			proxy.DefaultData = 15;
			data = proxy.DoWorkNonError(15);
			Assert.IsTrue(data == 30);

			data = -1;
			data = proxy.DoWorkErrorNoThrow(15);
			Assert.IsTrue(data == -1);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void DynamicProxyCastOperationTest()
		{
			if (System.Threading.Interlocked.CompareExchange(ref this.disposed, 1, -1) == -1)
			{
				var real = new ErrorProneAbstracted();
				dynamic proxy = new DynamicProxy(real);
				IDoWork interf = proxy;
				if (interf != null)
				{
					interf.DoWrok();
					Assert.IsTrue(interf.Default == 0);
					interf.Default = 65;
					interf.DoWorkErrored(12);
				}
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void AdvancedDynamicProxyCastOperationTest()
		{
			if (System.Threading.Interlocked.CompareExchange(ref this.disposed, 1, -1) == -1)
			{
				var real = new ErrorProneAbstracted();
				AdvancedDynamicProxy proxy = new AdvancedDynamicProxy(real);
				IDoWork interf = proxy.GetAbstraction<IDoWork>();
				if (interf != null)
				{
					interf.DoWrok();
					Assert.IsTrue(interf.Default == 0);
					interf.Default = 65;
					interf.DoWorkErrored(12);
				}
			}
		}

		[TestMethod]
		public void FastPropertyTest()
		{
			var real = new ErrorProneAbstracted();
			dynamic proxy = new DynamicProxy(real);

			Assert.IsTrue(proxy.Default == 0);
			proxy.Default = 23;
			Assert.IsTrue(proxy.Default == 23);

			Assert.IsTrue(TypePropertyMapper.Instance.Get(real.GetType(), "Default") != null);
		}

		interface IDoWork
		{
			int Default { get; set; }
			void DoWrok();
			int DoWorkErrored(int data);
		}
		class ErrorProneAbstracted : IDoWork
		{
			public int Default { get; set; }

			public void DoWrok()
			{
				int i;
				if (Default == 0)
					i = 5;
				else
					i = Default;
				i = i + 12;
				Console.WriteLine(i);
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorInjectionMode.OnError)]
			public int DoWorkErrored(int data)
			{
				int i;
				if (Default == 0)
					i = 5;
				else
					i = Default;
				i = i + 12;
				Console.WriteLine(i);
				throw new InvalidOperationException("Simulating exception.");
				return i;
			}
		}

		class PropertyHolder
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public bool Old { get; set; }
		}

		[TestMethod]
		public void DynamicProxyPropertyPerformanceSingleTest()
		{
			var holder1 = new PropertyHolder();
			var holder2 = new PropertyHolder();
			dynamic proxy = new DynamicProxy(holder1);
			dynamic proxy2 = new DynamicProxy(holder2, false);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			proxy.Id = 12;
			proxy.Name = "Test";
			proxy.Old = true;
			sw.Stop();
			long ticksFast = sw.ElapsedTicks;
			sw.Reset();
			sw.Start();
			proxy2.Id = 12;
			proxy2.Name = "Test";
			proxy2.Old = true;
			sw.Stop();
			long ticksReflection = sw.ElapsedTicks;

			Debug.WriteLine((ticksFast / ticksReflection));
			Assert.IsTrue(ticksFast > ticksReflection);
		}

		[TestMethod]
		public void DynamicProxyPropertyPerformanceMultipleTest()
		{
			var holder1 = new PropertyHolder();
			var holder2 = new PropertyHolder();
			dynamic proxy = new DynamicProxy(holder1);
			dynamic proxy2 = new DynamicProxy(holder2, false);
			int iterCount = 10000;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			for (int i = 1; i <= iterCount; i++)
			{
				proxy.Id = i;
				proxy.Name = "Test";
				proxy.Old = true;
			}
			sw.Stop();
			long ticksFast = sw.ElapsedTicks;
			sw.Reset();
			sw.Start();
			for (int i = 1; i <= iterCount; i++)
			{
				proxy2.Id = i;
				proxy2.Name = "Test";
				proxy2.Old = true;
			}
			sw.Stop();
			long ticksReflection = sw.ElapsedTicks;

			Debug.WriteLine((ticksFast / ticksReflection));
			Assert.IsTrue(ticksFast < ticksReflection);
		}
	}
}
