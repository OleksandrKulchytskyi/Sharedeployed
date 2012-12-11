using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace ShareDeployed.Models
{
	public class UsersContext : DbContext
	{
		public UsersContext()
			: base("Somee")
		{ }

		public UsersContext(string conName)
			: base(conName)
		{ }

		public DbSet<UserProfile> UserProfiles { get; set; }
		public DbSet<webpages_Roles> webpages_Roles { get; set; } // add roles table
	}

	[Table("UserProfile")]
	[Bind(Exclude = "UserId")]
	public class UserProfile
	{
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		[ScaffoldColumn(false)]
		public int UserId { get; set; }

		public string UserName { get; set; }

		[DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
		public string Email { get; set; }
	}

	[Table("webpages_Roles")]
	public class webpages_Roles
	{
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int RoleId { get; set; }
		[Display(Name = "Role name")]
		public string RoleName { get; set; }
	}

	public class RegisterExternalLoginModel
	{
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		public string ExternalLoginData { get; set; }
	}

	public class LocalPasswordModel
	{
		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Current password")]
		public string OldPassword { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "New password")]
		public string NewPassword { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm new password")]
		[System.Web.Mvc.Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

	public class LoginModel
	{
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }
	}

	public class RegisterModel
	{
		[Required]
		[Display(Name = "User name")]
		[Remote("CheckNameAvaliability", "Account", ErrorMessage = "User with specified name is alredy registered.")]
		public string UserName { get; set; }

		[Required]
		[DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
		[Display(Name = "Email")]
		[Remote("CheckEmailAvaliability", "Account", ErrorMessage = "User with specified Email address is alredy registered.")]
		public string Email { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm password")]
		[System.Web.Mvc.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

	public class ExternalLogin
	{
		public string Provider { get; set; }
		public string ProviderDisplayName { get; set; }
		public string ProviderUserId { get; set; }
	}
}
