﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Common.Models;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace ShareDeployed.Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void ToJsonString()
		{
			Revenue revenue=new Revenue() { Name = "Cake 5", UserId = 2, Amount = 23, Time = DateTime.Now , Description="Hello world", Id=3};
			JsonSerializer ser = new JsonSerializer();
			var sb=new StringBuilder();
			using (var tw = new StringWriter(sb))
				ser.Serialize(tw, revenue);

			string data = sb.ToString();
		}

		[TestMethod]
		public void Base64()
		{
			string decode = "admin:vax804".toBase64Utf8();
			if(decode!=null)
			{

			}
		}

		[TestMethod]
		public void DateTest()
		{
			var expire = DateTime.Now.AddMinutes(1);
			while(expire>DateTime.Now)
			{
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
			}
		}


	}
}
