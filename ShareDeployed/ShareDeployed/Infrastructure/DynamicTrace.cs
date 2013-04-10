using NLog;
using ShareDeployed.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http.Tracing;

namespace ShareDeployed.Infrastructure
{
	public class DynamicTrace : Hubs.SignalRBase<Hubs.TraceHub>, ITraceWriter
	{
		private readonly Lazy<Dictionary<TraceLevel, Action<string>>> loggingMap = new
			Lazy<Dictionary<TraceLevel, Action<string>>>(() => new Dictionary<TraceLevel, Action<string>>
			{
				{ TraceLevel.Info, LogManager.GetCurrentClassLogger().Info },
				{ TraceLevel.Debug, LogManager.GetCurrentClassLogger().Debug },
				{ TraceLevel.Error, LogManager.GetCurrentClassLogger().Error },
				{ TraceLevel.Fatal, LogManager.GetCurrentClassLogger().Fatal },
				{ TraceLevel.Warn, LogManager.GetCurrentClassLogger().Warn }
			});

		private Dictionary<TraceLevel, Action<string>> _nlogLogger
		{
			get
			{
				return loggingMap.Value;
			}
		}

		private readonly Lazy<string[]> logLevelsLazy = new
			Lazy<string[]>(() => System.Configuration.ConfigurationManager.AppSettings["logLevels"].Split(','));

		private string[] _logLevels
		{
			get
			{
				return logLevelsLazy.Value;
			}
		}

		public bool IsEnabled(string category, TraceLevel level)
		{
			return true; //obsolete
		}

		[System.Diagnostics.DebuggerStepThrough()]
		public void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
		{
			if (level != TraceLevel.Off && _logLevels.Contains(level.ToString(), StringComparer.OrdinalIgnoreCase))
			{
				TraceRecord record = new TraceRecord(request, category, level);
				traceAction(record);
				LogToHub(record);
				LogToNLog(record);
			}
		}

		private void LogToHub(TraceRecord record)
		{
			var msgBuilder = new StringBuilder();
			msgBuilder.AppendMessage(record.Level.ToString().ToUpper());
			msgBuilder.AppendMessage(DateTime.UtcNow.ToString("dd-MM-yyyy hh:mm"));

			if (record.Request != null)
				msgBuilder.AppendMessage(record.Request.Method.ToString(), Common.Extensions.Common.notEmpty).
					AppendMessage(record.Request.RequestUri.ToString(), Common.Extensions.Common.notEmpty).
					AppendMessage(record.Request.Content != null ?
								record.Request.Content.ToString() : string.Empty, Common.Extensions.Common.notEmpty);

			msgBuilder.AppendMessage(record.Category, Common.Extensions.Common.notEmpty).
					AppendMessage(record.Operator, Common.Extensions.Common.notEmpty).
					AppendMessage(record.Operation).
					AppendMessage(record.Message, Common.Extensions.Common.notEmpty);

			if (record.Exception != null && Common.Extensions.Common.notEmpty(record.Exception.Message))
				msgBuilder.AppendLine("MessageData").AppendMessage(record.Exception.Message);

			if (HubInstance != null)
				HubInstance.Clients.All.logTraceMsg(msgBuilder.ToString()).Wait();

			msgBuilder.Clear();
			msgBuilder = null;
		}

		private void LogToNLog(TraceRecord record)
		{
			StringBuilder sb = new StringBuilder();

			if (record.Request != null)
			{
				if (record.Request.Method != null)
					sb.AppendMessage(" ").AppendMessage(record.Request.Method.ToString());

				if (record.Request.RequestUri != null)
					sb.AppendMessage(" ").AppendMessage(record.Request.RequestUri.ToString());
			}

			if (!string.IsNullOrWhiteSpace(record.Category))
				sb.AppendMessage(" ").AppendMessage(record.Category);

			if (!string.IsNullOrWhiteSpace(record.Operator))
				sb.AppendMessage(" ").AppendMessage(record.Operator).AppendMessage(" ").AppendMessage(record.Operation);

			if (!string.IsNullOrWhiteSpace(record.Message))
				sb.AppendMessage(" ").AppendMessage(record.Message);

			if (record.Exception != null)
			{
				if (record.Exception.GetBaseException().Message != null)
					sb.AppendMessage(record.Exception.GetBaseException().Message);
			}

			_nlogLogger[record.Level](sb.ToString());
			sb.Clear(); sb = null;
		}
	}
}