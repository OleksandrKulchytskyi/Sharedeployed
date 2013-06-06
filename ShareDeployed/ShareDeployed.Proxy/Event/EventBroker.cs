using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShareDeployed.Proxy.Event
{
	public sealed class EventBrokerPipeline : IDisposable
	{
		MultivalueDictionary<string, EventSource> _sources = new MultivalueDictionary<string, EventSource>();
		MultivalueDictionary<string, EventSubscriber> _subscribers = new MultivalueDictionary<string, EventSubscriber>();

		public void Fire(string eventId, object sender, EventArgs e)
		{
			List<Exception> exceptions = new List<Exception>();

			foreach (EventSubscriber subscriber in _subscribers[eventId])
			{
				Exception ex = subscriber.Invoke(sender, e);
				if (ex != null) exceptions.Add(ex);
			}

			if (exceptions.Count > 0)
				throw new EventPipelineException(exceptions);
		}

		public void RegisterSubscriber(object subscriber, MethodInfo methodInfo, string eventID)
		{
			subscriber.ThrowIfNull("subscriber", "Parameter cannot be a null.");
			methodInfo.ThrowIfNull("methodInfo", "Parameter cannot be a null.");
			eventID.ThrowIfNull("eventId", "Parameter cannot be a null.");

			RemoveDeadSubscribersAndSources();
			_subscribers.Add(eventID, new EventSubscriber(subscriber, methodInfo));
		}

		public void RegisterSource(object source, EventInfo eventInfo, string eventID)
		{
			source.ThrowIfNull("source", "Parameter cannot be a null.");
			eventInfo.ThrowIfNull("eventInfo", "Parameter cannot be a null.");
			eventID.ThrowIfNull("eventID", "Parameter cannot be a null.");

			RemoveDeadSubscribersAndSources();
			_sources.Add(eventID, new EventSource(this, source, eventInfo, eventID));
		}

		void RemoveDeadSubscribersAndSources()
		{
			foreach (string eventID in _subscribers.Keys)
				_subscribers[eventID].RemoveAll(delegate(EventSubscriber subscriber)
				{
					return subscriber.Subscriber == null;
				});

			foreach (string eventID in _sources.Keys)
				_sources[eventID].RemoveAll(delegate(EventSource source) { return source.Source == null; });
		}

		public void UnregisterSubscriber(object subscriber, string eventID)
		{
			subscriber.ThrowIfNull("subscriber", "Parameter cannot be a null.");
			eventID.ThrowIfNull("eventID", "Parameter cannot be a null.");

			RemoveDeadSubscribersAndSources();

			List<EventSubscriber> matchingSinks = new List<EventSubscriber>();

			matchingSinks.AddRange(_subscribers.FindByKeyAndValue(delegate(string name)
			{
				return name == eventID;
			},
			delegate(EventSubscriber snk)
			{
				return snk.Subscriber == subscriber;
			}));

			foreach (EventSubscriber eventSubs in matchingSinks)
				_subscribers.Remove(eventID, eventSubs);
		}

		public void UnregisterSource(object source, string eventID)
		{
			source.ThrowIfNull("source", "Parameter cannot be a null.");
			eventID.ThrowIfNull("eventID", "Parameter cannot be a null.");

			RemoveDeadSubscribersAndSources();

			List<EventSource> matchingSources = new List<EventSource>();

			matchingSources.AddRange(_sources.FindByKeyAndValue(delegate(string name)
			{
				return name == eventID;
			},
			delegate(EventSource src) { return src.Source == source; }));

			foreach (EventSource eventSource in matchingSources)
			{
				eventSource.Dispose();
				_sources.Remove(eventID, eventSource);
			}
		}

		public void Dispose()
		{
			foreach (var item in _sources)
				foreach (var subItem in item.Value)
					subItem.Dispose();
		}
	}
}