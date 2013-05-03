using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Pooling;

namespace ShareDeployed.Test
{
	[TestClass]
	public class ObjectPoolUnitTest
	{
		[TestMethod]
		public void TestObjectPoolMethod()
		{
			int minCount = 1;
			int max = 5;
			// Creating a pool with minimum size of 5 and maximum size of 25, using custom Factory method to create and instance of ExpensiveResource
			ObjectPool<ExpensiveResource> pool = new ObjectPool<ExpensiveResource>(minCount, max, () => new ExpensiveResource(/* resource specific initialization */));
			Assert.IsTrue(pool.ObjectsInPoolCount == minCount);
			
			using (ExpensiveResource resource = pool.GetObject())
			{
				// Using the resource

			} // Exiting the using scope will return the object back to the pool
			using (ExpensiveResource resource = pool.GetObject())
			{
				// Using the resource

			} // Exiting the using scope will return the object back to the pool
			using (ExpensiveResource resource = pool.GetObject())
			{
				// Using the resource

			} // Exiting the using scope will return the object back to the pool
			using (ExpensiveResource resource = pool.GetObject())
			{
				// Using the resource

			} // Exiting the using scope will return the object back to the pool
			using (ExpensiveResource resource = pool.GetObject())
			{
				// Using the resource

			} // Exiting the using scope will return the object back to the pool
			Assert.IsTrue(pool.ObjectsInPoolCount == 2);
			// Creating a pool with wrapper object for managing external resources
			ObjectPool<PooledObjectWrapper<ExternalExpensiveResource>> newPool = new ObjectPool<PooledObjectWrapper<ExternalExpensiveResource>>(() =>
				new PooledObjectWrapper<ExternalExpensiveResource>(CreateNewResource())
				{
					WrapperReleaseResourcesAction = (r) => ExternalResourceReleaseResource(r),
					WrapperResetStateAction = (r) => ExternalResourceResetState(r)
				});

			using (var wrapper = newPool.GetObject())
			{
				// wrapper.InternalResource.DoStuff()
			}

			using (var wrapper = newPool.GetObject())
			{
				// wrapper.InternalResource.DoStuff()
			}

			Assert.IsTrue(newPool.ObjectsInPoolCount == 6);
		}

		private static ExternalExpensiveResource CreateNewResource()
		{
			return new ExternalExpensiveResource();
		}

		public static void ExternalResourceResetState(ExternalExpensiveResource resource)
		{
			// External Resource reset state code
		}

		public static void ExternalResourceReleaseResource(ExternalExpensiveResource resource)
		{
			// External Resource release code
		}
	}

	public class ExpensiveResource : PooledObject
	{
		public ExpensiveResource()
		{
			// Initialize the resource if needed
		}

		protected override void OnReleaseResources()
		{
			// Override if the resource needs to be manually cleaned before the memory is reclaimed
			System.Diagnostics.Debug.WriteLine("OnReleaseResources");
		}

		protected override void OnResetState()
		{
			// Override if the resource needs resetting before it is getting back into the pool
			System.Diagnostics.Debug.WriteLine("OnResetState");
		}
	}

	public class ExternalExpensiveResource
	{
	}
}
