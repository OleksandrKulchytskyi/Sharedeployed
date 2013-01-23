using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Crypt;
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
					Assert.Fail();
			}

			Assert.IsTrue(actual);
		}
	}
}
