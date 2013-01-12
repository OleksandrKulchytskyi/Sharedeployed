using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public class ExpandObject : DynamicObject
	{
		private readonly Dictionary<string, object> _members = new Dictionary<string, object>();

		/// <summary>
		/// When a new property is set, 
		/// add the property name and value to the dictionary
		/// </summary>     
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			if (!_members.ContainsKey(binder.Name))
				_members.Add(binder.Name, value);
			else
				_members[binder.Name] = value;

			return true;
		}

		/// <summary>
		/// When user accesses something, return the value if we have it
		/// </summary>      
		public override bool TryGetMember
			   (GetMemberBinder binder, out object result)
		{
			if (_members.ContainsKey(binder.Name))
			{
				result = _members[binder.Name];
				return true;
			}
			else
				return base.TryGetMember(binder, out result);
		}

		/// <summary>
		/// If a property value is a delegate, invoke it
		/// </summary>     
		public override bool TryInvokeMember
		   (InvokeMemberBinder binder, object[] args, out object result)
		{
			if (_members.ContainsKey(binder.Name)
					  && _members[binder.Name] is Delegate)
			{
				result = (_members[binder.Name] as Delegate).DynamicInvoke(args);
				return true;
			}
			else
				return base.TryInvokeMember(binder, args, out result);
		}


		/// <summary>
		/// Return all dynamic member names
		/// </summary>
		/// <returns>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _members.Keys;
		}
	}
}
