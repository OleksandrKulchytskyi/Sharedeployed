using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace ShareDeployed.Extension
{
	public class CacheManager
	{
		private static System.Web.Caching.Cache _CurrentCache;

		/// <summary>
		/// Return an instance of Cache object that can be accessed even by asynchronous code
		/// </summary>
		public static System.Web.Caching.Cache CurrentCache
		{
			get
			{
				if (_CurrentCache == null)
				{
					_CurrentCache = System.Web.HttpContext.Current.Cache;
				}
				return _CurrentCache;
			}
		}
	}

	public abstract class BaseCachedMethod<T>
	{
		/// <summary>
		/// cache expiration in seconds
		/// </summary>
		protected int _Expiration = 60;

		/// <summary>
		/// Cache priority
		/// </summary>
		protected System.Web.Caching.CacheItemPriority _Priority =
					   System.Web.Caching.CacheItemPriority.Normal;

		/// <summary>
		/// If true means that the cache will be automatically refreshed after 
		/// expiration
		/// </summary>
		protected bool _DoCallBack = true;

		/// <summary>
		/// If true the object is saved in cache, otherwise it's always
		/// retrieved from data source
		/// </summary>
		protected bool _UseCache = true;

		/// <summary>
		/// This property builds the cache key by using the reflected name of 
		/// the class and the GetCacheKey method implemented in the concrete 
		/// class
		/// </summary>
		private string CacheKey
		{
			get
			{
				return this.GetType().ToString() +
					   "-" +
					   this.GetCacheKey();
			}
		}

		/// <summary>
		/// Adds data do cache
		/// </summary>
		/// <param name=""localResult"" />
		private void AddDataToCache(T localResult)
		{
			if (_DoCallBack)
			{
				CacheManager.CurrentCache.Insert(CacheKey,
												 localResult,
												 null,
												 DateTime.UtcNow.AddSeconds(_Expiration),
												 System.Web.Caching.Cache.NoSlidingExpiration,
												 _Priority,
												 new CacheItemRemovedCallback(LoadCache));
			}
			else
			{
				CacheManager.CurrentCache.Insert(CacheKey,
												 localResult,
												 null,
												 DateTime.UtcNow.AddSeconds(_Expiration),
												 System.Web.Caching.Cache.NoSlidingExpiration,
												 _Priority,
												 null);
			}
		}

		/// <summary>
		/// This abstract method has to be redefined in the concrete class in 
		/// order to define a unique cache key
		/// </summary>
		/// <returns>
		protected abstract string GetCacheKey();

		/// <summary>
		/// This abstract method has to be implemented in the concrete class 
		/// and wiil contain the code that performs the query
		/// </summary>
		/// <returns>
		protected abstract T LoadData();

		/// <summary>
		/// This method calls the LoadData method and is passed to the 
		/// Cache.Insert method as a callback
		/// </summary>
		/// <param name=""cacheKey"" />
		/// <param name=""obj"" />
		/// <param name=""reason"" />
		private void LoadCache(string cacheKey,
							   object obj,
							   System.Web.Caching.CacheItemRemovedReason reason)
		{
			//If an object has been explicitly removed or is epired due to 
			//underusage, it is not added to cache.
			if (reason != System.Web.Caching.CacheItemRemovedReason.Removed &&
				reason != System.Web.Caching.CacheItemRemovedReason.Underused)
			{
				if (obj != null)
				{
					//Expired object is immediately added again to cache so the 
					//user doesn't have to wait till the end of the query
					CacheManager.CurrentCache.Insert(cacheKey, obj);
				}
				T localResult = LoadData();
				AddDataToCache(localResult);
			}
		}

		/// <summary>
		/// Gets the method data from data source or cache
		/// </summary>
		/// <returns>
		public T GetData()
		{
			T result = default(T);
			if (_UseCache)
			{
				object objInCache = CacheManager.CurrentCache.Get(CacheKey);
				if (objInCache == null)
				{
					result = LoadData();
					AddDataToCache(result);
				}
				else
				{
					result = (T)objInCache;
				}
			}
			else
			{
				result = LoadData();
			}
			return result;
		}
	}
}