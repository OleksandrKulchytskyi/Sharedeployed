using System;
//using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class StringExtensions
	{
		//bool IsKeyword(String tokenText, HashSet<string> keywords)
		//{
		//	int hashCode = 0;

		//	for (int i = 0; i < tokenText.Length; i++)
		//	{
		//		int c = (int)tokenText[i];

		//		// check upper bound
		//		if (c > 'z')
		//			return false;

		//		// to upper case for Latin letters
		//		if (c >= 'a')
		//			c ^= 0x20;

		//		// a keyword must be of Latin letters, numbers, and underscore
		//		if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
		//			return false;

		//		// update hash code
		//		hashCode = hashCode + c;
		//	}

		//	return keywords.Contains(hashCode);
		//}

		public static string ToMD5(this string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				return null;
			}

			string result = string.Empty;
			using (MD5 md5 = MD5.Create())
			{
				result = string.Join("", md5.ComputeHash(Encoding.Default.GetBytes(value))
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
				result = string.Join("", sha256.ComputeHash(Encoding.Default.GetBytes(value))
							 .Select(b => b.ToString("x2")));
			}
			return result;
		}

		public static string CalculateSHA256Hash(this string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentException("input");
			// Encode the input string into a byte array.
			byte[] inputBytes = Encoding.UTF8.GetBytes(input);
			// Create an instance of the SHA256 algorithm class and use it to calculate the hash.
			SHA256Managed sha256 = new SHA256Managed();
			byte[] outputBytes = sha256.ComputeHash(inputBytes);
			// Convert the outputed hash to a string and return it.
			return Convert.ToBase64String(outputBytes);
		}

		public static string FixUserName(this string username)
		{
			// simple for now, translate spaces to underscores
			return username.Replace(' ', '_');
		}
	}
}