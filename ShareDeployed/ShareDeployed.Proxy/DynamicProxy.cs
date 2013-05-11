using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace ShareDeployed.Proxy
{
	public class DynamicProxy : DynamicObject, IDisposable
	{
		private readonly string _targetParamName = "target";
		private readonly string _paramCannotBeNullMsg = "Parameter target cannot be a null.";

		protected int disposed = -1;
		protected int _initialized = 0;
		private bool _useFastProp;
		private bool _useDynamicDel;
		protected object _target;
		protected GenericWeakReference<object> _weakTarget;
		protected Type _targerType;

		private GenericWeakReference<TypeAttributesMapper> _weakMapper;
		protected SafeCollection<InterceptorInfo> _interceptors;
		private Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>> _methodInterceptors;

		private GenericWeakReference<IContractResolver> _resolver;

		#region ctors
		/// <summary>
		/// Empty ctor
		/// </summary>
		public DynamicProxy()
		{
			if (DynamicProxyPipeline.Instance != null)
				_resolver = new GenericWeakReference<IContractResolver>(DynamicProxyPipeline.Instance.ContracResolver);
			_methodInterceptors = new Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>>(() =>
									new ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>(), true);
			_weakMapper = new GenericWeakReference<TypeAttributesMapper>(TypeAttributesMapper.Instance);
		}

		/// <summary>
		/// ctor. Initiatiates new instance and uses FastProperty wrappers
		/// </summary>
		/// <param name="target">Proxy target</param>
		public DynamicProxy(object target)
			: this(target, true)
		{
		}

		/// <summary>
		/// ctor. Initiatiates new instance and uses FastProperty wrappers
		/// </summary>
		/// <param name="target">Proxy target</param>
		/// <param name="useFastProp">Use FastProperty wrapper for managing property calls</param>
		public DynamicProxy(object target, bool useFastProp)
			: this(target, useFastProp, false)
		{
		}

		/// <summary>
		/// ctor.
		/// </summary>
		/// <param name="target">Proxy target</param>
		/// <param name="useFastProperty">IUse FastProperty</param>
		/// <param name="useDynamicDelegates">Use DynamicMethodDelegate for fast methods calls</param>
		public DynamicProxy(object target, bool useFastProperty, bool useDynamicDelegates)
			: base()
		{
			target.ThrowIfNull(_targetParamName, _paramCannotBeNullMsg);

			_useFastProp = useFastProperty;
			_target = target;
			_targerType = _target.GetType();

			InitMappings();
		}
		#endregion

		#region Properties
		/// <summary>
		/// Flag that indicates whether proxy wrapper will be using DynamicMethodDelegate for method calls
		/// </summary>
		public virtual bool UseFastProperties
		{
			get { return _useFastProp; }
			set { if (value != _useFastProp) _useFastProp = value; }
		}

		/// <summary>
		/// Flag that indicates whether proxy wrapper will be using FastPropery wrappers for property manipulations
		/// </summary>
		public virtual bool UseDynamicDelegates
		{
			get { return _useDynamicDel; }
			set { if (value != _useDynamicDel) _useDynamicDel = value; }
		}
		#endregion

		/// <summary>
		/// Set target object for proxy instance
		/// </summary>
		/// <param name="target"></param>
		/// <param name="isWeak">if true, wrap object to weak reference</param>
		public virtual void SetTargetObject(object target, bool isWeak = false)
		{
			target.ThrowIfNull(_targetParamName, _paramCannotBeNullMsg);

			if (isWeak)
				_weakTarget = new GenericWeakReference<object>(target);
			else
				_target = target;
			_targerType = target.GetType();
			InitMappings();
		}

		#region protected methods
		protected void InitMappings()
		{
			if (System.Threading.Interlocked.CompareExchange(ref _initialized, 1, 1) == 1)
				return;

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

		protected IInvocation CreateMethodInvocation(InvokeMemberBinder binder, object _target, object[] args, Exception exc = null)
		{
			IInvocation invocation = new MethodInvocation(_target, binder, args);
			if (exc != null)
				invocation.SetException(exc);
			return invocation;
		}

		/// <summary>
		/// Check whether weak reference to a target object is still alive,otherwise WeakObjectDisposedException will be thrown
		/// <exception ref="ShareDeployed.Proxy.WeakObjectDisposedException"></exception>
		/// </summary>
		protected void CheckIfWeakRefIsAlive()
		{
			if (_weakTarget != null)
			{
				if (!_weakTarget.IsAlive && _weakTarget.Target == null)
					throw new WeakObjectDisposedException(string.Format("Dynamic proxy target object {0}, has been disposed.", _targerType));
			}
		}
		#endregion

		#region DynamicObject overrides

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			result = null;
			bool isFail = false;
			bool processed = false;
			MethodInfo mi = null;
			CheckIfWeakRefIsAlive();

			MethodCallInfo mci = new MethodCallInfo(binder.Name, binder.CallInfo.ArgumentCount, binder.CallInfo.ArgumentNames);
			IInvocation methodInvocation = CreateMethodInvocation(binder, _weakTarget == null ? _target : _weakTarget.Target, args);
			if ((mi = TypeMethodsMapper.Instance.Get(_targerType, mci)) == null)
			{
				mi = _targerType.GetMethod(binder.Name, ReflectionUtils.PublicInstanceInvoke);
				if (mi != null) TypeMethodsMapper.Instance.Add(_targerType, mci, mi);
			}

			var beforeInterceptors = _interceptors.Where(x => x.Mode == InterceptorInjectionMode.Before);

			try
			{
				if (!_useDynamicDel)
				{
					result = mi.Invoke(_weakTarget == null ? _target : _weakTarget.Target, args);
					processed = true;
				}
				else
				{
					result = TypeMethodsMapper.Instance.GetDynamicDelegate(_targerType, mci)(_targerType, args);
					processed = true;
				}

				if (!processed)
					throw new TargetInvocationException("Method wasn't found.",
							new InvalidOperationException(string.Format("Fail to find method {0}, in type {1}", binder.Name, _targerType)));
			}
			catch (Exception ex)
			{
				methodInvocation.SetException(ex);
				isFail = true;
				bool rethrow = true;
				var errorInterceptors = _interceptors.Where(x => x.Mode == InterceptorInjectionMode.OnError);
				if (errorInterceptors != null && errorInterceptors.Count() > 0)
				{
					rethrow = errorInterceptors.FirstOrDefault(x => x.EatException == true) == null ? true : false;
					foreach (InterceptorInfo interceptor in errorInterceptors)
					{
						InterceptInternal(methodInvocation, interceptor);
					}
				}
				else if (mi != null)
				{
					IEnumerable<InterceptorInfo> methodInterceptors = CallMethodLevelAttributes(mi);
					rethrow = methodInterceptors.FirstOrDefault(x => x.EatException == true) == null ? true : false;
					foreach (InterceptorInfo interceptor in methodInterceptors)
					{
						InterceptInternal(methodInvocation, interceptor);
					}
				}
				if (rethrow) throw;
			}

			if (isFail && mi.ReturnType != typeof(void))
				result = mi.ReturnType.GetDefaultValue();

			var afterInterceptors = _interceptors.Where(x => x.Mode == InterceptorInjectionMode.After);

			return true;
		}

		private static void InterceptInternal(IInvocation methodInvocation, InterceptorInfo interceptor)
		{
			methodInvocation.ThrowIfNull("methodInvocation", "Parameter cannot be a null.");
			interceptor.ThrowIfNull("interceptor", "Parameter cannot be a null.");

			IContractResolver resolver = DynamicProxyPipeline.Instance.ContracResolver;
			object resolved = resolver.Resolve(interceptor.Interceptor);

			IInterceptor casted = resolved as IInterceptor;
			if (casted != null)
				casted.Intercept(methodInvocation);
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
			object tgt = _weakTarget == null ? _target : _weakTarget.Target;
			if (tgt != null && binder.Type.IsAssignableFrom(_targerType))
			{
				result = tgt;
				return true;
			}
			else
				return base.TryConvert(binder, out result);
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = null;
			CheckIfWeakRefIsAlive();
			MemberInfo member = _targerType.GetMember(binder.Name).FirstOrDefault();
			switch (member.MemberType)
			{
				case MemberTypes.Field:
					result = ((FieldInfo)member).GetValue(_weakTarget == null ? _target : _weakTarget.Target);
					break;
				case MemberTypes.Property:
					if (_useFastProp)
					{
						FastReflection.FastProperty fProp = null;
						if ((fProp = TypePropertyMapper.Instance.Get(_targerType, binder.Name)) == null)
						{
							PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
							if (pInfo != null)
							{
								TypePropertyMapper.Instance.Add(_targerType, pInfo, out fProp);
								result = fProp.Get(_weakTarget == null ? _target : _weakTarget.Target);
							}
						}
						else
							result = fProp.Get(_weakTarget == null ? _target : _weakTarget.Target);
					}
					else
					{
						PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
						if (pInfo != null)
							result = pInfo.GetValue(_weakTarget == null ? _target : _weakTarget.Target, null);
					}

					break;
				default:
					throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo", binder.Name);
			}

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			CheckIfWeakRefIsAlive();

			FieldInfo fieldInfo = _targerType.GetField(binder.Name);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(_weakTarget == null ? _target : _weakTarget.Target, value);
				return true;
			}
			else
			{
				if (_useFastProp)
				{
					FastReflection.FastProperty fProp = null;
					if ((fProp = TypePropertyMapper.Instance.Get(_targerType, binder.Name)) == null)
					{
						PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
						if (pInfo != null)
						{
							TypePropertyMapper.Instance.Add(_targerType, pInfo, out fProp);
							fProp.Set(_weakTarget == null ? _target : _weakTarget.Target, value);
							return true;
						}
					}
					else
					{
						fProp.Set(_weakTarget == null ? _target : _weakTarget.Target, value);
						return true;
					}
				}
				else
				{
					PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
					if (pInfo != null)
					{
						pInfo.SetValue(_weakTarget == null ? _target : _weakTarget.Target, value, null);
						return true;
					}
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
				object target = _weakTarget == null ? _target : _weakTarget.Target;
				if ((target as IDisposable) != null)
					(target as IDisposable).Dispose();

				if (_target != null)
					_target = null;//make rootless, for better GC
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

		public AdvancedDynamicProxy(object target, bool userFastProp)
			: base(target, userFastProp)
		{
		}

		public T GetAbstraction<T>()
		{
			T abstr = (this as dynamic);
			return abstr;
		}
	}
}