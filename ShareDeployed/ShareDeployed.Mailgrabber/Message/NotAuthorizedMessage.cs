using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Message
{
	public class NotAuthorizedMessage
	{
		public NotAuthorizedMessage(bool tryToLoggedIn)
		{
			IsTriedToLog = tryToLoggedIn;
		}

		public bool IsTriedToLog { get; set; }
	}
}
