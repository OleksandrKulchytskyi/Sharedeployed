using System;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	public interface IInterceptor
	{
		void Intercept(IInvocation invocation);
	}

	public interface IInvocation
	{
		object[] Arguments { get; }
		object GetArgumentValue(int index);
		void SetArgumentValue(int index, object value);

		MethodInfo GetConcreteMethod();
		MethodInfo GetConcreteMethodInvocationTarget();

		void Proceed();

		Type[] GenericArguments { get; }
		object InvocationTarget { get; }
		MethodInfo Method { get; }
		MethodInfo MethodInvocationTarget { get; }
		object Proxy { get; }
		object ReturnValue { get; set; }
		Type TargetType { get; }
	}

	public interface IProxy
	{
		void AddInterceptor<TBase>(IInterceptor<TBase> interceptor);
	}

	public interface IInterceptor<TBase>
	{
		void Handle(IMethodInvocation<TBase> methodInvk);
	}

	public interface IMethodInvocation<TBase>
	{
		void Continue();
	}

	/// <summary>
	/// Default interceptor for the proxy.
	/// </summary>
	/// <typeparam name="TBase">The base type.</typeparam>
	public class DefaultInterceptor<TBase> : IInterceptor<TBase> where TBase : class
	{
		/// <summary>
		/// Handles the specified method invocation.
		/// </summary>
		/// <param name="methodInvocation">The method invocation.</param>
		public void Handle(IMethodInvocation<TBase> methodInvocation)
		{
			methodInvocation.Continue();
		}
	}
}