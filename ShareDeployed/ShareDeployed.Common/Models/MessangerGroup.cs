using ShareDeployed.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class MessangerGroup
	{
		public MessangerGroup()
		{
			Owners = new SafeCollection<MessangerUser>();
			Messages = new SafeCollection<Message>();
			Users = new SafeCollection<MessangerUser>();
			AllowedUsers = new SafeCollection<MessangerUser>();
		}

		[Key]
		public int Key { get; set; }

		public DateTime? LastNudged { get; set; }

		[MaxLength(200)]
		public string Name { get; set; }
		public bool Closed { get; set; }

		[StringLength(80)]
		public string Topic { get; set; }
		[StringLength(200)]
		public string Welcome { get; set; }

		public bool Private { get; set; }

		public string InviteCode { get; set; }

		// Creator of the group
		public virtual MessangerUser Creator { get; set; }
		public int? CreatorKey { get; set; }

		// Creator and owners
		public virtual ICollection<MessangerUser> AllowedUsers { get; set; }
		public virtual ICollection<MessangerUser> Owners { get; set; }
		public virtual ICollection<Message> Messages { get; set; }
		public virtual ICollection<MessangerUser> Users { get; set; }
	}
}
