using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.RequetResponse
{
	public class RangeRequest
	{
		[Required]
		[DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
		public DateTime From { get; set; }
		[Required]
		[DataType( System.ComponentModel.DataAnnotations.DataType.Date)]
		public DateTime To { get; set; }
	}
}
