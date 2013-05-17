using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Pooling;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ShareDeployed.Test.ObjectPool
{
	[TestClass]
	public class BytesArrayPoolTest
	{
		private int _id = -1;
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

		[TestMethod]
		public void ReadFileToChunks()
		{
			string fpath = @"d:\1.docx";
			Interlocked.Exchange(ref _id, 15);

			Assert.IsTrue(Interlocked.CompareExchange(ref _id, 0, 0) == 15);
			Assert.IsTrue(Interlocked.CompareExchange(ref _id, 0, 0) == 15);

			Interlocked.Exchange(ref _id, 265);

			Assert.IsTrue(Interlocked.CompareExchange(ref _id, 0, 0) == 265);
			Assert.IsTrue(Interlocked.CompareExchange(ref _id, 0, 0) == 265);

			long fLen = -1;
			using (FileStream fs = File.OpenRead(fpath))
			{
				fLen = fs.Length;
			}

			Assert.IsTrue(_instance.ItemsInUse == 0);
			Assert.IsTrue(_instance.AvailableCount == 2);

			List<ByteArray> buckets = _instance.Acquire(fLen);
			try
			{
				Assert.IsTrue(buckets.Count() > 7);
			}
			catch (InvalidOperationException ex)
			{
				if (ex != null)
				{
				}
			}


			using (FileStream fs = File.OpenRead(fpath))
			{
				if (fs.CanSeek)
					fs.Seek(0, SeekOrigin.Begin);

				int read = 0;
				byte[] buffer;
				foreach (ByteArray array in buckets)
				{
					try
					{
						buffer = array.GetBytesArray();
						read = fs.Read(buffer, 0, array.Capacity);
						array.AssignRealLength(read);
					}
					catch (Exception ex)
					{
						if (ex != null) { }
						throw;
					}
				}
			}

			using (FileStream fs = File.Create(@"D:\2.docx"))
			{
				foreach (ByteArray array in buckets)
				{
					fs.Write(array.GetBytesArray(), 0, array.RealLength);
				}
			}

			try
			{
				_instance.Release(buckets);
			}
			catch (Exception ex)
			{
				if (ex != null) { }

				throw;
			}

			Assert.IsTrue(_instance.ItemsInUse == 0);
			Assert.IsTrue(_instance.AvailableCount == buckets.Count);
		}

		[TestMethod]
		public void PromotePinnedObjectToHigherGen()
		{
			byte[] bytes = new byte[128];
			Debug.WriteLine("After array was allocated, generation is: " + GC.GetGeneration(bytes));
			System.Runtime.InteropServices.GCHandle gch = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			GC.Collect();
			Debug.WriteLine("Generation after GC.Collect: " + GC.GetGeneration(bytes));
			gch.Free();
			GC.KeepAlive(bytes);
			Debug.WriteLine("Generation after GC.KeepAlive: " + GC.GetGeneration(bytes));
		}

		[TestMethod]
		public void LOHAllocTestValidFor32bit()
		{
			//see: http://blog.mohammadjalloul.com/blogs/mo/archive/2010/02/21/the-large-object-heap.aspx
			// http://www.codeproject.com/Articles/15992/Trouble-with-the-Large-Object-Heap
			// Define size as 1 byte less than the cutoff value for allocating in the LOH.
			int ARRAY_SIZE_BYTES = 85000 - 13;
			byte[] bytesLOH = new byte[ARRAY_SIZE_BYTES + 2];
			byte[] bytes = new byte[ARRAY_SIZE_BYTES];
			for (int i = 0; i < bytes.Length; i++)
			{
				bytes[i] = 0x20; // space
			}

			for (int i = 0; i < bytesLOH.Length; i++)
			{
				bytesLOH[i] = 0x20; // space
			}

			int isInLoh = GC.GetGeneration(bytesLOH);
			int isInGen0 = GC.GetGeneration(bytes);
			Assert.IsTrue(isInLoh == 2);
			Assert.IsTrue(isInGen0 == 0);
		}
	}
}
