namespace ShareDeployed.Common.Proxy
{
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