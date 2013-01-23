using System;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Crypt
{
	public static class CryptExtensions
	{
		public static byte[] CreateSalt()
		{
			byte[] saltBytes;
			int minSaltSize = 8;
			int maxSaltSize = 9;

			// Generate a random number to determine the salt size.
			Random random = new Random();
			int saltSize = random.Next(minSaltSize, maxSaltSize);

			// Allocate a byte array, to hold the salt.
			saltBytes = new byte[saltSize];

			// Initialize the cryptographically secure random number generator.
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				// Fill the salt with cryptographically strong byte values.
				rng.GetNonZeroBytes(saltBytes);
			}

			return saltBytes;
		}

		public static byte[] HashPasswordBytes(string textToEncrypt, byte[] saltBytes)
		{
			// Convert the clear text into bytes.
			byte[] clearTextBytes = Encoding.UTF8.GetBytes(textToEncrypt);
			// Create a new array to hold clear text and salt.
			byte[] clearTextWithSaltBytes =
			new byte[clearTextBytes.Length + saltBytes.Length];
			// Copy clear text bytes into the new array.
			for (int i = 0; i < clearTextBytes.Length; i++)
				clearTextWithSaltBytes[i] = clearTextBytes[i];
			// Append salt bytes to the new array.
			for (int i = 0; i < saltBytes.Length; i++)
				clearTextWithSaltBytes[clearTextBytes.Length + i] = saltBytes[i];

			// Calculate the hash
			using (HashAlgorithm hash = new SHA256Managed())
			{
				return hash.ComputeHash(clearTextWithSaltBytes);
			}
		}

		public static string HashPasswordString(string textToEncrypt, byte[] saltBytes)
		{
			return UTF8Encoding.UTF8.GetString(HashPasswordBytes(textToEncrypt, saltBytes));
		}

		public static bool IsPasswordValid(string password, byte[] savedSalt, byte[] savedHash)
		{
			Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, savedSalt);
			// Convert the provided password into bytes.
			byte[] clearTextBytes = Encoding.UTF8.GetBytes(password);
			// Create a new array to hold clear text and salt.
			byte[] clearTextWithSaltBytes = new byte[clearTextBytes.Length + savedSalt.Length];
			// Copy clear text bytes into the new array.
			for (int i = 0; i < clearTextBytes.Length; i++)
				clearTextWithSaltBytes[i] = clearTextBytes[i];
			// Append salt bytes to the new array.
			for (int i = 0; i < savedSalt.Length; i++)
				clearTextWithSaltBytes[clearTextBytes.Length + i] = savedSalt[i];
			// Calculate the hash
			HashAlgorithm hash = new SHA256Managed();
			byte[] currentHash = hash.ComputeHash(clearTextWithSaltBytes);
			// Now check if the hash values match.
			bool matched = false;
			if (currentHash.Length == savedHash.Length)
			{
				int i = 0;
				while ((i < currentHash.Length) && (currentHash[i] == savedHash[i]))
				{
					i += 1;
				}
				if (i == currentHash.Length)
				{
					matched = true;
				}
			}
			return (matched);
		}

		public static byte[] GenerateMac(byte[] clearText, byte[] key)
		{
			using (HMACSHA256 hmac = new HMACSHA256(key))
			{
				return hmac.ComputeHash(clearText);
			}
		}

		public static bool IsMacValid(byte[] clearText, byte[] key, byte[] savedMac)
		{
			byte[] recalculatedMac = GenerateMac(clearText, key);
			bool matched = false;
			if (recalculatedMac.Length == savedMac.Length)
			{
				int i = 0;
				while ((i < recalculatedMac.Length) && (recalculatedMac[i] == savedMac[i]))
				{
					i += 1;
				}
				if (i == recalculatedMac.Length)
				{
					matched = true;
				}
				return (matched);
			}
			return false;
		}
	}
}