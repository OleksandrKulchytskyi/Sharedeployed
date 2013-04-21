using System;
using System.Collections.Generic;
using System.Dynamic;

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

	/// <summary>
	/// Extension methods for working with types.
	/// </summary>
	public static class ValueTypeExtensions
	{
		///<summary>
		/// Returns a wrapper <see cref="ValueTypeHolder"/> instance if <paramref name="obj"/> 
		/// is a value type.  Otherwise, returns <paramref name="obj"/>.
		///</summary>
		///<param name="obj">An object to be examined.</param>
		///<returns>A wrapper <seealso cref="ValueTypeHolder"/> instance if <paramref name="obj"/>
		/// is a value type, or <paramref name="obj"/> itself if it's a reference type.</returns>
		public static object WrapIfValueType(this object obj)
		{
			return obj.GetType().IsValueType ? new ValueTypeHolder(obj) : obj;
		}

		///<summary>
		/// Returns a wrapped object if <paramref name="obj"/> is an instance of <see cref="ValueTypeHolder"/>.
		///</summary>
		///<param name="obj">An object to be "erased".</param>
		///<returns>The object wrapped by <paramref name="obj"/> if the latter is of type <see cref="ValueTypeHolder"/>.  Otherwise,
		/// return <paramref name="obj"/>.</returns>
		public static object UnwrapIfWrapped(this object obj)
		{
			var holder = obj as ValueTypeHolder;
			return holder == null ? obj : holder.Value;
		}

		/// <summary>
		/// Determines whether <paramref name="obj"/> is a wrapped object (instance of <see cref="ValueTypeHolder"/>).
		/// </summary>
		/// <param name="obj">The object to check.</param>
		/// <returns>Returns true if <paramref name="obj"/> is a wrapped object (instance of <see cref="ValueTypeHolder"/>).</returns>
		public static bool IsWrapped(this object obj)
		{
			return obj as ValueTypeHolder != null;
		}
	}
}
