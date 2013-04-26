using System;

namespace ShareDeployed.Common.Proxy.Logging
{
	public enum LogSeverity
	{
		Info = 0,
		Warn,
		Error,
		Fatal
	}

	public interface ILoggerProvider
	{
		void Info(string message);
		void Warn(string message, Exception Exception);
		void Error(string message, Exception Exception);
		void Fatal(string message, Exception Exception);
	}

	public interface ILoggerAggregator
	{
		int Count { get; }
		void AddLogger(ILoggerProvider provider, bool isWeak = false);
		void RemoveLogger(ILoggerProvider provider);
		void Clear();

		void DoLog(LogSeverity severity, string msg, Exception exc);
	}
}
