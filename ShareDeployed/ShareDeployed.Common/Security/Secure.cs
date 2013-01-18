using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Security
{
	public class Secure
	{
		RijndaelManaged Crypto(string keySeed, string saltString)
		{
			byte[] salt = UTF8Encoding.UTF8.GetBytes(saltString);
			using (Rfc2898DeriveBytes derivedBytes = new Rfc2898DeriveBytes(keySeed, salt, 1000))
			{
				RijndaelManaged cryprto = new RijndaelManaged();
				cryprto.KeySize = 128;
				cryprto.Key = derivedBytes.GetBytes(16);
				cryprto.IV = derivedBytes.GetBytes(16);
				return cryprto;
			}
		}

	}
}
