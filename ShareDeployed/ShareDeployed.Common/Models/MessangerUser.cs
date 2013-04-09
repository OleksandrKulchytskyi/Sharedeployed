using ShareDeployed.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class MessangerUser
	{
		public MessangerUser()
		{
			ConnectedClients = new SafeCollection<MessangerClient>();
			OwnedGroups = new SafeCollection<MessangerGroup>();
			Groups = new SafeCollection<MessangerGroup>();
			AllowedGroups = new SafeCollection<MessangerGroup>();
			ReadMessages = new SafeCollection<Message>();
		}

		[Key]
		public int Key { get; set; }

		[MaxLength(200)]
		public string Id { get; set; }
		public string Name { get; set; }

		// MD5 email hash for gravatar
		public string Hash { get; set; }
		public string Salt { get; set; }
		public string HashedPassword { get; set; }
		public DateTime LastActivity { get; set; }
		public DateTime? LastNudged { get; set; }
		public int Status { get; set; }

		[StringLength(200)]
		public string Note { get; set; }

		[StringLength(200)]
		public string AfkNote { get; set; }

		public bool IsAfk { get; set; }

		[StringLength(2)]
		public string Flag { get; set; }

		public string Identity { get; set; }

		public string Email { get; set; }

		public bool IsAdmin { get; set; }
		public bool IsBanned { get; set; }

		// List of clients that are currently connected for this user
		public virtual ICollection<MessangerClient> ConnectedClients { get; set; }
		public virtual ICollection<MessangerGroup> OwnedGroups { get; set; }
		public virtual ICollection<MessangerGroup> Groups { get; set; }

		// Private groups this user is allowed to go into
		public virtual ICollection<MessangerGroup> AllowedGroups { get; set; }

		// List of messages that have been read BY THIS USER
		public virtual ICollection<Message> ReadMessages { get; set; }

		public bool HasCredentials()
		{
			return !string.IsNullOrEmpty(HashedPassword) && !string.IsNullOrEmpty(Name);
		}
	}
}
