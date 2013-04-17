using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy;


namespace ShareDeployed.Test.ProxyFactoryTest
{
	[TestClass]
	public class ProxyFactoryUnitTest
	{
		[TestMethod]
		public void SecurityProxyTestMethod()
		{
			ITest test = (ITest)SecurityProxy.NewInstance(new TestImpl());
			test.TestFunctionOne();
			test.TestFunctionTwo(new Object(), new Object());
		}
	}

	public interface IService
	{
		void DoWork();
	}

	public class DefaultService : IService
	{
		public void DoWork()
		{
			Console.WriteLine("Do work ...");
		}
	}


	public interface ITest
	{
		void TestFunctionOne();
		Object TestFunctionTwo(Object a, Object b);
	}

	public class TestImpl : ITest
	{
		public void TestFunctionOne()
		{
			Console.WriteLine("In TestImpl.TestFunctionOne()");
		}

		public Object TestFunctionTwo(Object a, Object b)
		{
			Console.WriteLine("In TestImpl.TestFunctionTwo( Object a, Object b )");
			return null;
		}
	}

	/// <summary>
	/// Test proxy invocation handler which is used to check a methods security before invoking the method
	/// </summary>
	public class SecurityProxy : IProxyInvocationHandler
	{
		Object obj = null;

		private SecurityProxy(Object obj)
		{
			this.obj = obj;
		}

		public static Object NewInstance(Object obj)
		{
			return ProxyFactory.GetInstance().Create(new SecurityProxy(obj), obj.GetType());
		}

		public Object Invoke(Object proxy, System.Reflection.MethodInfo method, Object[] parameters)
		{
			Object retVal = null;
			string userRole = "role";

			// if the user has permission to invoke the method, the method
			// is invoked, otherwise an exception is thrown indicating they
			// do not have permission
			if (true)
				// The actual method is invoked
				retVal = method.Invoke(obj, parameters);
			else
				throw new Exception("Invalid permission to invoke " + method.Name);

			return retVal;
		}
	}
}