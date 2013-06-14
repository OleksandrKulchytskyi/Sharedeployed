using ShareDeployed.Proxy.Event;
using ShareDeployed.Proxy.Logging;
using System;
using System.Collections.Generic;

namespace ShareDeployed.Proxy
{
	public enum InterceptorMode
	{
		None = 0, Before, After, OnError
	}

	public interface IBuildAware
	{
		void OnBuilt();
	}

	public interface IPipeline
	{
		void Initialize(bool withinDomain = false);

		ILogAggregator LoggerAggregator { get; set; }
		IContractResolver ContracResolver { get; set; }
		IDynamicProxyManager DynamixProxyManager { get; set; }
		IEventBrokerPipeline EventPipeline { get; }
	}

	public interface IConfigurable
	{
		void Configure();
	}

	public sealed class ResolutionFailEventArgs : EventArgs
	{
		public ResolutionFailEventArgs()
			: base()
		{
		}

		public ResolutionFailEventArgs(Type t, Exception ex)
		{
			ResolveType = t;
			ResolutionError = ex;
		}

		public ResolutionFailEventArgs(Type resolvingType, string msg)
		{
			ResolveType = resolvingType;
			ErrorMessage = msg;
		}

		public Type ResolveType { get; set; }

		public Exception ResolutionError { get; set; }

		public string ErrorMessage { get; set; }
	}

	public interface IContractResolver
	{
		bool OmitNotRegistred { get; set; }
		IEventBrokerRegistrator EventRegistrator { get; }

		event EventHandler<ResolutionFailEventArgs> ResolveFailed;

		object Resolve(Type contract);
		object Resolve(string alias);
		T Resolve<T>();
		T Resolve<T>(string alias);
		IEnumerable<object> ResolveAll(params Type[] types);

		void Unregister<T>();
		void Unregister(Type contract);
	}

	public class InterceptorInfo
	{
		public InterceptorInfo(Type interceptorType, InterceptorMode mode) :
			this(interceptorType, mode, false)
		{
		}

		public InterceptorInfo(Type interceptorType, InterceptorMode mode, bool eatException)
		{
			Interceptor = interceptorType;
			Mode = mode;
			EatException = eatException;
		}

		public Type Interceptor { get; private set; }
		public InterceptorMode Mode { get; private set; }
		public bool EatException { get; private set; }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class InterceptorAttribute : Attribute
	{
		public Type InterceptorType { get; set; }
		public InterceptorMode Mode { get; set; }
		public bool EatException { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class GetInstanceAttribute : Attribute
	{
		public string Alias { get; set; }
		public Type TypeOf { get; set; }
	}

	/// <summary>
	/// Markable attribute, just for gettin values from IContractResolver in DynamicProxyPipeline
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public sealed class InstantiateAttribute : Attribute
	{
		public InstantiateAttribute()
		{
		}

		public InstantiateAttribute(bool defaultIfUnable)
		{
			this.DefaultIfUnable = defaultIfUnable;
		}

		public bool DefaultIfUnable { get; set; }
	}

	[AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	sealed class InjectionConstructorAttribute : Attribute
	{
		public InjectionConstructorAttribute()
		{
		}
	}

	/// <summary>
	/// A wrapper for value type.  Must be used in order for Fasterflect to 
	/// work with value type such as struct.
	/// </summary>
	internal class ValueTypeHolder
	{
		/// <summary>
		/// Creates a wrapper for <paramref name="value"/> value type.  The wrapper
		/// can then be used with Fasterflect.
		/// </summary>
		/// <param name="value">The value type to be wrapped.  
		/// Must be a derivative of <code>ValueType</code>.</param>
		public ValueTypeHolder(object value)
		{
			Value = (ValueType)value;
		}

		/// <summary>
		/// The actual struct wrapped by this instance.
		/// </summary>
		public ValueType Value { get; set; }
	}
}
