using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.ViewModels
{
	public class GroupMessagesVM
	{
		public GroupMessagesVM()
		{
			Messages = new List<MessageViewModel>();
		}

		public string GroupName { get; set; }
		public ICollection<MessageViewModel> Messages { get; set; }
	}
}