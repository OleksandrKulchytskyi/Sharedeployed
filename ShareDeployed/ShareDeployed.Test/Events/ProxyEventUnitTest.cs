using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Event;

namespace ShareDeployed.Test.Eventing
{
	[TestClass]
	public class ProxyEventUnitTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			IEventPipelineBuilder builder = new EventPipelineBuilder();
			builder.BuildFor(typeof(ProxyEventUnitTest).Assembly);
		}
	}

	public class EventSource
	{
		[Proxy.Event.EventSource("workCompleted")]
		public event EventHandler WorkCompleted;
	}

	public class EventSubscriber1
	{
		[Proxy.Event.EventSubscriber("workCompleted")]
		public void OnWorkCompleted(object sender,EventArgs e)
		{
			Console.WriteLine("Done " + sender.ToString());
		}
	}

	public class EventSubscriber2
	{
		[Proxy.Event.EventSubscriber("workCompleted")]
		public void OnWorkCompleted(object sender, EventArgs e)
		{
			Console.WriteLine("Done " + sender.ToString());
		}
	}
}
