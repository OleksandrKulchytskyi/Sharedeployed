using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using ShareDeployed.Common.Proxy;

namespace ShareDeployed.Test
{
	[TestClass]
	public class TypeExtensionTest
	{
		[TestMethod]
		public void PerformanceTest()
		{
			object value = null;
			Stopwatch sw = new Stopwatch();

			sw.Start();
			value = typeof(byte).GetDefaultValue();
			value = typeof(int).GetDefaultValue();
			value = typeof(long).GetDefaultValue();
			value = typeof(bool).GetDefaultValue();
			value = typeof(float).GetDefaultValue();
			value = typeof(double).GetDefaultValue();
			value = typeof(char).GetDefaultValue();
			value = typeof(string).GetDefaultValue();
			value = typeof(object).GetDefaultValue();
			sw.Stop();
			long tick1 = sw.ElapsedTicks;

			sw.Reset();

			sw.Start();
			value = typeof(byte).GetDefaultValueExp();
			value = typeof(int).GetDefaultValueExp();
			value = typeof(long).GetDefaultValueExp();
			value = typeof(bool).GetDefaultValueExp();
			value = typeof(float).GetDefaultValueExp();
			value = typeof(double).GetDefaultValueExp();
			value = typeof(char).GetDefaultValueExp();
			value = typeof(string).GetDefaultValueExp();
			value = typeof(object).GetDefaultValueExp();
			sw.Stop();
			long tick2 = sw.ElapsedTicks;

			Assert.IsTrue(tick1 < tick2);
		}
	}
}
