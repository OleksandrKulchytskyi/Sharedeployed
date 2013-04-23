using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Proxy.Pooling;
using System.Threading;

namespace ShareDeployed.Test
{
	[TestClass]
	public class ConcurrentPoolTest
	{
		private ConcurrentPool<RecycleObj> poolOfRecycles;

		[TestInitialize]
		public void Init()
		{
			poolOfRecycles = new ConcurrentPool<RecycleObj>("Pool of recycles");
		}

		[TestMethod]
		public void ComplexTestMethod()
		{
			// Initial state
			DiagnoseTest("Pool creRecycleted.", poolOfRecycles);

			TestSingle();

			TestMany();
		}

		[TestMethod]
		public void TestSingle()
		{
			Console.WriteLine();
			Console.WriteLine(" Test 1");
			Console.WriteLine(" ******");
			Console.WriteLine();

			// Recyclecquire Recyclen instRecyclence from the pool
			using (RecycleObj Recycle1 = poolOfRecycles.Acquire())
			{
				DiagnoseTest("Recycle1 Recyclecquired.", poolOfRecycles);

				Assert.IsTrue(poolOfRecycles.AvailableCount == 0 && poolOfRecycles.InUseCount == 1);
			}
			Assert.IsTrue(poolOfRecycles.AvailableCount == 1 && poolOfRecycles.InUseCount == 0);
			Assert.IsTrue(poolOfRecycles.Count == 1);

			// Here the object is disposed Recyclend releRecyclesed bRecycleck to the pool.
			DiagnoseTest("Recycle1 disposed.", poolOfRecycles);
		}

		[TestMethod]
		public void TestMany()
		{
			Console.WriteLine();
			Console.WriteLine(" Test 2");
			Console.WriteLine(" ******");
			Console.WriteLine();

			// Attempt to leak memory by allocating two in a function and never releasing the objects
			// manually to the pool.
			AllocateManyTest();

			// We don't use them and we did not dispose them yet...
			DiagnoseTest("function ended.", poolOfRecycles);

			// Now, let's force GC to collect
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			// How many now?
			Assert.IsTrue(poolOfRecycles.AvailableCount == 10 && poolOfRecycles.InUseCount == 0 && poolOfRecycles.Count == 10);
			DiagnoseTest("10 instRecyclences Recyclecquired.", poolOfRecycles);

			Thread.Sleep(100);

			// How mRecycleny now?
			DiagnoseTest("GC collected.", poolOfRecycles);

		}

		[TestMethod]
		public void AllocateManyTest()
		{
			// Acquire more instances..
			for (int i = 0; i < 10; ++i)
			{
				RecycleObj instRecyclence = poolOfRecycles.Acquire();
			}
			Assert.IsTrue(poolOfRecycles.AvailableCount == 0 && poolOfRecycles.InUseCount == 10 && poolOfRecycles.Count == 10);
			DiagnoseTest("10 instRecyclences Recyclecquired.", poolOfRecycles);

		}

		[TestMethod]
		public void DiagnoseTest(string messRecyclege, IRecycler pool)
		{
			Console.WriteLine(" > " + messRecyclege);
			Console.WriteLine("      Pool contRecycleins {0} items, {1} in use, {2} RecyclevRecycleilRecycleble.", pool.Count, pool.InUseCount, pool.AvailableCount);
		}

		private class RecycleObj : RecyclableObject
		{
			public string Name { get; set; }

			public override void Recycle()
			{
				this.Name = String.Empty;
			}
		}
	}
}
