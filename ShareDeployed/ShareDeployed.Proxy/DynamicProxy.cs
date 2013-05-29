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
		private readonly string _paramCannotBeNullMsg = "Parameter cannot be a null.";

		private System.Threading.SpinLock _spinLock;

		protected int disposed = -1;
		protected int _typeHash;
		protected int _initialized = 0;
		private bool _useFastProp;
		private bool _useDynamicDel;
		//dynamic proxy target object
		protected object _target;
		//uses in case whem dynamic proxy is short-lived object and its target is long lived (gain better GC and prevent potential MemoryLeak)
		protected GenericWeakReference<object> _weakTarget;
		//type of dynamic proxy target object
		protected Type _targerType;

		private GenericWeakReference<TypeAttributesMapper> _weakMapper;
		//class level interceptors
		protected SafeCollection<InterceptorInfo> _interceptors;
		//method level interceptors
		private Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>> _methodInterceptors;
		//current service reolver
		private GenericWeakReference<IContractResolver> _resolver = null;

		#region ctors
		/// <summary>
		/// Empty ctor
		/// </summary>
		public DynamicProxy()
		{
			if (DynamicProxyPipeline.Instance != null)
				_resolver = new GenericWeakReference<IContractResolver>(DynamicProxyPipeline.Instance.ContracResolver);

			InitInternals();
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
			_typeHash = _targerType.GetHashCode();

			InitInternals();

			InitMappings();
		}

		private void InitInternals()
		{
			if (_weakMapper == null)
				_weakMapper = new GenericWeakReference<TypeAttributesMapper>(TypeAttributesMapper.Instance);

			if (_methodInterceptors == null)
				_methodInterceptors = new Lazy<ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>>(() => new ConcurrentDictionary<MethodInfo, IList<InterceptorInfo>>(), true);

			_spinLock = new System.Threading.SpinLock();
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
			_typeHash = _typeHash.GetHashCode();
			InitMappings();
		}

		#region protected methods
		protected void InitMappings()
		{
			if (System.Threading.Interlocked.CompareExchange(ref _initialized, 1, 1) == 1)
				return;

			if (!_weakMapper.Target.Contains(_typeHash))
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
					_weakMapper.Target.EmptyAndAddRange(_typeHash, _interceptors);
				}
				else
				{
					_interceptors = new SafeCollection<InterceptorInfo>(attributes.Length);
					_weakMapper.Target.EmptyAndAddRange(_typeHash, _interceptors);
				}
			}
			else
				_interceptors = _weakMapper.Target.GetInterceptions(_typeHash);
		}

		protected IInvocation CreateMethodInvocation(InvokeMemberBinder binder, object _target, object[] args, Exception exc = null, Type returnType = null, MethodInfo mi = null)
		{
			IInvocation invocation = null;
			EnterCritical(ref _spinLock, () =>
			{
				if (returnType == null)
					invocation = new MethodInvocation(_target, binder, args);
				else
					invocation = new MethodInvocation(_target, binder, args, returnType);

				if (exc != null) invocation.SetException(exc);
				if (mi != null) invocation.SetHadlingMethod(mi);
			});
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
				object target = _weakTarget.Target;
				if (!_weakTarget.IsAlive && target == null)
					throw new WeakObjectDisposedException(string.Format("Dynamic proxy target object {0}, has been disposed.", _targerType));
			}
		}

		protected void EnterCritical(ref System.Threading.SpinLock sl, Action action)
		{
			bool lockTaken = false;
			try
			{
				sl.TryEnter(ref lockTaken);
				action();
			}
			finally
			{
				if (lockTaken)
					sl.Exit(false);
			}
		}
		#endregion

		#region DynamicObject overrides

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			CheckIfWeakRefIsAlive();
			result = null;
			bool isFail = false;
			bool processed = false;
			MethodInfo mi = null;

			MethodCallInfo mci;
			Type[] argsTypes;
			if (TryParseParametersTypes(args, out argsTypes))
				mci = new MethodCallInfo(binder.Name, binder.CallInfo.ArgumentCount, binder.CallInfo.ArgumentNames, RetrieveHashCode(argsTypes));
			else
				mci = new MethodCallInfo(binder.Name, binder.CallInfo.ArgumentCount, binder.CallInfo.ArgumentNames);

			if ((mi = TypeMethodsMapper.Instance.Get(_typeHash, ref mci)) == null)
			{
				try
				{
					if (argsTypes == null) mi = _targerType.GetMethod(binder.Name, ReflectionUtils.PublicInstanceInvoke);
					else mi = _targerType.GetMethod(binder.Name, argsTypes);
				}
				catch (AmbiguousMatchException ex)
				{
					IContractResolver ioc = this._resolver.Target;
					if (ioc != null)
					{
						Logging.ILogAggregator aggr = ioc.Resolve<Logging.ILogAggregator>();
						if (aggr != null) aggr.DoLog(Logging.LogSeverity.Error, ex.Message, ex);
					}
				}

				if (mi != null) TypeMethodsMapper.Instance.Add(_typeHash, ref mci, mi);
			}

			IInvocation methodInvocation = CreateMethodInvocation(binder, _weakTarget == null ? _target : _weakTarget.Target,
											args, returnType: mi.ReturnType, mi: mi);

			IEnumerable<InterceptorInfo> beforeInterceptors = _interceptors.Where(x => x.Mode == InterceptorMode.Before);
			if (beforeInterceptors.Count() > 0)
				ProcessBeforeExecuteInterceptors(methodInvocation, beforeInterceptors);
			else
			{
				EnterCritical(ref _spinLock, () =>
				{
					beforeInterceptors = GetMethodLevelInterceptors(mi, InterceptorMode.Before);
					ProcessBeforeExecuteInterceptors(methodInvocation, beforeInterceptors);
				});
			}

			try
			{
				if (!_useDynamicDel)
				{
					result = mi.Invoke(_weakTarget == null ? _target : _weakTarget.Target, args);
					processed = true;
				}
				else
				{
					result = TypeMethodsMapper.Instance.GetDynamicDelegate(_typeHash, ref mci)(_targerType, args);
					processed = true;
				}

				if (!processed)
					throw new TargetInvocationException("Method wasn't found.",
							new InvalidOperationException(string.Format("Fail to find method {0}, in type {1}", binder.Name, _targerType)));
			}
			catch (Exception ex)
			{
				EnterCritical(ref _spinLock, () => methodInvocation.SetException(ex));

				isFail = true;
				bool rethrow = true;
				var errorInterceptors = _interceptors.Where(x => x.Mode == InterceptorMode.OnError);
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
					IEnumerable<InterceptorInfo> methodInterceptors;
					EnterCritical(ref _spinLock, () =>
					{
						methodInterceptors = GetMethodLevelInterceptors(mi, InterceptorMode.OnError);
						rethrow = methodInterceptors.FirstOrDefault(x => x.EatException == true) == null ? true : false;
						foreach (InterceptorInfo interceptor in methodInterceptors)
						{
							InterceptInternal(methodInvocation, interceptor);
						}
					});
				}
				if (rethrow) throw;
			}

			if (isFail && mi.ReturnType != typeof(void))
				result = mi.ReturnType.GetDefaultValue();

			IEnumerable<InterceptorInfo> afterInterceptors = _interceptors.Where(x => x.Mode == InterceptorMode.After);
			if (afterInterceptors.Count() > 0)
				ProcessAfterExecuteInterceptors(methodInvocation, afterInterceptors);
			else
			{
				EnterCritical(ref _spinLock, () =>
				{
					afterInterceptors = GetMethodLevelInterceptors(mi, InterceptorMode.After);
					ProcessAfterExecuteInterceptors(methodInvocation, afterInterceptors);
				});
			}

			return true;
		}

		private bool TryParseParametersTypes(object[] paramsValue, out Type[] parsedTypes)
		{
			if (paramsValue == null)
			{
				parsedTypes = null;
				return false;
			}

			parsedTypes = new Type[paramsValue.Length];
			for (int i = 0; i < paramsValue.Length; i++)
			{
				parsedTypes[i] = paramsValue[i].GetType();
			}
			return true;
		}

		private int RetrieveHashCode(Type[] types)
		{
			int hash = 0;
			foreach (Type item in types)
			{
				hash += item.GetHashCode();
			}
			return hash;
		}

		private void ProcessBeforeExecuteInterceptors(IInvocation invocation, IEnumerable<InterceptorInfo> beforeInterceptors)
		{
			invocation.ThrowIfNull("invocation", _paramCannotBeNullMsg);
			beforeInterceptors.ThrowIfNull("beforeInterceptors", _paramCannotBeNullMsg);
			foreach (InterceptorInfo item in beforeInterceptors)
			{
				IInterceptor casted = ResolveInterceptorFromIoC(item);
				if (casted != null) casted.Intercept(invocation);
			}
		}

		private void InterceptInternal(IInvocation methodInvocation, InterceptorInfo interceptor)
		{
			methodInvocation.ThrowIfNull("methodInvocation", _paramCannotBeNullMsg);
			interceptor.ThrowIfNull("interceptor", _paramCannotBeNullMsg);

			IInterceptor casted = ResolveInterceptorFromIoC(interceptor);
			if (casted != null) casted.Intercept(methodInvocation);
		}

		private void ProcessAfterExecuteInterceptors(IInvocation invocation, IEnumerable<InterceptorInfo> afterInterceptors)
		{
			invocation.ThrowIfNull("invocation", _paramCannotBeNullMsg);
			afterInterceptors.ThrowIfNull("afterInterceptors", _paramCannotBeNullMsg);
			foreach (InterceptorInfo item in afterInterceptors)
			{
				IInterceptor casted = ResolveInterceptorFromIoC(item);
				if (casted != null) casted.Intercept(invocation);
			}
		}

		protected IInterceptor ResolveInterceptorFromIoC(InterceptorInfo item)
		{
			IContractResolver resolver = DynamicProxyPipeline.Instance.ContracResolver;
			object resolved = resolver.Resolve(item.Interceptor);
			return (resolved as IInterceptor);
		}

		protected SafeCollection<InterceptorInfo> GetMethodLevelInterceptors(MethodInfo mi, InterceptorMode mode = InterceptorMode.None)
		{
			SafeCollection<InterceptorInfo> localInterc = null;
			InterceptorAttribute[] attributes = mi.GetCustomAttributes(typeof(InterceptorAttribute), false) as InterceptorAttribute[];
			if (attributes != null && attributes.Length > 0)
			{
				//here we might have a situation when we chached only class level interceptors but method level interceptors left untouched
				if (!_methodInterceptors.Value.ContainsKey(mi))
				{
					localInterc = new SafeCollection<InterceptorInfo>(attributes.Length);
					for (int i = 0; i < attributes.Length; i++)
					{
						InterceptorAttribute attr = attributes[i];
						if (attr != null && mode == InterceptorMode.None)
							CreateAndAddInterceptorInfo(localInterc, attr);
						else if (attr != null && attr.Mode == mode)
							CreateAndAddInterceptorInfo(localInterc, attr);
					}
					_methodInterceptors.Value.TryAdd(mi, localInterc.ToList());
				}
				else
				{
					IList<InterceptorInfo> methInterc;
					if (_methodInterceptors.Value.TryGetValue(mi, out methInterc))
					{
						localInterc = new SafeCollection<InterceptorInfo>();
						localInterc.AddRange(methInterc);
						if (mode != InterceptorMode.None && localInterc.Where(x => x.Mode == mode).FirstOrDefault() == null)
						{
							var methodLevelAtt = attributes.Where(x => x.Mode == mode).ToList();
							if (methodLevelAtt.Count > 0)
							{
								foreach (InterceptorAttribute attr in methodLevelAtt)
								{
									CreateAndAddInterceptorInfo(localInterc, attr);
								}
								_methodInterceptors.Value.TryAdd(mi, localInterc.ToList());
							}
							else localInterc.Clear();
						}
					}
				}
			}
			return localInterc ?? new SafeCollection<InterceptorInfo>();
		}

		protected void CreateAndAddInterceptorInfo(SafeCollection<InterceptorInfo> interceptors, InterceptorAttribute attr)
		{
			InterceptorInfo info = new InterceptorInfo(attr.InterceptorType, attr.Mode, attr.EatException);
			interceptors.Add(info);
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
						if ((fProp = TypePropertyMapper.Instance.Get(_typeHash, binder.Name)) == null)
						{
							PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
							if (pInfo != null)
							{
								TypePropertyMapper.Instance.Add(_typeHash, pInfo, out fProp);
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
			value.ThrowIfNull("value", "Value cannot be null");
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
					if ((fProp = TypePropertyMapper.Instance.Get(_typeHash, binder.Name)) == null)
					{
						PropertyInfo pInfo = _targerType.GetProperty(binder.Name);
						if (pInfo != null)
						{
							TypePropertyMapper.Instance.Add(_typeHash, pInfo, out fProp);
							fProp.Set(_weakTarget == null ? _target : _weakTarget.Target, value);
							return true;
						}
						else throw new InvalidOperationException("Fail to retrieve property " + binder.Name + " in the Type:" + _targerType.FullName);
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