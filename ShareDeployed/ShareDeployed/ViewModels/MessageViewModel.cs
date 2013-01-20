using ShareDeployed.Common.Models;
using System;

namespace ShareDeployed.ViewModels
{
	public class MessageViewModel
	{
		public MessageViewModel(Message message)
		{
			Id = message.Id;
			From = message.From ?? string.Empty;
			Content = message.Content ?? string.Empty;
			Subject = message.Subject ?? string.Empty;

			if (message.User != null)
				User = new UserViewModel(message.User);

			When = message.When;
		}

		public string Id { get; set; }

		public string From { get; set; }

		public string Subject { get; set; }

		public string Content { get; set; }

		public DateTimeOffset When { get; set; }

		public UserViewModel User { get; set; }
	}
}