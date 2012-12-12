using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ShareDeployed.Common.Models
{
	[DataContract]
	public class MessageResponse
	{
		[Key]
		[DataMember(IsRequired=true)]
		public int Key { get; set; }

		[DataMember(IsRequired=true)]
		[Required(ErrorMessage = "Response text cannot be empty.")]
		public string ResponseText { get; set; }

		[DataMember(IsRequired=true)]
		[Required]
		public bool IsSent { get; set; }
	}
}
