using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy.Logging
{
	/// <summary>
	/// Log provider for log4net component
	/// </summary>
	public class Log4netProvider : ILogProvider
	{
		/// <summary>
		/// log4net component
		/// </summary>
		private ILog _logger = null;

		public Log4netProvider()
		{
			log4net.Config.XmlConfigurator.Configure();
			_logger = log4net.LogManager.GetLogger(typeof(Log4netProvider));
		}

		public Log4netProvider(ILog logger)
		{
			logger.ThrowIfNull("logger", "Parameter cannot be a null.");
			_logger = logger;
		}

		/// <summary>
		/// Write info message
		/// </summary>
		/// <param name="message"></param>
		public void Info(string message)
		{
			_logger.Info(message);
		}

		/// <summary>
		/// Write warning message 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="ex"></param>
		public void Warn(string message, Exception ex)
		{
			_logger.Warn(message, ex);
		}

		/// <summary>
		/// Write error message
		/// </summary>
		/// <param name="message"></param>
		/// <param name="ex"></param>
		public void Error(string message, Exception ex)
		{
			_logger.Error(message, ex);
		}

		/// <summary>
		/// Write fatal message
		/// </summary>
		/// <param name="message"></param>
		/// <param name="ex"></param>
		public void Fatal(string message, Exception ex)
		{
			_logger.Fatal(message, ex);
		}
	}
}
