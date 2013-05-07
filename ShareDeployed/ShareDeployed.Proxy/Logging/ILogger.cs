using System;

namespace ShareDeployed.Proxy.Logging
{
	/// <summary>
	/// Log severity enumeration
	/// </summary>
	public enum LogSeverity
	{
		Info = 0,
		Warn,
		Error,
		Fatal
	}

	public interface ILogProvider
	{
		void Info(string message);
		void Warn(string message, Exception Exception);
		void Error(string message, Exception Exception);
		void Fatal(string message, Exception Exception);
	}

	public interface ILogAggregator
	{
		int Count { get; }
		void AddLogger(ILogProvider provider, bool isWeak = false);
		void RemoveLogger(ILogProvider provider);
		void Clear();

		void DoLog(LogSeverity severity, string msg, Exception exc);
	}
}
