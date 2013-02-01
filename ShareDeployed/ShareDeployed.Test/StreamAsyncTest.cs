using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ShareDeployed.Common.Extensions;
using System.Text;
namespace ShareDeployed.Test
{
	[TestClass]
	public class StreamAsyncTest
	{
		private const long maxGarbage = 2000;

		[TestMethod]
		public void TestMethod1()
		{
			StringBuilder sb = new StringBuilder();
			string fpath = @"E:\Physicians.xml";
			string fpath1 = @"E:\Comp1.xml";
			string fpath2 = @"E:\Comp2.xml";
			byte[] buffer = new byte[256];

			using (var fs = new FileStream(fpath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamUtils.GetClusterSize(fpath), FileOptions.Asynchronous))
			{
				var task = fs.ReadAsync(buffer);
				task.Wait();
				sb.Append(Encoding.UTF8.GetString(buffer, 0, task.Result));
				while (task.Result == buffer.Length)
				{
					task = fs.ReadAsync(buffer);
					task.Wait();
					sb.Append(Encoding.UTF8.GetString(buffer, 0, task.Result));
				}
				task.Dispose();
			}
			var data = sb.ToString();
			var fdata = File.ReadAllText(fpath, Encoding.UTF8);
			if (data.Length != fdata.Length)
			{
				for (int i = 0; i < fdata.Length; i++)
				{
					if (fdata[i] != data[i])
					{

					}
				}

				File.WriteAllText(fpath1, data, Encoding.UTF8);
				File.WriteAllText(fpath2, fdata, Encoding.UTF8);
			}
			Assert.IsTrue(data.Equals(fdata, StringComparison.OrdinalIgnoreCase));
			System.Diagnostics.Debug.WriteLine(data);
		}

		[TestMethod]
		public void ByteLenTestMethod()
		{
			int maxLockNumber = 0x400;
			var value = StreamUtils.FormatBytesLen(maxLockNumber);
			Assert.IsTrue(value.Contains("Bytes"));

			value = StreamUtils.FormatBytesLen(maxLockNumber + 512);
			Assert.IsTrue(value.Contains("KB"));

			value = StreamUtils.FormatBytesLen((maxLockNumber * 1024) + 4024);
			Assert.IsTrue(value.Contains("MB"));

			long len = (((maxLockNumber * 1024) * 1024)) + 2;
			value = StreamUtils.FormatBytesLen(len);
			Assert.IsTrue(value.Contains("GB"));
		}

		[TestMethod]
		public void MemoryTestMethod()
		{
			System.Diagnostics.Debug.WriteLine("Total Memory before instantiating: {0}", StreamUtils.FormatBytesLen(GC.GetTotalMemory(false)));

			StreamAsyncTest myGCCol = new StreamAsyncTest();

			System.Diagnostics.Debug.WriteLine("Before garbage allocating, Total Memory: {0}", StreamUtils.FormatBytesLen(GC.GetTotalMemory(false)));
			// Determine the maximum number of generations the system garbage collector currently supports.
			System.Diagnostics.Debug.WriteLine("The highest generation is {0}", GC.MaxGeneration);

			myGCCol.MakeSomeGarbage();

			// Determine which generation myGCCol object is stored in.
			System.Diagnostics.Debug.WriteLine("Object is in Generation: {0}", GC.GetGeneration(myGCCol));

			// Determine the best available approximation of the number of bytes currently allocated in managed memory.
			System.Diagnostics.Debug.WriteLine("Total Memory: {0}", StreamUtils.FormatBytesLen(GC.GetTotalMemory(false)));

			// Perform a collection of generation 0 only.
			GC.Collect(0);
			System.Diagnostics.Debug.WriteLine("GC collect -> 0");

			// Determine which generation myGCCol object is stored in.
			System.Diagnostics.Debug.WriteLine("Object is in Generation: {0}", GC.GetGeneration(myGCCol));
			System.Diagnostics.Debug.WriteLine("Total Memory: {0}", StreamUtils.FormatBytesLen(GC.GetTotalMemory(false)));

			// Perform a collection of all generations up to and including 2.
			GC.Collect(2);
			System.Diagnostics.Debug.WriteLine("GC collect -> 2");

			// Determine which generation myGCCol object is stored in.
			System.Diagnostics.Debug.WriteLine("Object is in  Generation: {0}", GC.GetGeneration(myGCCol));
			System.Diagnostics.Debug.WriteLine("Total Memory: {0}", StreamUtils.FormatBytesLen(GC.GetTotalMemory(true)));
			System.Threading.Thread.Sleep(4000);
		}

		void MakeSomeGarbage()
		{
			Version vt;

			for (int i = 0; i < maxGarbage; i++)
			{
				// Create objects and release them to fill up memory with unused objects.
				vt = new Version();
			}
		}

		[TestMethod]
		public void TM4()
		{
			MyClass mc = new MyClass();
			int hashCode = mc.GetHashCode();
			if (hashCode > 0)
			{

			}

			Boolean t1 = (true ^ false);
			Boolean t2 = (false ^ true);
			Boolean t3 = (false ^ false);
			Assert.IsTrue(t1);
			Assert.IsTrue(t2);
			Assert.IsFalse(t3);

			int i1 = (0 ^ 1 ^ 2);
			Assert.IsTrue(i1 == 3);

			int i2 = (18 ^ 16);
			Assert.IsTrue(i2 == 2);

			int i3 = (19 ^ 16 ^ 1);
			Assert.IsTrue(i3 == 2);

			int i4 = (1 ^ 16 ^ 18);
			Assert.IsTrue(i4 == 35);
		}

		internal class SomeType
		{
			public override int GetHashCode()
			{
				return 0;
			}
		}

		internal class AnotherType
		{
			public override int GetHashCode()
			{
				return 1;
			}
		}

		internal class LastType
		{
			public override int GetHashCode()
			{
				return 2;
			}
		}

		internal class MyClass
		{
			SomeType a = new SomeType();
			AnotherType b = new AnotherType();
			LastType c = new LastType();

			public override int GetHashCode()
			{
				return a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode();
			}
		}
	}
}
