using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Pooling;
using System.Collections.Generic;

namespace ShareDeployed.Test.ObjectPool
{
	[TestClass]
	public class BytesArrayPoolTest
	{
		private Proxy.Pooling.PoolBytesArray _instance;

		[TestInitialize]
		public void Init()
		{
			_instance = new PoolBytesArray();
		}

		[TestMethod]
		public void CheckDefaultTest()
		{
			int len = 85000 * 3;
			Assert.IsTrue(_instance.ItemsInUse == 0);
			Assert.IsTrue(_instance.AvailableCount == 2);

			IEnumerable<ByteArray> data = _instance.Acquire(len);
			try
			{
				Assert.IsTrue(data.Count() == 3);
			}
			catch (InvalidOperationException ex)
			{
				if (ex != null)
				{

				}
			}
		}
	}
}
