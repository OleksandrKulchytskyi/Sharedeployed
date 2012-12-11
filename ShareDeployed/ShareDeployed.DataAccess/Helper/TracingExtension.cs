using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Helper
{
	public static class TracingExtension
	{
		private const string debugSeperator = "-------------------------------------------------------------------------------";

		public static IQueryable<T> TraceQuery<T>(this IQueryable<T> query)
		{
			if (query != null)
			{
				ObjectQuery<T> objectQuery = query as ObjectQuery<T>;
				if (objectQuery != null)// && Boolean.Parse(ConfigurationManager.AppSettings["Debugging"]))
				{
					StringBuilder queryString = new StringBuilder();
					queryString.Append(Environment.NewLine)
						.AppendLine(debugSeperator)
						.AppendLine("QUERY GENERATED...")
						.AppendLine(debugSeperator)
						.AppendLine(objectQuery.ToTraceString())
						.AppendLine(debugSeperator)
						.AppendLine(debugSeperator)
						.AppendLine("PARAMETERS...")
						.AppendLine(debugSeperator);
					foreach (ObjectParameter parameter in objectQuery.Parameters)
					{
						queryString.Append(String.Format("{0}({1}) \t- {2}", parameter.Name, parameter.ParameterType, parameter.Value)).Append(Environment.NewLine);
					}
					queryString.AppendLine(debugSeperator).Append(Environment.NewLine);
					Console.WriteLine(queryString);
					Trace.WriteLine(queryString);
				}
			}
			return query;
		}
	}
}
