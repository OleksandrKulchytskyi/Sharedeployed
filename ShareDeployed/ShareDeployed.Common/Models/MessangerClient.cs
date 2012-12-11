using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class MessangerClient
	{
		[Key]
		public int Key { get; set; }

		public string Id { get; set; }
		public MessangerUser User { get; set; }
		public DateTimeOffset LastActivity { get; set; }

		public string UserAgent { get; set; }

		public int UserKey { get; set; }
	}
}
