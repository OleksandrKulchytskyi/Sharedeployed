using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;

namespace ShareDeployed.Test
{
	[TestClass]
	public class PulseWaitUnitTest
	{
		ProcessQueue<SimpObj> impl = new ProcessQueue<SimpObj>();
		[TestMethod]
		public void TestMethod()
		{
			Thread putThread = new Thread(Put);
			Thread processThread = new Thread(Process);
			putThread.Start();
			processThread.Start();

			Thread.Sleep(TimeSpan.FromSeconds(20));

			impl.Add(null);

			Thread.Sleep(TimeSpan.FromSeconds(10));
		}

		void Put()
		{
			while (true)
			{
				impl.Add(new SimpObj() { Data = new Random().Next(1, 1999) });
				Thread.Sleep(100);
			}
		}

		void Process()
		{
			impl.Process();
		}
	}

	public class ProcessQueue<T> where T : class
	{
		private Queue<T> _queue = null;
		private readonly object _locker;

		public ProcessQueue()
		{
			_queue = new Queue<T>();
			_locker = new object();
		}

		public void Process()
		{
			while (true)
			{
				T item;
				lock (_locker)
				{
					while (_queue.Count == 0)
					{
						System.Diagnostics.Debug.WriteLine("Begin waiting....");
						Monitor.Wait(_locker, TimeSpan.FromMilliseconds(500));
						System.Diagnostics.Debug.WriteLine("END WAITING !!!");
					}
					item = _queue.Dequeue();
				}
				if (item == null)
				{
					System.Diagnostics.Debug.WriteLine("Prepare for exit process");
					return;         // This signals our exit.
				}
				System.Diagnostics.Debug.WriteLine(item.ToString());
			}
		}

		public void Add(T data)
		{
			lock (_locker)
			{
				_queue.Enqueue(data);
				Monitor.Pulse(_locker);
			}
		}
	}

	public class SimpObj
	{
		public int Data { get; set; }

		public override string ToString()
		{
			return Data.ToString();
		}
	}
}
