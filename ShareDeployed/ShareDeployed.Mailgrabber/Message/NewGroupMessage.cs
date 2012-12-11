using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Message
{
	internal class NewGroupMessage
	{
		public NewGroupMessage(MessangerGroup group)
		{
			NewGroup = group;
		}

		public MessangerGroup NewGroup { get; set; }
	}
}
