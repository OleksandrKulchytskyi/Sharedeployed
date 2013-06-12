using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ShareDeployed.Proxy.Event
{
	public interface IEventPipelineBuilder
	{
		IEventBrokerPipeline BuildFor(params Assembly[] assemblies);
	}

	public class EventPipelineBuilder : IEventPipelineBuilder
	{
		public IEventBrokerPipeline BuildFor(params Assembly[] assemblies)
		{
			EventBrokerPipeline pipeline = new EventBrokerPipeline();
			foreach (Assembly asm in assemblies)
			{
				BuildInternal(pipeline, asm);
			}

			return pipeline;
		}

		private void BuildInternal(EventBrokerPipeline pipeline, Assembly asm)
		{
			var evntSubscriber = (from t in asm.GetTypes().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount - 1)
								  where TypePredicate(t)
								  from m in t.GetMethods(ReflectionUtils.PublicInstanceInvoke)
								  let attributes = m.GetCustomAttributes(typeof(Event.EventSubscriberAttribute), true)
								  where attributes.Length >= 1
								  let metadata = new { Method = m, Attributes = attributes }
								  select new { Type = t, Metadata = metadata }).ToList();

			var evntSources = (from t in asm.GetTypes().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount - 1)
							   where TypePredicate(t)
							   from e in t.GetEvents(ReflectionUtils.PublicInstanceMembers)
							   let attributes = e.GetCustomAttributes(typeof(Event.EventSourceAttribute), true)
							   where attributes.Length >= 1
							   let metadata = new { Event = e, Attributes = attributes }
							   select new { Type = t, Metadata = metadata }).ToList();
		}

		private bool TypePredicate(Type t)
		{
			return (t != null && t.IsPublic && !t.IsNested && !t.IsInterface);
		}
	}
}
