using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class MessangerApplication
	{
		public MessangerApplication()
		{
			SentMessages =new Common.Infrastructure.SafeCollection<Message>();
		}

		[Key]
		public int Key { get; set; }

		[Required(ErrorMessage = "Application Id cannot be empty.")]
		public string AppId { get; set; }

		public string MachineName { get; set; }

		[DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
		public DateTime LastLoggedIn { get; set; }

		public virtual ICollection<Message> SentMessages { get; set; }
	}
}
