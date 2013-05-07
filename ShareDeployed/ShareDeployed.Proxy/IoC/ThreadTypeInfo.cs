using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public struct ThreadTypeInfo
	{
		public ThreadTypeInfo(int threadId, Type contract)
		{
			_threadId = threadId;
			_contract = contract;
		}

		private int _threadId;
		public int ThreadId
		{
			get { return _threadId; }
			set { _threadId = value; }
		}

		private Type _contract;
		public Type Contract
		{
			get { return _contract; }
			set { _contract = value; }
		}

		public override int GetHashCode()
		{
			return _contract.GetHashCode() ^ _threadId.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (obj is ThreadTypeInfo)
			{
				ThreadTypeInfo compare = (ThreadTypeInfo)obj;
				return (compare.Contract.Equals(this.Contract) && ThreadId.Equals(compare.ThreadId));
			}
			return false;
		}
	}

	public class ThreadTypeInfoEqualityComparer : IEqualityComparer<ThreadTypeInfo>
	{

		public bool Equals(ThreadTypeInfo x, ThreadTypeInfo y)
		{
			return x.Equals(y);
		}

		public int GetHashCode(ThreadTypeInfo obj)
		{
			return obj.GetHashCode();
		}
	}
}
