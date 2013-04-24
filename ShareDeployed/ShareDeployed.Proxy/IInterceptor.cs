using System;
using System.Dynamic;
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

		Exception Exception { get; }

		void SetException(Exception ex);
	}

	public class DefaultIInvocation : IInvocation
	{
		private object _target;
		private InvokeMemberBinder _invokeMemberBinder;

		public DefaultIInvocation(object target, InvokeMemberBinder binder, object[] args)
		{
			_target = target;
			_invokeMemberBinder = binder;
			_args = args;
		}

		private object[] _args;
		public object[] Arguments
		{
			get
			{
				return _args;
			}
			private set
			{
				_args = value;
			}
		}

		public object GetArgumentValue(int index)
		{
			if (_args != null && _args.Length > 0 &&
				(_args.Length - 1 >= index || index >= 0))
			{
				return _args[index];
			}
			return null;
		}

		public void SetArgumentValue(int index, object value)
		{
			if (_args != null && _args.Length > 0 &&
				(_args.Length - 1 >= index || index >= 0))
			{
				_args[index] = value;
			}
		}

		public MethodInfo GetConcreteMethod()
		{
			throw new NotImplementedException();
		}

		public MethodInfo GetConcreteMethodInvocationTarget()
		{
			return _target.GetType().GetMethod(_invokeMemberBinder.Name);
		}

		public virtual void Proceed()
		{
			if (MethodInvocationTarget != null)
				MethodInvocationTarget.Invoke(_target, _args);
		}

		public Type[] GenericArguments
		{
			get { throw new NotImplementedException(); }
		}

		public object InvocationTarget
		{
			get { return _target; }
		}

		public MethodInfo Method
		{
			get { return null; }
		}

		public MethodInfo MethodInvocationTarget
		{
			get { return _target.GetType().GetMethod(_invokeMemberBinder.Name); }
		}

		public object Proxy
		{
			get { throw new NotImplementedException(); }
		}

		public object ReturnValue
		{
			get
			{
				return _invokeMemberBinder.ReturnType;
			}
			set { }
		}

		public Type TargetType
		{
			get { return _target.GetType(); }
		}

		private Exception _ex;
		public Exception Exception
		{
			get { return _ex; }
		}

		public void SetException(Exception ex)
		{
			_ex = ex;
		}
	}
}