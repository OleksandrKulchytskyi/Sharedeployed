﻿using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy;
using Microsoft.CSharp.RuntimeBinder;

namespace ShareDeployed.Test
{
	[TestClass]
	public class DynamicAssemblyUnitTest
	{
		[TestMethod]
		public void DynamicAssemblyTest()
		{
			var builder = DynamicAssemblyBuilder.Create("TestAssembly");
			Assert.IsNotNull(builder);
		}

		[TestMethod]
		public void InterfaceObjectFactoryTest()
		{
			ITestInterface1 interf = InterfaceObjectFactory.New<ITestInterface1>();
			Assert.IsTrue((interf is ITestInterface1), "Wrong interface generated");
		}

		[TestMethod]
		public void DynamicProxyTest()
		{
			dynamic dp = new DynamicProxy(new DynamicTestData());
			dp.DoStuff();
		}

		[TestMethod]
		public void DynamicProxyMapperTest()
		{
			dynamic dp = new DynamicProxy(new DynamicTestData());
			dp.DoStuff();

			dynamic dp2 = new DynamicProxy(new DynamicTestData());
			dp2.DoStuff();
		}

		[TestMethod]
		[ExpectedException(typeof(RuntimeBinderException))]
		public void DynamicProxyErrorTest()
		{
			dynamic dp = new DynamicProxy(new DynamicTestData());
			dp.DoNonExistentMethod();
		}

		[TestMethod]
		public void DynamicProxyFieldAccessTest()
		{
			dynamic dp = new DynamicProxy(new DynamicTestData());
			dp.data = "12";
			dp.Id = 12;
			Console.WriteLine(dp.Id);
			dp.WriteDataValue();
		}

		[TestMethod]
		public void DynamicProxyExceptionIntercepterTest()
		{
			dynamic dp = new DynamicProxy(new ErrorProneClass());
			dp.data = "12";
			dp.Id = 12;
			Console.WriteLine(dp.Id);
			dp.WriteDataValue();
		}

		[Interceptor(InterceptorType = typeof(DynamicTestData), Mode = ExecutionInjectionMode.After)]
		class DynamicTestData
		{
			public string data;

			public int Id { get; set; }

			public void DoStuff()
			{
				Console.WriteLine("Hello");
			}

			public void WriteDataValue()
			{
				Console.WriteLine(data);
			}
		}

		[Interceptor(InterceptorType = typeof(ExceptionInterceptor), Mode = ExecutionInjectionMode.OnError, EatException = false)]
		class ErrorProneClass
		{
			public string data;

			public int Id { get; set; }

			public void DoStuff()
			{
				Console.WriteLine("Hello");
			}

			public void WriteDataValue()
			{
				Console.WriteLine(data);
				throw new InvalidOperationException("Some error message");
			}
		}

		public interface IFoo
		{
			int GetNum();
			DayOfWeek GetDay();
		}

		[TestMethod]
		public void DynamicProxyGeneratorDefaultMethod()
		{
			IFoo instance = null;
			try
			{
				instance = DynamicProxyGeneratorDefault.GetInstanceFor<IFoo>();
			}
			catch (Exception ex) { Console.WriteLine(ex.Message); }

			try
			{
				int num = instance.GetNum();
				var day = instance.GetDay();
				Console.WriteLine(num);
				Console.WriteLine(day);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}

	public interface IEmptyInterface { }

	public interface ITestInterface1
	{
		string Name { get; set; }
		void Handle();
	}
}
