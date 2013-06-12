using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Proxy.Event;
using ShareDeployed.Proxy;

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

		[TestMethod]
		public void TestMethod2()
		{
			DynamicProxyPipeline.Instance.ContracResolver.EventRegistrator.RegiterEvent<EventSource>();

			DynamicProxyPipeline.Instance.ContracResolver.EventRegistrator.RegiterEventListener<EventSubscriber1>();
			DynamicProxyPipeline.Instance.ContracResolver.EventRegistrator.RegiterEventListener<EventSubscriber2>();

			EventSource source = DynamicProxyPipeline.Instance.ContracResolver.Resolve<EventSource>();
			EventSubscriber2 subs2 = DynamicProxyPipeline.Instance.ContracResolver.Resolve<EventSubscriber2>();

			source.Invoke();

			Assert.IsTrue(subs2.Handled);
		}
	}

	public class EventSource
	{
		[Proxy.Event.EventSource("workCompleted")]
		public event EventHandler WorkCompleted;

		public void Invoke()
		{
			EventHandler handler = WorkCompleted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
	}

	public class EventSubscriber1
	{
		[Proxy.Event.EventSubscriber("workCompleted")]
		public void OnWorkCompleted(object sender, EventArgs e)
		{
			Console.WriteLine("Done " + sender.ToString());
		}
	}

	public class EventSubscriber2
	{
		internal bool Handled = false;
		[Proxy.Event.EventSubscriber("workCompleted")]
		public void OnWorkCompleted(object sender, EventArgs e)
		{
			Console.WriteLine("Done " + sender.ToString());
			Handled = true;
		}
	}
}
