using System;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	internal static class StringUtils
	{
		public const string CarriageReturnLineFeed = "\r\n";
		public const string Empty = "";
		public const char CarriageReturn = '\r';
		public const char LineFeed = '\n';
		public const char Tab = '\t';

		public static string FormatWith(this string format, IFormatProvider provider, object arg0)
		{
			return format.FormatWith(provider, new[] { arg0 });
		}

		public static string FormatWith(this string format, IFormatProvider provider, object arg0, object arg1)
		{
			return format.FormatWith(provider, new[] { arg0, arg1 });
		}

		public static string FormatWith(this string format, IFormatProvider provider, object arg0, object arg1, object arg2)
		{
			return format.FormatWith(provider, new[] { arg0, arg1, arg2 });
		}

		public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
		{
			return string.Format(provider, format, args);
		}

		/// <summary>
		/// Determines whether the string is all white space. Empty string will return false.
		/// </summary>
		/// <param name="s">The string to test whether it is all white space.</param>
		/// <returns>
		/// 	<c>true</c> if the string is all white space; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsWhiteSpace(string s)
		{
			if (s == null)
				throw new ArgumentNullException("s");

			if (s.Length == 0)
				return false;

			for (int i = 0; i < s.Length; i++)
			{
				if (!char.IsWhiteSpace(s[i]))
					return false;
			}

			return true;
		}

		public static string ToCamelCase(string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;

			if (!char.IsUpper(s[0]))
				return s;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				bool hasNext = (i + 1 < s.Length);
				if ((i == 0 || !hasNext) || char.IsUpper(s[i + 1]))
				{
					char lowerCase;
					lowerCase = char.ToLowerInvariant(s[i]);
					sb.Append(lowerCase);
				}
				else
				{
					sb.Append(s.Substring(i));
					break;
				}
			}

			return sb.ToString();
		}
	}
}
