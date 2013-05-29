using System;
using System.Diagnostics;
using System.Reflection;

namespace ShareDeployed.Proxy
{
	public delegate object InvocationHandler(IInvocation invocation);
	public delegate object InterceptorHandler(object proxy, MethodInfo targetMethod,
											  StackTrace trace, Type[] genericTypeArgs, object[] args);

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

		void SetHadlingMethod(MethodInfo mi);

		MethodInfo MethodInvocationTarget { get; }

		object Proxy { get; }

		object ReturnValue { get; set; }
		Type ReturnValueType { get; set; }

		Type TargetType { get; }

		Exception Exception { get; }

		void SetException(Exception ex);

		StackTrace StackTrace { get; }

		MethodInfo CallingMethod { get; }
	}
}