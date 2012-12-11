using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ShareDeployed.Common.Models
{
	[DataContract()]
	public class Expense
	{
		public Expense()
		{
			Id = 0;
			Time = DateTime.Now;
			Description = string.Empty;
		}

		[Key]
		[DataMember(IsRequired = true)]
		public int Id { get; set; }

		[Required]
		[DataMember(IsRequired=true)]
		public int UserId { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		public string Name { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		[DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
		public DateTime Time { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		public decimal Amount { get; set; }

		[Required]
		[DataMember(IsRequired = true)]
		public bool IsRequired { get; set; }

		[DataMember(IsRequired = true)]
		public string Description { get; set; }
	}

	[DataContract()]
	public class ExpenseTotal
	{
		[DataMember(IsRequired=true)]
		public decimal Total { get; set; }
	}
}
