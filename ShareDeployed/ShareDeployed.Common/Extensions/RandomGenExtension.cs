using System;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class RandomGenHelper
	{
		static public Int32 GetCryptographicallyRandomInt32()
		{
			byte[] randomBytes = new byte[4];
			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(randomBytes);
				Int32 randomInt = BitConverter.ToInt32(randomBytes, 0);
				return randomInt;
			}
		}

		static public string GeneratePassword(Random r, int length, char[] allowableChars)
		{
			StringBuilder passwordBuilder = new StringBuilder((int)length);

			for (int i = 0; i < length; i++)
			{
				int nextInt = r.Next(allowableChars.Length);
				char c = allowableChars[nextInt];
				passwordBuilder.Append(c);
			}
			return passwordBuilder.ToString();
		}
	}
}