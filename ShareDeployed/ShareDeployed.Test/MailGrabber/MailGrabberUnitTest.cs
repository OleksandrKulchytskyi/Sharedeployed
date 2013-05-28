using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using ShareDeployed.Mailgrabber.Helpers;

namespace ShareDeployed.Test.MailGrabber
{
	[TestClass]
	public class MailGrabberUnitTest
	{
		[TestMethod]
		public void TestWeakEvents()
		{
			EventSource es = new EventSource();
			Subscriber1 s1 = new Subscriber1();
			Subscriber2 s2 = new Subscriber2();
			es.SomeEvent += s1.Handle;
			es.SomeEvent += s2.Handle;

			GC.Collect();
			es.Fire();
			GC.Collect();
			es.Fire();
			s1 = null;
			GC.Collect();
			es.Fire();
			es.Fire();
		}
	}

	public class EventSource
	{
		List<WeakEventHandlerAdvanced<TestEventArgs>> _invocations;
		public EventSource()
		{
			_invocations = new List<WeakEventHandlerAdvanced<TestEventArgs>>();
		}

		public event EventHandler<TestEventArgs> SomeEvent
		{
			add
			{
				_invocations.Add(new WeakEventHandlerAdvanced<TestEventArgs>(value));
			}
			remove
			{
				_invocations.Remove(new WeakEventHandlerAdvanced<TestEventArgs>(value));
			}
		}

		public void Fire()
		{
			List<WeakEventHandlerAdvanced<TestEventArgs>> toRemove = new List<WeakEventHandlerAdvanced<TestEventArgs>>();
			foreach (WeakEventHandlerAdvanced<TestEventArgs> weak in _invocations)
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
