using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;

namespace ShareDeployed.Test
{
	[TestClass]
	public class ThreadTest
	{
		static ReaderWriterLock rw = new ReaderWriterLock();
		static List<int> items = new List<int>();
		static Random rand = new Random();

		private void Run()
		{
			new Thread(delegate() { while (true) AppendItem(); }).Start();
			new Thread(delegate() { while (true) RemoveItem(); }).Start();
			new Thread(delegate() { while (true) WriteTotal(); }).Start();
			new Thread(delegate() { while (true) WriteTotal(); }).Start();
		}

		static int GetRandNum(int max) { lock (rand) return rand.Next(max); }

		static void WriteTotal()
		{
			rw.AcquireReaderLock(10000);
			int tot = 0;

			foreach (int i in items)
				tot += i;
			System.Diagnostics.Debug.WriteLine("Total is " + tot);
			Console.WriteLine(tot);
			rw.ReleaseReaderLock();
		}

		static void AppendItem()
		{
			rw.AcquireWriterLock(10000);
			items.Add(GetRandNum(1000));
			System.Diagnostics.Debug.WriteLine("Item has been appended");
			Thread.SpinWait(400);
			rw.ReleaseWriterLock();
		}

		static void RemoveItem()
		{
			rw.AcquireWriterLock(10000);

			if (items.Count > 0)
			{
				items.RemoveAt(GetRandNum(items.Count));
				System.Diagnostics.Debug.WriteLine("Item has been removed");
			}

			rw.ReleaseWriterLock();
		}

		[TestMethod]
		public void TestMethod1()
		{
			Run();

			Thread.Sleep(new TimeSpan(0, 2, 0));
		}
	}
}
