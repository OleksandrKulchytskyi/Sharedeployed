using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShareDeployed.Mailgrabber.Model;

namespace ShareDeployed.Mailgrabber.Message
{
	internal class LoginMessage
	{
		internal LoginModel Login { get; set; }

		public LoginMessage(LoginModel logData)
		{
			Login = logData;
		}
	}
}
