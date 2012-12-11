using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Outlook;
using System.Collections.Generic;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest3
	{
		[TestMethod]
		public void TestOutlookModule()
		{
			using (OutlookManager manager = new Common.Outlook.OutlookManager())
			{
				List<OutlookMailInfo> expected = null;
				List<OutlookMailInfo> actual = null;
				actual = manager.GetMailList();
				Assert.AreNotEqual(expected, actual);
			}
		}
	}
}
