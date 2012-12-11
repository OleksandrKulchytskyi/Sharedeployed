using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class FileHelper
	{
		///<summary>
		/// Encrypts a file using Rijndael algorithm.
		///</summary>
		///<param name="inputFile"></param>
		///<param name="outputFile"></param>
		public static void EncryptToFile(this string data, string outputFile, string password)
		{
			try
			{
				byte[] key = Encoding.UTF8.GetBytes(password);

				string cryptFile = outputFile;
				using (FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create))
				{
					using (RijndaelManaged RMCrypto = new RijndaelManaged())
					{
						CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateEncryptor(key, key), CryptoStreamMode.Write);
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data)))
						{
							int datavar;
							while ((datavar = ms.ReadByte()) != -1)
								cs.WriteByte((byte)datavar);

						}
					}
				}
			}
			catch
			{
			}
		}

		///<summary>
		/// Decrypts a file using Rijndael algorithm.
		///</summary>
		///<param name="inputFile"></param>
		///<param name="outputFile"></param>
		public static string DecryptFromFile(string inputFile, string password)
		{
			byte[] key = Encoding.UTF8.GetBytes(password);
			string decoded = string.Empty;
			using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
			{
				using (RijndaelManaged RMCrypto = new RijndaelManaged())
				{
					CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateDecryptor(key, key), CryptoStreamMode.Read);
					using (MemoryStream ms = new MemoryStream())
					{
						int data;
						while ((data = cs.ReadByte()) != -1)
							ms.WriteByte((byte)data);

						if (ms.CanSeek)
							ms.Seek(0, SeekOrigin.Begin);

						decoded = Encoding.UTF8.GetString(ms.ToArray());
					}
				}
			}
			return decoded;
		}
	}
}