using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Security
{
	public class Secure
	{
		private const string minSalt = "&B6yhj$,";

		/// <summary>
		/// Create RijndaelManaged object
		/// </summary>
		/// <param name="keySeed"></param>
		/// <param name="saltString"></param>
		/// <returns></returns>
		private static RijndaelManaged Cryptor(string keySeed, string saltString)
		{
			byte[] salt = UTF8Encoding.UTF8.GetBytes(saltString + minSalt);
			using (Rfc2898DeriveBytes derivedBytes = new Rfc2898DeriveBytes(keySeed, salt, 1000))
			{
				RijndaelManaged cryptor = new RijndaelManaged();
				// KeySize must be set before the Key
				cryptor.KeySize = 128;
				cryptor.Key = derivedBytes.GetBytes(16);
				cryptor.IV = derivedBytes.GetBytes(16);
				return cryptor;
			}
		}

		/// <summary>
		/// Encrypt data
		/// </summary>
		/// <param name="clearText"></param>
		/// <param name="keySeed"></param>
		/// <param name="salt"></param>
		/// <remarks>
		/// <example>
		/// cookie.Value = Secure.EncryptToBase64("my secret text","password", this.Request.UserHostAddress);
		/// </example>
		/// </remarks>
		/// <returns></returns>
		public static string EncryptToBase64(string clearText, string keySeed, string salt)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (ICryptoTransform encryptor = Cryptor(keySeed, salt).CreateEncryptor())
				{
					using (CryptoStream encrypt = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					{
						byte[] data = new UTF8Encoding(false).GetBytes(clearText);
						encrypt.Write(data, 0, data.Length); encrypt.Close();
						return Convert.ToBase64String(ms.ToArray());
					}
				}
			}
		}

		/// <summary>
		/// Decrypt Data
		/// </summary>
		/// <param name="cipherText"></param>
		/// <param name="keySeed"></param>
		/// <param name="salt"></param>
		/// <remarks>
		/// <example>
		/// string secret = Secure.DecryptFromBase64(cookie.Value,"password", this.Request.UserHostAddress); 
		/// </example>
		/// </remarks>
		/// <returns></returns>
		public static string DecryptFromBase64(string cipherText, string keySeed, string salt)
		{
			byte[] data = Convert.FromBase64String(cipherText);
			using (MemoryStream ms = new MemoryStream())
			{
				using (ICryptoTransform decryptor = Cryptor(keySeed, salt).CreateDecryptor())
				{
					using (CryptoStream decrypt = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
					{
						decrypt.Write(data, 0, data.Length);
						decrypt.FlushFinalBlock();
						return new UTF8Encoding(false).GetString(ms.ToArray());
					}
				}
			}
		}
	}
}