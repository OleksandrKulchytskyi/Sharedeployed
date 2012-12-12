using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest2
	{
		[TestMethod]
		public void TestMethod1()
		{
			using(DataAccess.MessangerContext context=new DataAccess.MessangerContext())
			{
				var data=context.Application.FirstOrDefault(x => x.AppId.Equals("398338ad-aec9-4707-a713-6b446da0c015", StringComparison.OrdinalIgnoreCase));
				if (data != null &&
					data.SentMessages.ToList().Count > 0)
				{

				}
				else
					Assert.Fail();
					
			}
		}
	}
}
