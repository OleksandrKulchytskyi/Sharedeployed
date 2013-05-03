using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Outlook;
using System.Collections.Generic;

namespace ShareDeployed.Test
{
	[TestClass]
	public class OutlookMnagerTest
	{
		[TestMethod]
		public void TestOutlookModule()
		{
			using (OutlookManager manager = new Outlook.OutlookManager())
			{
				List<OutlookMailInfo> expected = null;
				List<OutlookMailInfo> actual = null;
				actual = manager.GetMailList();
				Assert.AreNotEqual(expected, actual);
			}
		}
	}
}
