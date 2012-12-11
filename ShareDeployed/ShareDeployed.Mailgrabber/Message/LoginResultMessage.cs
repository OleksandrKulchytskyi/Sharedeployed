using ShareDeployed.Mailgrabber.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Message
{
	internal class LoginResultMessage
	{
		internal LoginResult LoginResult { get; set; }

		public LoginResultMessage(LoginResult logData)
		{
			LoginResult = logData;
		}

	}
}
