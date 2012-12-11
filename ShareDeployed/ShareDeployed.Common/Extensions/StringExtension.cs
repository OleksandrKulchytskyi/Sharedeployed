using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class StringExtensions
	{
		public static string ToMD5(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return null;
			}

			string result = string.Empty;
			using (MD5 md5 = MD5.Create())
			{
				result = String.Join("", md5.ComputeHash(Encoding.Default.GetBytes(value))
							 .Select(b => b.ToString("x2")));
			}
			return result;
		}

		public static string ToSha256(this string value, string salt)
		{
			string saltedValue = ((salt ?? "") + value);

			string result = string.Empty;
			using (SHA256 sha256 = SHA256.Create())
			{
				result = String.Join("", sha256.ComputeHash(Encoding.Default.GetBytes(value))
							 .Select(b => b.ToString("x2")));
			}
			return result;
		}

		public static string FixUserName(this string username)
		{
			// simple for now, translate spaces to underscores
			return username.Replace(' ', '_');
		}
	}
}
