using System.Collections.Generic;

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