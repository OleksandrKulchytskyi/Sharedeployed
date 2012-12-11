using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace ShareDeployed.Services
{
	public interface ICryptoService
	{
		string CreateSalt();
	}

	public class CryptoService : ICryptoService
	{
		public string CreateSalt()
		{
			var data = new byte[0x10];

			using (var crypto = new RNGCryptoServiceProvider())
			{
				crypto.GetBytes(data);

				return Convert.ToBase64String(data);
			}
		}
	}
}