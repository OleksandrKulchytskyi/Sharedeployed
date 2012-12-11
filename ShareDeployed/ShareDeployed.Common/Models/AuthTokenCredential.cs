using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Models
{
	public class AuthTokenCredential
	{
		public AuthTokenCredential()
		{
			UserName = string.Empty;
			Password = string.Empty;
		}

		[Required(AllowEmptyStrings = false, ErrorMessage = "UserName cannot be empty")]
		public string UserName { get; set; }

		[Required(AllowEmptyStrings = false, ErrorMessage = "Password cannot be empty")]
		[MinLength(6, ErrorMessage = "Password must contains at least 6 characters")]
		public string Password { get; set; }
	}
}
