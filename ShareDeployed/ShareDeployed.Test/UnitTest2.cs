using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Helper;
using System.Text;
using System.Security.Cryptography;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest2
	{
		[TestMethod]
		public void TestMethod1()
		{
			using (DataAccess.MessangerContext context = new DataAccess.MessangerContext())
			{
				var data = context.Application.FirstOrDefault(x => x.AppId.Equals("398338ad-aec9-4707-a713-6b446da0c015", StringComparison.OrdinalIgnoreCase));
				if (data != null &&
					data.SentMessages.ToList().Count > 0)
				{
				}
				else
					Assert.Fail();
			}
		}

		[TestMethod]
		public void TimeStampTest()
		{
			var data = DateTime.UtcNow.ToUnixTimestamp();
			if (data == 0)
				Assert.Fail();

			var data2 = DateTime.UtcNow.ToString("u");
			if (string.IsNullOrEmpty(data2))
				Assert.Fail();
		}

		[TestMethod]
		public void HmacAndPassCryptTest()
		{
			var hash = ComputeHash("LohrhqqoDy6PhLrHAXi7dUVACyJZilQtlDzNbLqzXlw=", "hello");
			Assert.IsNotNull(hash);

			try
			{
				var saltB=Common.Crypt.PassCrypt.GenerateSalt();
				var passB = Common.Crypt.PassCrypt.HashPassword("helloworld", saltB);
				bool theSame = Common.Crypt.PassCrypt.Verify("helloworld", passB);
				Assert.IsTrue(theSame);
			}
			catch (Exception ex) { if (ex is AssertFailedException) throw; }
		}

		private static string ComputeHash(string hashedPassword, string message)
		{
			var key = Encoding.UTF8.GetBytes(hashedPassword.ToUpper());
			string hashString;

			using (var hmac = new HMACSHA256(key))
			{
				var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
				hashString = Convert.ToBase64String(hash);
			}

			return hashString;
		}
	}
}
