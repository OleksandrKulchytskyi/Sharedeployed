using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public struct ThreadTypeInfo : IEquatable<ThreadTypeInfo>
	{
		private int _hash;
		public ThreadTypeInfo(int threadId, int contractId)
		{
			_hash = 0;
			_threadId = threadId;
			_contractId = contractId;
		}

		private int _threadId;
		public int ThreadId
		{
			get { return _threadId; }
			set { _threadId = value; }
		}

		private int _contractId;
		public int ContractId
		{
			get { return _contractId; }
			set { _contractId = value; }
		}

		public override int GetHashCode()
		{
			if (_hash != 0) return _hash;

			_hash = 17;
			_hash = _hash * 31 + _contractId.GetHashCode();
			_hash = _hash * 31 + _threadId.GetHashCode();
			return _hash;
		}

		public override bool Equals(object obj)
		{
			return (obj is ThreadTypeInfo) ? Equals((ThreadTypeInfo)obj) : false;
		}

		public bool Equals(ThreadTypeInfo compare)
		{
			return (compare.ContractId.Equals(this.ContractId) && ThreadId.Equals(compare.ThreadId));
		}

		public static bool operator ==(ThreadTypeInfo left, ThreadTypeInfo right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ThreadTypeInfo left, ThreadTypeInfo right)
		{
			return !(left == right);
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
