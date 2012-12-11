using ShareDeployed.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class Message
	{
		public Message()
		{
			UsersWhoRead = new SafeCollection<MessangerUser>();
		}

		[Key]
		public int Key { get; set; }

		public string Id { get; set; }

		public string Subject { get; set; }

		public string From { get; set; }

		public string FromEmail { get; set; }

		public string CC { get; set; }

		public string Content { get; set; }

		public bool IsNew { get; set; }

		public virtual MessangerGroup Group { get; set; }
		public virtual MessangerUser User { get; set; }
		public DateTimeOffset When { get; set; }

		public int? GroupKey { get; set; }
		public int? UserKey { get; set; }

		//Users that were read this message
		public virtual ICollection<MessangerUser> UsersWhoRead { get; set; }
	}
}
