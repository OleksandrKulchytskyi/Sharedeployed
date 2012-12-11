using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ShareDeployed.Common.Models
{
	[DataContract()]
	//[Table("Word",Schema="dbo")]
	public class Word
	{
		public Word()
		{
			ForeignWord = string.Empty;
			Translation = string.Empty;
		}

		[Key]
		[DataMember(IsRequired = true)]
		public int Id { get; set; }
		
		[Required]
		[DataMember(IsRequired = true)]
		public int UserId { get; set; }

		[Required(AllowEmptyStrings=false,ErrorMessage="Foreign word cannot be empty")]
		[DataMember(IsRequired = true)]
		public string ForeignWord { get; set; }

		[Required(AllowEmptyStrings = false, ErrorMessage = "Foreign word cannot be empty")]
		[DataMember(IsRequired = true)]
		public string Translation { get; set; }

		[DataMember(IsRequired = true)]
		public bool Complicated { get; set; }
	}
}
