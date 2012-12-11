using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ShareDeployed.Common.Models
{
	[DataContract()]
	public class Revenue
	{
		public Revenue()
		{
			Id = 0;
			Time = DateTime.Now;
			Description = string.Empty;
		}

		[Key]
		[DataMember(IsRequired = true)]
		public int Id { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		//[System.ComponentModel.DataAnnotations.Schema.Column()]
		public int UserId { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		public string Name { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		[DataType(DataType.Date)]
		public DateTime Time { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		public decimal Amount { get; set; }

		[DataMember(IsRequired=true)]
		public string Description { get; set; }
	}

	[DataContract()]
	public class RevenueTotal
	{
		[DataMember(IsRequired = true)]
		public decimal Total { get; set; }
	}
}
