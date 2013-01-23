using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.Adapters
{
	public interface ICacheStorage
	{
		void Remove(string key);
		void Store(string key, object data);
		T Retrieve<T>(string key);
	}

	public class HttpContextCacheAdapter : ICacheStorage
	{
		public void Remove(string key)
		{
			HttpContext.Current.Cache.Remove(key);
		}

		public void Store(string key, object data)
		{
			HttpContext.Current.Cache.Insert(key, data, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
		}

		public T Retrieve<T>(string key)
		{
			T itemStored = (T)HttpContext.Current.Cache.Get(key);
			if (itemStored == null)
				itemStored = default(T);
			return itemStored;
		}
	}
}