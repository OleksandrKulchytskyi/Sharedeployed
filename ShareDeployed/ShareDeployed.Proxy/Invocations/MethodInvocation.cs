using System;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public class MethodInvocation : IInvocation
	{
		private object _target;
		private InvokeMemberBinder _invokeMemberBinder;

		public MethodInvocation(object target, InvokeMemberBinder binder, object[] args)
		{
			target.ThrowIfNull("target", "Property cannot be a null.");
			binder.ThrowIfNull("binder", "Property cannot be a null.");

			_target = target;
			_invokeMemberBinder = binder;
			_args = args;
		}

		public MethodInvocation(object target, InvokeMemberBinder binder, object[] args, Type retType)
			: this(target, binder, args)
		{
			_returnType = retType;
		}

		public MethodInvocation(object target, InvokeMemberBinder binder, object[] args, object proxy)
			: this(target, binder, args)
		{
			proxy.ThrowIfNull("proxy", "Property cannot be a null.");
			_proxy = proxy;
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

		public void SetHadlingMethod(MethodInfo mi)
		{
			mi.ThrowIfNull("mi", "Parameter cannot be a null.");
			_methodInvocationTarget = mi;
			wasGot = true;
		}

		public MethodInfo GetConcreteMethodInvocationTarget()
		{
			if (_methodInvocationTarget == null)
			{
				Type[] argsTypes = (from v in _args select v.GetType()).ToArray();
				return _target.GetType().GetMethod(_invokeMemberBinder.Name, argsTypes);
			}
			return _methodInvocationTarget;
		}

		public virtual void Proceed()
		{
			if (MethodInvocationTarget != null)
			{
				if (ReturnValueType == null) ReturnValueType = MethodInvocationTarget.ReturnType;
				ReturnValue = MethodInvocationTarget.Invoke(_target, _args);
			}
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

		bool wasGot = false;
		private MethodInfo _methodInvocationTarget;
		public MethodInfo MethodInvocationTarget
		{
			get
			{
				if (!wasGot)
				{
					_methodInvocationTarget = GetConcreteMethodInvocationTarget();
					return _methodInvocationTarget;
				}
				return _methodInvocationTarget;
			}
		}

		private object _proxy;
		public object Proxy
		{
			get { return _proxy; }
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

		private Type _returnType;
		public Type ReturnValueType
		{
			get { return _returnType; }
			set { _returnType = value; }
		}

		private StackTrace _trace;
		public StackTrace StackTrace
		{
			get { return _trace; }
			set { _trace = value; }
		}

		public MethodInfo CallingMethod
		{
			get { return (MethodInfo)_trace.GetFrame(0).GetMethod(); }
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("Calling Method: {0,30:G}\n", GetMethodName(CallingMethod));
			builder.AppendFormat("Target Method:{0,30:G}\n", GetMethodName(MethodInvocationTarget));
			builder.AppendLine("Arguments:");

			foreach (ParameterInfo info in MethodInvocationTarget.GetParameters())
			{
				object currentArgument = _args[info.Position];
				if (currentArgument == null)
					currentArgument = "(null)";
				builder.AppendFormat("\t{0,10:G}: {1}\n", info.Name, currentArgument.ToString());
			}
			builder.AppendLine();

			return builder.ToString();
		}

		private string GetMethodName(MethodInfo method)
		{
			if (method == null)
				return string.Empty;

			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("{0}.{1}", method.DeclaringType.Name, method.Name);
			builder.Append("(");

			ParameterInfo[] parameters = method.GetParameters();
			int parameterCount = parameters != null ? parameters.Length : 0;

			int index = 0;
			foreach (ParameterInfo param in parameters)
			{
				index++;
				builder.AppendFormat("{0} {1}", param.ParameterType.Name, param.Name);

				if (index < parameterCount)
					builder.Append(", ");
			}
			builder.Append(")");

			return builder.ToString();
		}
	}
}
