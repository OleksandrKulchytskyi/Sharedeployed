using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Extensions;
using System;
using System.Collections.Generic;

namespace ShareDeployed.Test.Ioc
{
	[TestClass]
	public class WeakEventHandlerUnitTest
	{
		[TestMethod]
		public void Test()
		{
			EventSource es = new EventSource();
			Subscriber1 s1 = new Subscriber1();
			Subscriber2 s2 = new Subscriber2();

			es.SomeEvent += s1.Handle;
			es.SomeEvent += s2.Handle;
			es.Fire();

			es.SomeEvent -= s1.Handle;
		}
	}

	public class EventSource
	{
		private List<WeakEventHandler<TestEventArgs>> _invocations;

		public EventSource()
		{
			_invocations = new List<WeakEventHandler<TestEventArgs>>();
		}

		public event EventHandler<TestEventArgs> SomeEvent
		{
			add
			{
				_invocations.Add((WeakEventHandler<TestEventArgs>)value);
			}
			remove
			{
				_invocations.Remove(new WeakEventHandler<TestEventArgs>(value));
			}
		}

		public void Fire()
		{
			List<WeakEventHandler<TestEventArgs>> toRemove = new List<WeakEventHandler<TestEventArgs>>();
			foreach (WeakEventHandler<TestEventArgs> weak in _invocations)
			{
				if (weak.IsAlive())
				{
					EventHandler<TestEventArgs> handler = (EventHandler<TestEventArgs>)weak;
					var args = new TestEventArgs() { Message = "Hello " + DateTime.Now.ToLongTimeString() };
					handler(this, args);
				}
				else
					toRemove.Add(weak);
			}

			foreach (var item in toRemove)
			{
				_invocations.Remove(item);
			}
		}
	}

	public class Subscriber1
	{
		public Subscriber1()
		{
		}

		public void Handle(object sender, TestEventArgs e)
		{
			Console.WriteLine(e.Message);
		}
	}

	public class Subscriber2
	{
		public Subscriber2()
		{
		}

		public void Handle(object sender, TestEventArgs e)
		{
			Console.WriteLine(e.Message);
		}
	}

	public class TestEventArgs : EventArgs
	{
		public string Message { get; set; }
	}
}