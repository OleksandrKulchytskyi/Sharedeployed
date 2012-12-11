using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Caching
{
	public interface ICache
	{
		object Get(string key);
		void Set(string key, object value, TimeSpan expiresIn);
		void Remove(string key);
	}
}
