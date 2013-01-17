using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Timers;

namespace ShareDeployed.Test
{
	[TestClass]
	public class TimerTest
	{
		private volatile int count;

		[TestMethod]
		public void TestTimerMethod()
		{
			count = 0;
			Timer t1 = new Timer((int)(new TimeSpan(0, 0, 2).TotalMilliseconds), true);
			t1.Elapsed += t1_Elapsed;
			t1.Start();
			System.Threading.Thread.Sleep(new TimeSpan(0, 0, 18));
			t1.Stop();
			if (count < 9)
				Assert.Fail();

			System.Threading.Thread.Sleep(new TimeSpan(0, 0, 5));
			if (count > 10)
				Assert.Fail();
		}

		[TestMethod]
		public void TestTimerMethod2()
		{
			count = 0;
			Timer t1 = new Timer((int)(new TimeSpan(0, 0, 2).TotalMilliseconds), true);
			t1.Elapsed += t1_Elapsed;
			t1.Start();
			System.Threading.Thread.Sleep(new TimeSpan(0, 0, 2));
			t1.Stop();
			if (count != 1)
				Assert.Fail();
		}

		void t1_Elapsed(object sender, EventArgs e)
		{
			count++;
		}
	}
}
