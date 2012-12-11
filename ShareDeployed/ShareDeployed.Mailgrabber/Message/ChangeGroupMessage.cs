using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Message
{
	public class ChangeGroupMessage
	{
		public ChangeGroupMessage(MessangerGroup changed)
		{
			DefaultGroup = changed;
		}

		public MessangerGroup DefaultGroup { get; set; }
	}
}
