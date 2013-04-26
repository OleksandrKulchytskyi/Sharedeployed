﻿using ShareDeployed.Common.Proxy.Caching;
using System;
using System.Threading;

namespace ShareDeployed.Common.Proxy.Logging
{
	public class LogAggregator : ILoggerAggregator
	{
		private Cache<string, ILoggerProvider> _cache;

		public LogAggregator()
		{
			_cache = new Cache<string, ILoggerProvider>();
		}

		public void AddLogger(ILoggerProvider provider, bool isWeak = false)
		{
			string name = provider.GetType().FullName;
			if (!_cache.Contains(name))
			{
				_cache.Insert(name, provider, isWeak ? CacheStrategy.Temporary : CacheStrategy.Permanent);
				Interlocked.Increment(ref _count);
			}
		}

		public void AddLogger(string alias, ILoggerProvider provider, bool isWeak = false)
		{
			if (!_cache.Contains(alias))
			{
				_cache.Insert(alias, provider, isWeak ? CacheStrategy.Temporary : CacheStrategy.Permanent);
				Interlocked.Increment(ref _count);
			}
		}

		public void RemoveLogger(ILoggerProvider provider)
		{
			string name = provider.GetType().FullName;
			if (_cache.Contains(name))
			{
				_cache.Remove(name);
				Interlocked.Decrement(ref _count);
			}
		}

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
		public int Count
		{
			get { return _count; }
		}

		public void DoLog(LogSeverity severity, string msg, Exception exc)
		{
			switch (severity)
			{
				case LogSeverity.Info:
					foreach (var logger in _cache.Values)
					{
						ILoggerProvider provider = GetCasted(logger);
						provider.Info(msg);
					}
					break;
				case LogSeverity.Warn:
					foreach (var logger in _cache.Values)
					{
						ILoggerProvider provider = GetCasted(logger);
						provider.Warn(msg, exc);
					}
					break;
				case LogSeverity.Error:
					foreach (var logger in _cache.Values)
					{
						ILoggerProvider provider = GetCasted(logger);
						provider.Error(msg, exc);
					}
					break;
				case LogSeverity.Fatal:
					foreach (var logger in _cache.Values)
					{
						ILoggerProvider provider = GetCasted(logger);
						provider.Fatal(msg, exc);
					}
					break;
			}
		}

		private ILoggerProvider GetCasted(object provider)
		{
			return (provider is ILoggerProvider) ? (ILoggerProvider)provider : ((ILoggerProvider)((provider as WeakReference).Target));
		}
	}
}
