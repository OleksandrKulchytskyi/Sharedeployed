using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy;
using System.Diagnostics;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicProxyUnitTest
	{
		private int disposed = -1;

		[TestInitializeAttribute()]
		public void OnInit()
		{
			IPipeline pipeline = DynamicProxyPipeline.Instance;
			if (pipeline != null)
				pipeline.Initialize(true);
		}

		private class ErrorProneForProxy
		{
			public int DefaultData { get; set; }

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.Before)]
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

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = false, Mode = InterceptorMode.OnError)]
			public int DoWorkReturnError(int add)
			{
				int i = 5;
				i = i + add;
				throw new InvalidOperationException("Something is going wrong.");
				return i;
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = false, Mode = InterceptorMode.OnError)]
			public void DoWorkError(int add)
			{
				int i = 5;
				i = i + add;
				Console.WriteLine(i);
				throw new InvalidOperationException("Something is going wrong.");
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
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

			Assert.IsTrue(TypePropertyMapper.Instance.Get(real.GetType().GetHashCode(), "Default") != null);
		}

		[TestMethod]
		public void FastFieldGetSetTest()
		{
			ErrorProneAbstracted objInstance = new ErrorProneAbstracted();
			System.Reflection.FieldInfo fi = objInstance.GetType().GetField("Id");
			Proxy.FastReflection.FastField ff = new Proxy.FastReflection.FastField(fi);
			Proxy.FastReflection.FastField<ErrorProneAbstracted, int> ff3 = new Proxy.FastReflection.FastField<ErrorProneAbstracted, int>(fi);
			var dynField = Proxy.FastReflection.DynamicField.Create(fi);

			int id = -1;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			ff.Set(objInstance, 12);
			id = (int)ff.Get(objInstance);
			sw.Stop();

			long fastElapsed = sw.ElapsedTicks;
			Assert.IsTrue(id == 12);

			sw.Reset();
			sw.Start();
			fi.SetValue(objInstance, 13);
			id = (int)fi.GetValue(objInstance);
			sw.Stop();
			long reflectElapsed = sw.ElapsedTicks;
			Assert.IsTrue(id == 13);

			sw.Reset();
			sw.Start();
			dynField.SetValue(objInstance, 134);
			id = (int)dynField.GetValue(objInstance);
			sw.Stop();
			long dynElapsed = sw.ElapsedTicks;
			Assert.IsTrue(id == 134);

			sw.Reset();
			sw.Start();
			ff3.Set(objInstance, 17);
			id = ff3.Get(objInstance);
			sw.Stop();

			long fastGenElapsed = sw.ElapsedTicks;
			Assert.IsTrue(id == 17);

			Debug.WriteLine("FastField {0},FastField generic {1}, Reflection {2}, DynamicField {3}", fastElapsed, fastGenElapsed, reflectElapsed, dynElapsed);
			Assert.IsTrue(reflectElapsed > fastElapsed);
		}

		interface IDoWork
		{
			int Default { get; set; }
			void DoWork();
			int DoWork2(int data);
			int DoWork3(int data, int data2);
			int DoWorkErrored(int data);
		}

		[Interceptor(InterceptorType = typeof(AfterMethodExecutedInterceptor), Mode = InterceptorMode.After)]
		class ErrorProneAbstracted : IDoWork
		{
			public int Id = -1;

			public int Default { get; set; }

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
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

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
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

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
			public int DoWork3(int data, int data2)
			{
				return data + data2;
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
			public int DoWork3(string data, string data2)
			{
				int i, i2;
				if (Int32.TryParse(data, out i) && Int32.TryParse(data2, out i2))
					return i + i2;
				else
					throw new AggregateException();
			}

			[Interceptor(InterceptorType = typeof(ExceptionInterceptor), EatException = true, Mode = InterceptorMode.OnError)]
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
			int loopCount = 1000;
			PropertyHolder ph = new PropertyHolder();
			//the slowest one
			Proxy.FastReflection.PropertyAccessor pa = new Proxy.FastReflection.PropertyAccessor(typeof(PropertyHolder), "Id");
			System.Reflection.PropertyInfo pi = typeof(PropertyHolder).GetProperty("Id");
			ShareDeployed.Proxy.FastReflection.FastProperty fp = new ShareDeployed.Proxy.FastReflection.FastProperty(pi);
			ShareDeployed.Proxy.FastReflection.FastProperty<PropertyHolder> fp2 = new Proxy.FastReflection.FastProperty<PropertyHolder>(pi);
			ShareDeployed.Proxy.FastReflection.FastProperty<PropertyHolder, int> fp3 = new Proxy.FastReflection.FastProperty<PropertyHolder, int>(pi);

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

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				fp2.Set(ph, i);
				id = (int)fp2.Get(ph);
			}
			sw.Stop();
			long fpTime2 = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				fp3.Set(ph, i);
				id = fp3.Get(ph);
			}
			sw.Stop();
			long fpTime3 = sw.ElapsedMilliseconds;

			Debug.WriteLine(string.Format("Difference is:{0},{1},{2},{3},{4},{5}", directTime, reflTime, paTime, fpTime, fpTime2, fpTime3));
			Assert.IsTrue(fpTime3 < fpTime && fpTime2 < fpTime);
			Assert.IsTrue(directTime < paTime && paTime > fpTime && fpTime3 < reflTime);
		}

		[TestMethod]
		public void FieldAccessorPerformanceTest()
		{
			int loopCount = 100;
			ErrorProneAbstracted instance = new ErrorProneAbstracted();
			System.Reflection.FieldInfo fi = typeof(ErrorProneAbstracted).GetField("Id");
			ShareDeployed.Proxy.FastReflection.FastField ff = new Proxy.FastReflection.FastField(fi);
			ShareDeployed.Proxy.FastReflection.FastField<ErrorProneAbstracted> ff2 = new Proxy.FastReflection.FastField<ErrorProneAbstracted>(fi);
			ShareDeployed.Proxy.FastReflection.FastField<ErrorProneAbstracted, int> ff3 = new Proxy.FastReflection.FastField<ErrorProneAbstracted, int>(fi);
			var dynField = ShareDeployed.Proxy.FastReflection.DynamicField.Create(fi);

			Stopwatch sw = new Stopwatch();
			sw.Start();
			int id = 0;
			for (int i = 1; i <= loopCount; i++)
			{
				fi.SetValue(instance, 12);
				id = (int)fi.GetValue(instance);
			}
			sw.Stop();
			long reflTime = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				ff.Set(instance, i);
				id = (int)ff.Get(instance);
			}
			sw.Stop();
			long fastFieldTime = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				ff2.Set(instance, i);
				id = (int)ff2.Get(instance);
			}
			sw.Stop();
			long fastFieldTime2 = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				ff3.Set(instance, i);
				id = ff3.Get(instance);
			}
			sw.Stop();
			long fastFieldTime3 = sw.ElapsedMilliseconds;

			sw.Reset();
			sw.Start();
			for (int i = 1; i <= loopCount; i++)
			{
				dynField.SetValue(instance, 12);
				id = (int)dynField.GetValue(instance);
			}
			sw.Stop();
			long dynFieldTime = sw.ElapsedMilliseconds;

			Debug.WriteLine(string.Format("Difference is:{0},{1},{2},{3},{4}", reflTime, fastFieldTime, fastFieldTime2, fastFieldTime3, dynFieldTime));
			Assert.IsTrue(fastFieldTime3 < fastFieldTime2 && fastFieldTime2 < fastFieldTime);
			Assert.IsTrue(dynFieldTime < reflTime && fastFieldTime < reflTime);
		}

		[TestMethod]
		public void GetPropertiesPerformanceTest()
		{
			ManyPropClass cls = new ManyPropClass();
			Type clsType = cls.GetType();

			Stopwatch sw = new Stopwatch();
			sw.Start();
			var prop = System.ComponentModel.TypeDescriptor.GetProperties(cls);
			sw.Stop();
			long pdTime = sw.ElapsedTicks;

			sw.Reset();
			sw.Start();
			System.Reflection.PropertyInfo[] pis = clsType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			sw.Stop();
			long reflTime = sw.ElapsedTicks;

			Assert.IsTrue(pdTime > reflTime);

		}

		private class ManyPropClass
		{
			public int Id { get; set; }
			public string Name { get; set; }

			public long Some { get; set; }

			public char Sex { get; set; }
		}

		[TestMethod]
		public void TestCaseWithOverridedMethod()
		{
			dynamic proxy = new DynamicProxy(new ErrorProneAbstracted());
			dynamic proxy2 = new DynamicProxy(new ErrorProneAbstracted());

			bool equals = (proxy == proxy2);
			
			int result = proxy.DoWork3(1, 2);
			int result2 = proxy.DoWork3("1", "2");
			Assert.IsTrue(result == result2);
		}
	}

	public class PropertyHolder
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool Old { get; set; }

		public string GetName()
		{
			return Name;
		}
	}
}
