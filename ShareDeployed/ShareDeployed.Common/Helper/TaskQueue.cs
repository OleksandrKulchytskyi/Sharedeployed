using System;
using System.Collections.Generic;
using System.Threading;

namespace ShareDeployed.Common.Helper
{
	public class TaskQueue : IDisposable
	{
		object locker = new object();
		Thread[] workers;
		Queue<string> taskQ = new Queue<string>();

		public TaskQueue(int workerCount)
		{
			workers = new Thread[workerCount];
			// Create and run separate pool  for each consumer
			for (int i = 0; i < workerCount; i++)
				(workers[i] = new Thread(Consume)).Start();
		}

		public void Dispose()
		{
			foreach (Thread worker in workers)
				EnqueueTask(null);

			foreach (Thread worker in workers)
				worker.Join();
		}

		public void EnqueueTask(string task)
		{
			lock (locker)
			{
				taskQ.Enqueue(task);
				Monitor.PulseAll(locker);
			}
		}

		void Consume()
		{
			while (true)
			{
				string task;

				lock (locker)
				{
					while (taskQ.Count == 0)
						Monitor.Wait(locker);
					task = taskQ.Dequeue();
				}

				if (task == null)
					return;  //exit signal was triggered

				System.Diagnostics.Debug.Write(task);
				Thread.Sleep(1000); //simulation of long-running task
			}
		}
	}
}
