using System;
using System.Collections.Generic;
using System.Dynamic;

namespace ShareDeployed.Common.Proxy
{
	public enum InterceptorInjectionMode
	{
		None = 0,
		Before,
		After,
		OnError
	}

	public interface IPipeline
	{
		void Initialize(bool withinDomain=false);

		Logging.ILogAggregator LoggerAggregator { get; }

		IContractResolver ContracResolver { get; }
	}

	public interface IContractResolver
	{
		object Resolve(Type contract);
		T Resolve<T>();
		IEnumerable<object> ResolveAll(params Type[] types);
	}

	public class InterceptorInfo
	{
		public InterceptorInfo(Type interceptorType, InterceptorInjectionMode mode) :
			this(interceptorType, mode, false)
		{
		}

		public InterceptorInfo(Type interceptorType, InterceptorInjectionMode mode, bool eatException)
		{
			Interceptor = interceptorType;
			Mode = mode;
			EatException = eatException;
		}

		public Type Interceptor { get; private set; }
		public InterceptorInjectionMode Mode { get; private set; }
		public bool EatException { get; private set; }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public sealed class InterceptorAttribute : Attribute
	{
		public Type InterceptorType { get; set; }
		public InterceptorInjectionMode Mode { get; set; }
		public bool EatException { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class GetInstanceAttribute : Attribute
	{
		public string Alias { get; set; }
		public Type TypeOf { get; set; }
	}

	/// <summary>
	/// Markable attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public sealed class InstantiateAttribute : Attribute
	{
		public InstantiateAttribute()
		{
		}
	}

	internal sealed class DynamicBuilder : DynamicObject
	{
		private readonly Dictionary<string, object> members = new Dictionary<string, object>();

		#region DynamicObject Overrides
		/// <summary>
		/// Assigns the given value to the specified member, overwriting any previous definition if one existed.
		/// </summary>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			members[binder.Name] = value;
			return true;
		}

		/// <summary>
		/// Gets the value of the specified member.
		/// </summary>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (members.ContainsKey(binder.Name))
			{
				result = members[binder.Name];
				return true;
			}
			return base.TryGetMember(binder, out result);
		}

		/// <summary>
		/// Invokes the specified member (if it is a delegate).
		/// </summary>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			object member;
			if (members.TryGetValue(binder.Name, out member))
			{
				var method = member as Delegate;
				if (method != null)
				{
					result = method.DynamicInvoke(args);
					return true;
				}
			}
			return base.TryInvokeMember(binder, args, out result);
		}

		/// <summary>
		/// Gets a list of all dynamically defined members.
		/// </summary>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return members.Keys;
		}
		#endregion
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
