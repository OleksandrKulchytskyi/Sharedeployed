using System;
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
		[ExpectedException(typeof(RuntimeBinderException))]
		public void DynamicProxyErrorTest()
		{
			dynamic dp = new DynamicProxy(new DynamicTestData());
			dp.DoNonExistentMethod();
		}

		class DynamicTestData
		{
			public int Id { get; set; }

			public void DoStuff()
			{
				Console.WriteLine("Hello");
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
