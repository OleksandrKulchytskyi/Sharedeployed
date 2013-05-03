using ShareDeployed.Common.Proxy.Caching;
using System;
using System.Threading;

namespace ShareDeployed.Common.Proxy.Logging
{
	/// <summary>
	/// Log aggregator
	/// </summary>
	public class LogAggregator : ILogAggregator
	{
		private Cache<string, ILogProvider> _cache;

		public LogAggregator()
		{
			_cache = new Cache<string, ILogProvider>();
		}

		/// <summary>
		/// Adds logger to the system
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="isWeak">Indicates wether object needs to be wrapper in WeakReference</param>
		public void AddLogger(ILogProvider provider, bool isWeak = false)
		{
			string name = provider.GetType().FullName;
			if (!_cache.Contains(name))
			{
				_cache.Insert(name, provider, isWeak ? CacheStrategy.Temporary : CacheStrategy.Permanent);
				Interlocked.Increment(ref _count);
			}
		}

		/// <summary>
		/// Adds loger with alias to the system
		/// </summary>
		/// <param name="alias">Logger alias</param>
		/// <param name="provider">Log provider</param>
		/// <param name="isWeak">Needs to be wrapped in GenericWeakRef</param>
		public void AddLogger(string alias, ILogProvider provider, bool isWeak = false)
		{
			if (!_cache.Contains(alias))
			{
				_cache.Insert(alias, provider, isWeak ? CacheStrategy.Temporary : CacheStrategy.Permanent);
				Interlocked.Increment(ref _count);
			}
		}

		/// <summary>
		/// Removes logger from system
		/// </summary>
		/// <param name="provider"></param>
		public void RemoveLogger(ILogProvider provider)
		{
			string name = provider.GetType().FullName;
			if (_cache.Contains(name))
			{
				_cache.Remove(name);
				Interlocked.Decrement(ref _count);
			}
		}

		/// <summary>
		/// Removes logger from system by it alias name
		/// </summary>
		/// <param name="alias"></param>
		public void RemoveLogger(string alias)
		{
			if (_cache.Contains(alias))
			{
				_cache.Remove(alias);
				Interlocked.Decrement(ref _count);
			}
		}

		public void Clear()
		{
			if (Interlocked.CompareExchange(ref _count, 0, 0) != 0)
				_cache.Clear();
		}

		int _count;
		/// <summary>
		/// Loggers count
		/// </summary>
		public int Count
		{
			get { return _count; }
		}

		/// <summary>
		/// Performs log operation in all registered loggers
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="msg"></param>
		/// <param name="exc"></param>
		public void DoLog(LogSeverity severity, string msg, Exception exc)
		{
			switch (severity)
			{
				case LogSeverity.Info:
					foreach (var logger in _cache.Values)
					{
						ILogProvider provider = GetCasted(logger);
						provider.Info(msg);
					}
					break;
				case LogSeverity.Warn:
					foreach (var logger in _cache.Values)
					{
						ILogProvider provider = GetCasted(logger);
						provider.Warn(msg, exc);
					}
					break;
				case LogSeverity.Error:
					foreach (var logger in _cache.Values)
					{
						ILogProvider provider = GetCasted(logger);
						provider.Error(msg, exc);
					}
					break;
				case LogSeverity.Fatal:
					foreach (var logger in _cache.Values)
					{
						ILogProvider provider = GetCasted(logger);
						provider.Fatal(msg, exc);
					}
					break;
			}
		}

		private ILogProvider GetCasted(object provider)
		{
			return (provider is ILogProvider) ? 
				(ILogProvider)provider : ((ILogProvider)((provider as WeakReference).Target));
		}
	}
}
