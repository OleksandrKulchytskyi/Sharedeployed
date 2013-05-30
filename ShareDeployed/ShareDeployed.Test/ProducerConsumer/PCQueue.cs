using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ShareDeployed.Test.ProducerConsumer
{
	public class PCQueue : IDisposable
	{
		private BlockingCollection<Action> _taskQ = new BlockingCollection<Action>();

		public PCQueue(int workerCount)
		{
			// Create and start a separate Task for each consumer:
			for (int i = 0; i < workerCount; i++)
				Task.Factory.StartNew(Consume);
		}

		public void Enqueue(Action action)
		{
			_taskQ.Add(action);
		}

		private void Consume()
		{
			// This sequence that we're enumerating will block when no elements
			// are available and will end when CompleteAdding is called.
			foreach (Action action in _taskQ.GetConsumingEnumerable())
				action(); // Perform task.
		}

		public void Dispose()
		{
			_taskQ.CompleteAdding();
		}
	}

	//var pcQ = new PCQueueTask (2); // Maximum concurrency of 2
	//string result =  pcQ.Enqueue (() => "That was easy!").Result;
	public class PCQueueTask : IDisposable
	{
		private BlockingCollection<Task> _taskQ = new BlockingCollection<Task>();

		public PCQueueTask(int workerCount)
		{
			// Create and start a separate Task for each consumer:
			for (int i = 0; i < workerCount; i++)
				Task.Factory.StartNew(Consume);
		}

		public Task Enqueue(Action action, CancellationToken cancelToken = default (CancellationToken))
		{
			var task = new Task(action, cancelToken);
			_taskQ.Add(task);
			return task;
		}

		public Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancelToken = default (CancellationToken))
		{
			var task = new Task<TResult>(func, cancelToken);
			_taskQ.Add(task);
			return task;
		}

		private void Consume()
		{
			foreach (var task in _taskQ.GetConsumingEnumerable())
				try
				{
					if (!task.IsCanceled) task.RunSynchronously();
				}
				catch (InvalidOperationException) { } // Race condition
		}

		public void Dispose()
		{
			_taskQ.CompleteAdding();
		}
	}

	public class PCQueueTaskWorkItems : IDisposable
	{
		private class WorkItem
		{
			public readonly TaskCompletionSource<object> TaskSource;
			public readonly Action Action;
			public readonly CancellationToken? CancelToken;

			public WorkItem(TaskCompletionSource<object> taskSource, Action action, CancellationToken? cancelToken)
			{
				TaskSource = taskSource;
				Action = action;
				CancelToken = cancelToken;
			}
		}

		private BlockingCollection<WorkItem> _taskQ = new BlockingCollection<WorkItem>();

		public PCQueueTaskWorkItems(int workerCount)
		{
			// Create and start a separate Task for each consumer:
			for (int i = 0; i < workerCount; i++)
				Task.Factory.StartNew(Consume);
		}

		public void Dispose()
		{
			_taskQ.CompleteAdding();
		}

		public Task EnqueueTask(Action action)
		{
			return EnqueueTask(action, null);
		}

		public Task EnqueueTask(Action action, CancellationToken? cancelToken)
		{
			var tcs = new TaskCompletionSource<object>();
			_taskQ.Add(new WorkItem(tcs, action, cancelToken));
			return tcs.Task;
		}

		private void Consume()
		{
			foreach (WorkItem workItem in _taskQ.GetConsumingEnumerable())
				if (workItem.CancelToken.HasValue &&
					workItem.CancelToken.Value.IsCancellationRequested)
				{
					workItem.TaskSource.SetCanceled();
				}
				else
				{
					try
					{
						workItem.Action();
						workItem.TaskSource.SetResult(null); // Indicate completion
					}
					catch (Exception ex)
					{
						workItem.TaskSource.SetException(ex);
					}
				}
		}
	}
}