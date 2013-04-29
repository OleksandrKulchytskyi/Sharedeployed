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
			//Since method above in case on error will eat exception 
			//it must return default value as a result
			Assert.IsTrue(data == 0);
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
					interf.DoWork();
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
					interf.DoWork();
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
			void DoWork();
			int DoWork2(int data);
			int DoWork3(int data, int data2);
			int DoWorkErrored(int data);
		}

		class ErrorProneAbstracted : IDoWork
		{
			public int Default { get; set; }

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorInjectionMode.OnError)]
			public void DoWork()
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
			public int DoWork2(int data)
			{
				int i;
				if (Default == 0)
					i = 5;
				else
					i = Default;
				i = i + data;
				return i;
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorInjectionMode.OnError)]
			public int DoWork3(int data, int data2)
			{
				return data + data2;
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

		public class PropertyHolder
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

			Debug.WriteLine((ticksReflection / ticksFast));
			Assert.IsTrue(ticksFast < ticksReflection);
		}


		[TestMethod]
		public void MethodCallingPerformance()
		{
			int iter = 5000;
			var obj1 = new ErrorProneAbstracted();
			var obj2 = new ErrorProneAbstracted();
			dynamic proxy1 = new DynamicProxy(obj1, true);
			dynamic proxy2 = new DynamicProxy(obj2, true, true);
			int result = 1;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < iter; i++)
			{
				proxy1.DoWork();
				result = proxy1.DoWork2(12);
				result = proxy1.DoWork3(12, 123);
			}
			sw.Stop();
			long tick1 = sw.ElapsedMilliseconds;
			sw.Reset();

			sw.Start();
			for (int i = 0; i < iter; i++)
			{
				proxy2.DoWork();
				result = proxy2.DoWork2(12);
				result = proxy2.DoWork3(12, 123);
			}
			sw.Stop();
			long tick2 = sw.ElapsedMilliseconds;

			Debug.WriteLine("{0} - {1} Diff: {2}", tick1, tick2, tick1 - tick2);
			Assert.IsTrue(tick1 > tick2);
		}

		[TestMethod]
		public void DynamicConversionTest()
		{
			var obj1 = new ErrorProneAbstracted();
			dynamic proxy1 = new DynamicProxy(obj1, true);

			ErrorProneAbstracted abst = proxy1;
			Assert.IsNotNull(abst);
		}

		[TestMethod]
		public void DynamicProxyFactoryTest()
		{
			var obj1 = new ErrorProneAbstracted();
			dynamic proxy = DynamicProxyFactory.CreateDynamicProxy(obj1);
			Assert.IsTrue(proxy.Default == 0);
		}

		[TestMethod]
		public void PropertyAccessorPerformanceTest()
		{
			int loopCount = 5000;
			PropertyHolder ph = new PropertyHolder();
			ShareDeployed.Common.Proxy.FastReflection.PropertyAccessor pa = new Common.Proxy.FastReflection.PropertyAccessor(typeof(PropertyHolder), "Id");
			System.Reflection.PropertyInfo pi = typeof(PropertyHolder).GetProperty("Id");
			ShareDeployed.Common.Proxy.FastReflection.FastProperty fp = new Common.Proxy.FastReflection.FastProperty(pi);


			Stopwatch sw = new Stopwatch();
			sw.Start();
			int id = 0;
			for (int i = 1; i <= loopCount; i++)
			{
				ph.Id = i;
				id = ph.Id;
			}
			sw.Stop();
			long directTime = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				pi.SetValue(ph, i, null);
				id = (int)pi.GetValue(ph, null);
			}
			sw.Stop();
			long reflTime = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				pa.Set(ph, i);
				id = (int)pa.Get(ph);
			}
			sw.Stop();
			long paTime = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				fp.Set(ph, i);
				id = (int)fp.Get(ph);
			}
			sw.Stop();
			long fpTime = sw.ElapsedMilliseconds;

			Debug.WriteLine(string.Format("Difference is:{0},{1},{2},{3}", directTime, reflTime, paTime, fpTime));
			Assert.IsTrue(directTime < paTime && paTime > fpTime && fpTime < reflTime);
		}
	}
}
