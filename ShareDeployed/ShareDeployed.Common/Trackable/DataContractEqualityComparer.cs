using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Trackable
{
	public class DataContractEqualityComparer<T> : IEqualityComparer<T>
	{
		public bool Equals(T x, T y)
		{
			return SerializationUtil.IsEqual<T>(x, y);
		}

		public int GetHashCode(T obj)
		{
			string hash = SerializationUtil.GetHash<T>(obj);
			return hash.GetHashCode();
		}
	}
}
