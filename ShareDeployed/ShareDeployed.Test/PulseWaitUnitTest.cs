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

			Thread.Sleep(TimeSpan.FromSeconds(30));

			impl.Add(null);

			Thread.Sleep(TimeSpan.FromSeconds(2));
		}

		void Put()
		{
			while (true)
			{
				impl.Add(new SimpObj() { Data = new Random().Next(1, 1999) });
				Thread.Sleep(500);
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
		private volatile bool _stopProcessed;

		public ProcessQueue()
		{
			_queue = new Queue<T>();
			_locker = new object();
			_stopProcessed = false;
		}

		public void Add(T data)
		{
			if (_stopProcessed)
				return;
			lock (_locker)
			{
				_queue.Enqueue(data);
				Monitor.Pulse(_locker);
			}
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
						Monitor.Wait(_locker, TimeSpan.FromMilliseconds(300));
						System.Diagnostics.Debug.WriteLine("END WAITING !!!");
					}
					item = _queue.Dequeue();
				}
				if (item == null)
				{
					_stopProcessed = true;
					System.Diagnostics.Debug.WriteLine("Prepare for exit process");
					return;         // This signals our exit.
				}
				System.Diagnostics.Debug.WriteLine(item.ToString());
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
