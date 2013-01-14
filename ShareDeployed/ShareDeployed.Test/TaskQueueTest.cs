using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Helper;

namespace ShareDeployed.Test
{
	[TestClass]
	public class TaskQueueTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			using (TaskQueue q = new TaskQueue(2))
			{
				System.Diagnostics.Debug.Write("Enqueue in queue up to 10 tasks.");
				System.Diagnostics.Debug.Write("Wait for tasks completion.");

				for (int i = 0; i < 20; i++)
					q.EnqueueTask(" Task" + i);
			}
		}
	}
}
