using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Proxy.Logging
{
	public class Log4netProvider : ILoggerProvider
	{
		private ILog _logger = null;

		public Log4netProvider()
		{
			log4net.Config.XmlConfigurator.Configure();
			_logger = log4net.LogManager.GetLogger(typeof(Log4netProvider));
		}

		public Log4netProvider(ILog logger)
		{
			logger.ThrowIfNull("logger", "Parameter cannot be null.");
			_logger = logger;
		}

		public void Info(string message)
		{
			_logger.Info(message);
		}

		public void Warn(string message, Exception ex)
		{
			_logger.Warn(message, ex);
		}

		public void Error(string message, Exception ex)
		{
			_logger.Error(message, ex);
		}

		public void Fatal(string message, Exception ex)
		{
			_logger.Fatal(message, ex);
		}
	}
}
