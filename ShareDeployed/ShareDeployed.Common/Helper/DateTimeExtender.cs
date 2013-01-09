using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Helper
{
	public static class DateTimeExtender
	{
		private static readonly DateTime UnixEpoch =new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long GetCurrentUnixTimestampMillis()
		{
			return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
		}

		public static DateTime DateTimeFromUnixTimestampMillis(long millis)
		{
			return UnixEpoch.AddMilliseconds(millis);
		}

		public static long GetCurrentUnixTimestampSeconds()
		{
			return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
		}

		public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
		{
			return UnixEpoch.AddSeconds(seconds);
		}

		#region Functions
		/// <summary>
		/// Methods to convert DateTime to Unix time stamp
		/// </summary>
		/// <param name="_UnixTimeStamp">Unix time stamp to convert</param>
		/// <returns>Return Unix time stamp as long type</returns>
		public static long ToUnixTimestamp(this DateTime thisDateTime)
		{
			TimeSpan _UnixTimeSpan = (thisDateTime - new DateTime(1970, 1, 1, 0, 0, 0));
			return (long)_UnixTimeSpan.TotalSeconds;
		}

		#endregion
	}
}
