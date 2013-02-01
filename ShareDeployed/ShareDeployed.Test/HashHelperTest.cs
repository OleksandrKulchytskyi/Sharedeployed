﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Extensions;
using System.Diagnostics;
using System.Collections.Generic;

namespace ShareDeployed.Test
{
	[TestClass]
	public class HashHelperTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			Test tc = new Test() { A = 345, B = "Petersburg", ID = 12345 };
			int hc = tc.GetHashCode();
			Assert.AreNotEqual(0, hc);
		}

		[TestMethod]
		public void PerformanceTest()
		{
			int count = 10000;
			List<Test> list = new List<Test>();
			HashSet<Test> hs = new HashSet<Test>();

			Stopwatch sw = new Stopwatch();

			Test f1 = null;
			Test f2 = null;

			sw.Start();
			for (int i = 1; i <= count; i++)
			{
				Test tc = new Test() { A = i + 1, B = "Petersburg" + i, ID = i };
				hs.Add(tc);
				if (i == 5000)
					f1 = tc;
			}
			sw.Stop();

			long r1 = sw.ElapsedTicks;
			sw.Reset();


			sw.Start();
			for (int i = 1; i <= count; i++)
			{
				Test tc = new Test() { A = i + 1, B = "Petersburg" + i, ID = i };
				list.Add(tc);
				if (i == 5000)
					f2 = tc;
			}
			sw.Stop();

			long r2 = sw.ElapsedTicks;
			sw.Reset();
			Assert.AreNotSame(r1, r2);

			sw.Start();
			if (!hs.Contains(f1))
			{
				Assert.Fail();
			}
			sw.Stop();
			r1 = sw.ElapsedTicks;
			sw.Reset();

			sw.Start();
			if (!list.Contains(f2))
			{
				Assert.Fail();
			}
			sw.Stop();
			r2 = sw.ElapsedTicks;
			sw.Reset();

			Assert.IsTrue(r1 < r2);
		}


		private class Test
		{
			public int A { get; set; }

			public string B { get; set; }

			public int ID { get; set; }

			public override int GetHashCode()
			{
				return HashHelper.GetHashCode(A, B, ID);
			}
		}
	}
}
