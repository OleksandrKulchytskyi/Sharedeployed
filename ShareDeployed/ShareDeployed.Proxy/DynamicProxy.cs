using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Common.Proxy
{
	public class DynamicProxy : DynamicObject, IDisposable
	{
		protected int disposed = -1;
		protected readonly object _target;
		private GenericWeakReference<TypeAttributesMapper> _weakMapper;
		protected SafeCollection<InterceptorInfo> _interceptors;
		private Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>> _methodInterceptors;
		protected Type _targerType;

		public DynamicProxy(object target)
		{
			if (target == null)
				throw new ArgumentNullException("Parameter target cannot be a null.");

			_methodInterceptors = new Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>>(
										() => new ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>(), true);
			_weakMapper = new GenericWeakReference<TypeAttributesMapper>(TypeAttributesMapper.Instance);
			_target = target;
			_targerType = _target.GetType();

			InitMappings();
		}

		#region protected methods
		protected void InitMappings()
		{
			if (!_weakMapper.Target.Contains(_targerType))
			{
				InterceptorAttribute[] attributes = _targerType.GetCustomAttributes(typeof(InterceptorAttribute), false) as InterceptorAttribute[];
				if (attributes != null && attributes.Length > 0)
				{
					_interceptors = new SafeCollection<InterceptorInfo>(attributes.Length);

					for (int i = 0; i < attributes.Length; i++)
					{
						InterceptorAttribute attr = attributes[i];
						if (attr != null)
						{
							InterceptorInfo info = new InterceptorInfo(attr.InterceptorType, attr.Mode, attr.EatException);
							_interceptors.Add(info);
						}
					}
					_weakMapper.Target.EmptyAndAddRange(_targerType, _interceptors);
				}
				else
				{
					_interceptors = new SafeCollection<InterceptorInfo>(attributes.Length);
					_weakMapper.Target.EmptyAndAddRange(_targerType, _interceptors);
				}
			}
			else
				_interceptors = _weakMapper.Target.GetInterceptions(_targerType);
		}

		protected IInvocation CastToInvocation(InvokeMemberBinder binder, object _target, object[] args)
		{
			return new DefaultIInvocation(_target, binder, args);
		}

		protected IInvocation CastToExceptionalInvocation(InvokeMemberBinder binder, object _target, object[] args, Exception ex)
		{
			IInvocation invocator = new DefaultIInvocation(_target, binder, args);
			invocator.SetException(ex);
			return invocator;
		}
		#endregion

		#region DynamicObject overrides

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			result = null;
			bool isFail = false;
			var beforeInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.Before);
			Console.WriteLine("before invoking " + binder.Name);
			MethodInfo mi = null;

			try
			{
				MethodCallInfo mci = new MethodCallInfo(binder.Name, binder.CallInfo.ArgumentCount, binder.CallInfo.ArgumentNames);

				if ((mi = TypeMethodsMapper.Instance.Get(_targerType, mci)) != null)
					result = mi.Invoke(_target, args);
				else
				{
					mi = _targerType.GetMethod(binder.Name, (BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance));
					if (mi != null)
					{
						TypeMethodsMapper.Instance.Add(_targerType, mci, mi);
						result = mi.Invoke(_target, args);
					}
				}
			}
			catch (Exception ex)
			{
				isFail = true;
				bool rethrow = true;
				var errorInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.OnError);
				if (errorInterceptors != null && errorInterceptors.Count() > 0)
				{
					rethrow = errorInterceptors.FirstOrDefault(x => x.EatException == true) == null ? true : false;
					foreach (InterceptorInfo interceptor in errorInterceptors)
					{
						//old way
						//IInterceptor real = Activator.CreateInstance(interceptor.Interceptor) as IInterceptor;
						//new way
						var instDel = ObjectCreatorHelper.ObjectInstantiater(interceptor.Interceptor, false);
						IInterceptor real = instDel() as IInterceptor;
						if (real != null)
							real.Intercept(CastToExceptionalInvocation(binder, _target, args, ex));
					}
				}
				else if (mi != null)
				{
					IEnumerable<InterceptorInfo> methodInterceptors = CallMethodLevelAttributes(mi);
					rethrow = methodInterceptors.FirstOrDefault(x => x.EatException == true) == null ? true : false;
					foreach (InterceptorInfo interceptor in methodInterceptors)
					{
						var instDel = ObjectCreatorHelper.ObjectInstantiater(interceptor.Interceptor, false);
						IInterceptor real = instDel() as IInterceptor;
						if (real != null)
							real.Intercept(CastToExceptionalInvocation(binder, _target, args, ex));
					}
				}
				if (rethrow)
					throw;
			}

			if (isFail && mi.ReturnType != typeof(void))
			{
				result = mi.ReturnType.GetDefaultValue();
			}

			var afterInterceptors = _interceptors.Where(x => x.Mode == ExecutionInjectionMode.After);
			Console.WriteLine("after invoking " + binder.Name);
			return true;
		}

		private IEnumerable<InterceptorInfo> CallMethodLevelAttributes(MethodInfo mi)
		{
			IList<InterceptorInfo> interceptors = null;
			InterceptorAttribute[] attributes = mi.GetCustomAttributes(typeof(InterceptorAttribute), false) as InterceptorAttribute[];
			if (attributes != null && attributes.Length > 0)
			{
				if (!_methodInterceptors.Value.ContainsKey(mi))
				{
					interceptors = new List<InterceptorInfo>(attributes.Length);
					for (int i = 0; i < attributes.Length; i++)
					{
						InterceptorAttribute attr = attributes[i];
						if (attr != null)
						{
							InterceptorInfo info = new InterceptorInfo(attr.InterceptorType, attr.Mode, attr.EatException);
							interceptors.Add(info);
						}
					}
					_methodInterceptors.Value.TryAdd(mi, interceptors);
				}
				else
					_methodInterceptors.Value.TryGetValue(mi, out interceptors);
			}

			return interceptors;
		}

		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			if (binder.Type == typeof(IDisposable))
			{
				result = this;
				return true;
			}

			if (_target != null && binder.Type.IsAssignableFrom(_targerType))
			{
				result = _target;
				return true;
			}
			else
				return base.TryConvert(binder, out result);
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = null;
			MemberInfo member = _targerType.GetMember(binder.Name).FirstOrDefault();
			switch (member.MemberType)
			{
				case MemberTypes.Field:
					result = ((FieldInfo)member).GetValue(_target);
					break;
				case MemberTypes.Property:
					result = ((PropertyInfo)member).GetValue(_target, null);
					break;
				default:
					throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo", binder.Name);
			}

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			FieldInfo fieldInfo = _targerType.GetField(binder.Name);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(_target, value);
				return true;
			}
			else
			{
				PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
				if (pInfo != null && pInfo.CanWrite)
				{
					pInfo.SetValue(_target, value, null);
					return true;
				}
			}
			return false;
		}

		public override DynamicMetaObject GetMetaObject(System.Linq.Expressions.Expression parameter)
		{
			return base.GetMetaObject(parameter);
		}

		public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
		{
			return base.TryInvoke(binder, args, out result);
		}

		public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
		{
			return base.TryCreateInstance(binder, args, out result);
		}

		#endregion

		#region IDisposable
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && System.Threading.Interlocked.CompareExchange(ref this.disposed, 1, -1) == -1)
			{
				if ((_target as IDisposable) != null)
					(_target as IDisposable).Dispose();
			}
		}
		#endregion
	}

	public sealed class AdvancedDynamicProxy : DynamicProxy
	{
		public AdvancedDynamicProxy(object target)
			: base(target)
		{

		}

		public T GetAbstraction<T>()
		{
			T abstr = (this as dynamic);
			return abstr;
		}
	}
}