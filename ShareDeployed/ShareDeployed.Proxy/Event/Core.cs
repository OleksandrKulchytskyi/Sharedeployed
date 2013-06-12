using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace ShareDeployed.Proxy.Event
{
	[AttributeUsage(AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
	public class EventSourceAttribute : Attribute
	{
		public EventSourceAttribute(string eventId)
		{
			EventId = eventId;
		}

		public string EventId { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class EventSubscriberAttribute : Attribute
	{
		public EventSubscriberAttribute(string eventId)
		{
			EventId = eventId;
		}

		public string EventId { get; set; }
	}

	[Serializable()]
	public class EventPipelineException : Exception
	{
		readonly List<Exception> exceptions;

		public EventPipelineException()
			: base("One or more exceptions were thrown by event broker sinks") { }

		public EventPipelineException(IEnumerable<Exception> exceptions)
			: this()
		{
			this.exceptions = new List<Exception>(exceptions);
		}

		public ReadOnlyCollection<Exception> Exceptions
		{
			get { return exceptions.AsReadOnly(); }
		}
	}

	internal sealed class EventSource : IDisposable
	{
		readonly string _eventId;
		readonly EventInfo _eventInfo;
		readonly MethodInfo _handlerMethod;
		readonly EventBrokerPipeline _pipeline;
		readonly WeakReference _source;

		public EventSource(EventBrokerPipeline pipeline,
							  object source,
							  EventInfo eventInfo,
							  string eventID)
		{
			this._pipeline = pipeline;
			this._source = new WeakReference(source);
			this._eventInfo = eventInfo;
			this._eventId = eventID;

			_handlerMethod = this.GetType().GetMethod("SourceHandler");
			Delegate @delegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, _handlerMethod);
			eventInfo.AddEventHandler(source, @delegate);
		}

		public object Source
		{
			get { return _source.Target; }
		}

		public void Dispose()
		{
			object sourceObj = _source.Target;

			if (sourceObj != null)
			{
				Delegate @delegate = Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, _handlerMethod);
				_eventInfo.RemoveEventHandler(sourceObj, @delegate);
			}
		}

		public void SourceHandler(object sender, EventArgs e)
		{
			_pipeline.Fire(_eventId, sender, e);
		}
	}

	internal sealed class EventSubscriber
	{
		readonly Type _handlerEventArgsType;
		readonly MethodInfo _methodInfo;
		readonly WeakReference _subscriber;

		public EventSubscriber(object sink, MethodInfo methodInfo)
		{
			this._subscriber = new WeakReference(sink);
			this._methodInfo = methodInfo;

			ParameterInfo[] parameters = methodInfo.GetParameters();

			if (parameters.Length != 2 || !typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType))
				throw new ArgumentException("Method does not appear to be a valid event handler", "methodInfo");
			//TODO: elaborate the retrieving and caching logic for generic event handlers
			_handlerEventArgsType = typeof(EventHandler<>).MakeGenericType(parameters[1].ParameterType);
		}

		public object Subscriber
		{
			get { return _subscriber.Target; }
		}

		public Exception Invoke(object sender, EventArgs e)
		{
			object subscriberObject = _subscriber.Target;

			try
			{
				if (subscriberObject != null)
				{
					Delegate @delegate = Delegate.CreateDelegate(_handlerEventArgsType, subscriberObject, _methodInfo);
					@delegate.DynamicInvoke(sender, e);
				}
				return null;
			}
			catch (TargetInvocationException ex)
			{
				return ex.InnerException;
			}
		}
	}
}