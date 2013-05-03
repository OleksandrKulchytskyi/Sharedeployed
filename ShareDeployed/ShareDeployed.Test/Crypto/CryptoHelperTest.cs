using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using ShareDeployed.Infrastructure;
using System.Text;

namespace ShareDeployed.Test
{
	public class CryptoHelperFacts
	{
		[TestMethod]
		public void ProtectCanUnProtect()
		{
			byte[] encryptionKey = GenerateRandomBytes();
			byte[] validationKey = GenerateRandomBytes();

			using (var algo = new AesCryptoServiceProvider())
			{
				byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
				byte[] payload = CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);

				byte[] buffer = CryptoHelper.Unprotect(encryptionKey, validationKey, payload);

				Assert.AreEqual("Hello World", Encoding.UTF8.GetString(buffer));
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void WrongValidationKeyWontDecrypt()
		{
			byte[] encryptionKey = GenerateRandomBytes();
			byte[] validationKey = GenerateRandomBytes();

			using (var algo = new AesCryptoServiceProvider())
			{
				byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
				byte[] payload = CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);
				CryptoHelper.Unprotect(encryptionKey, GenerateRandomBytes(), payload);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void WrongPayloadDecrypt()
		{
			byte[] encryptionKey = GenerateRandomBytes();
			byte[] validationKey = GenerateRandomBytes();

			using (var algo = new AesCryptoServiceProvider())
			{
				byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
				CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);
				CryptoHelper.Unprotect(encryptionKey, validationKey, GenerateRandomBytes());
			}
		}

		[TestMethod]
		public void ToHex()
		{
			var buffer = new byte[] { 1, 2, 3, 4, 25, 15 };

			var bitConv = BitConverter.ToString(buffer).Replace("-", string.Empty);
			var hex = CryptoHelper.ToHex(buffer);
			Assert.AreEqual(bitConv, hex);
			Assert.AreEqual("01020304190F", hex);
		}

		[TestMethod]
		public void FromHex()
		{
			string value = "01020304190F";
			var buffer = new byte[] { 1, 2, 3, 4, 25, 15 };

			byte[] resut = CryptoHelper.FromHex(value);

			Assert.AreEqual(buffer, resut);
		}

		[TestMethod]
		public void ToAndFromHex()
		{
			for (int i = 0; i < 20; i++)
			{
				var buffer = GenerateRandomBytes();
				string hex = CryptoHelper.ToHex(buffer);
				var bitConverter = BitConverter.ToString(buffer).Replace("-", string.Empty);
				Assert.AreEqual(bitConverter, hex);

				var fromBytes = CryptoHelper.FromHex(hex);
				Assert.AreEqual(buffer, fromBytes);
			}
		}

		private byte[] GenerateRandomBytes(int n = 32)
		{
			using (var cryptoProvider = new RNGCryptoServiceProvider())
			{
				var bytes = new byte[n];
				cryptoProvider.GetBytes(bytes);
				return bytes;
			}
		}
	}
}
