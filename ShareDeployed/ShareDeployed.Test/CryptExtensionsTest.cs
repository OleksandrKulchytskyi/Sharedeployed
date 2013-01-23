using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Crypt;
using System.Text;
namespace ShareDeployed.Test
{
	[TestClass]
	public class CryptExtensionsTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var salt = CryptExtensions.CreateSalt();
			var hashedPass = CryptExtensions.HashPasswordBytes("vax1111", salt);
			bool actual = false;
			try
			{
				actual = CryptExtensions.IsPasswordValid("vax1111", salt, hashedPass);
			}
			catch (Exception ex)
			{
				if (ex.Message != null)
					Assert.Fail(ex.Message);
			}

			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void TestMacMethod()
		{
			var text = Encoding.UTF8.GetBytes("Hello world");
			var key = Encoding.UTF8.GetBytes("pass1234");
			var actual = false;
			try
			{
				var mac = CryptExtensions.GenerateMac(text, key);
				actual = CryptExtensions.IsMacValid(text, key, mac);
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}

			Assert.IsTrue(actual);
		}
	}
}
