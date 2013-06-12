using System;
using System.Linq;
using System.Reflection;
using ShareDeployed.Proxy;
namespace ShareDeployed.Proxy.Event
{
	public interface IEventBrokerRegistrator
	{
		void RegiterEvent<T>(string eventName, string evntPipeId);
		void RegiterEventListener<T>(string eventHandler, string evntPipeId);

		void RegiterEvent<T>();
		void RegiterEventListener<T>();
	}

	public class EventBrokerRegistrator : IEventBrokerRegistrator
	{
		private IContractResolver _resolver;
		private IEventBrokerPipeline _eventPipeline;

		public EventBrokerRegistrator()
		{
			_resolver = DynamicProxyPipeline.Instance.ContracResolver;
			_eventPipeline = DynamicProxyPipeline.Instance.ContracResolver.Resolve<IEventBrokerPipeline>();
		}

		public EventBrokerRegistrator(IContractResolver resolver, IEventBrokerPipeline eventPipeline)
		{
			_resolver = resolver;
			_eventPipeline = eventPipeline;
		}

		public void RegiterEvent<T>(string eventName, string evntPipeId)
		{
			eventName.ThrowIfNull("eventName", "Parameter cannot be a null.");
			evntPipeId.ThrowIfNull("evntPipeId", "Parameter cannot be a null.");
			Type eventHolderType = typeof(T);

			EventInfo eInfo = eventHolderType.GetEvent(eventName, ReflectionUtils.PublicInstanceMembers);
			if (eInfo != null)
			{
				ServicesMapper.Register<T>(ServiceLifetime.Singleton);
				_eventPipeline.RegisterSource(_resolver.Resolve<T>(), eInfo, evntPipeId);
			}
		}

		public void RegiterEventListener<T>(string eventHandler, string evntPipeId)
		{
			eventHandler.ThrowIfNull("eventHandler", "Parameter cannot be a null.");
			evntPipeId.ThrowIfNull("evntPipeId", "Parameter cannot be a null.");
			Type eventHolderType = typeof(T);

			MethodInfo eInfo = eventHolderType.GetMethod(eventHandler, ReflectionUtils.PublicInstanceInvoke);
			if (eInfo != null)
			{
				ServicesMapper.Register<T>(ServiceLifetime.Singleton);
				_eventPipeline.RegisterSubscriber(_resolver.Resolve<T>(), eInfo, evntPipeId);
			}
		}

		public void RegiterEvent<T>()
		{
			Type eventHolderType = typeof(T);
			var query = (from e in eventHolderType.GetEvents(ReflectionUtils.PublicInstanceMembers)
						 let attributes = e.GetCustomAttributes(typeof(Event.EventSourceAttribute), true)
						 where attributes.Length >= 1
						 select new { Event = e, Attributes = attributes }).ToList();
			if (query.Count > 0)
			{
				ServicesMapper.Register<T>(ServiceLifetime.Singleton);
				object source = _resolver.Resolve<T>();
				foreach (var item in query)
				{
					_eventPipeline.RegisterSource(source, item.Event, (item.Attributes[0] as EventSourceAttribute).EventId);
				}
			}
		}

		public void RegiterEventListener<T>()
		{
			Type eventHolderType = typeof(T);
			var query = (from m in eventHolderType.GetMethods(ReflectionUtils.PublicInstanceMembers)
						 let attributes = m.GetCustomAttributes(typeof(Event.EventSubscriberAttribute), true)
						 where m.GetParameters().Length == 2 && attributes.Length >= 1
						 select new { Method = m, Attributes = attributes }).ToList();
			if (query.Count > 0)
			{
				ServicesMapper.Register<T>(ServiceLifetime.Singleton);
				object source = _resolver.Resolve<T>();
				foreach (var item in query)
				{
					_eventPipeline.RegisterSubscriber(source, item.Method, (item.Attributes[0] as EventSubscriberAttribute).EventId);
				}
			}
		}
	}
}