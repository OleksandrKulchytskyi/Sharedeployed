using System;

namespace ShareDeployed.Common.Proxy
{
	public enum ExecutionInjectionMode
	{
		None = 0,
		Before,
		After,
		OnError
	}

	public class InterceptorInfo
	{
		public InterceptorInfo(Type interceptorType, ExecutionInjectionMode mode) :
			this(interceptorType, mode, false)
		{
		}

		public InterceptorInfo(Type interceptorType, ExecutionInjectionMode mode, bool eatException)
		{
			Interceptor = interceptorType;
			Mode = mode;
			EatException = eatException;
		}

		public Type Interceptor { get; private set; }
		public ExecutionInjectionMode Mode { get; private set; }
		public bool EatException { get; private set; }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
	public sealed class InterceptorAttribute : Attribute
	{
		public Type InterceptorType { get; set; }
		public ExecutionInjectionMode Mode { get; set; }
		public bool EatException { get; set; }
	}
}
