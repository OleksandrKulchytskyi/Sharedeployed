using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ShareDeployed.Common.Models
{
	[DataContract()]
	public class User
	{
		[Key]
		[DataMember(IsRequired = true)]
		public int Id { get; set; }
		
		[Required]
		[DataMember(IsRequired = true)]
		public string Name { get; set; }

		[DataMember(IsRequired = true)]
		public string LogonName { get; set; }
	}
}
