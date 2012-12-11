using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessangerUserMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<MessangerUser>
	{
		public MessangerUserMap()
		{
			// Primary Key
			this.HasKey(u => u.Key);

			// Properties
			this.Property(u => u.Note)
				.HasMaxLength(200);

			this.Property(u => u.AfkNote)
				.HasMaxLength(200);

			this.Property(u => u.Flag)
				.HasMaxLength(2);

			// Table & Column Mappings
			this.ToTable("MessangerUsers");
			this.Property(u => u.Key).HasColumnName("Key");
			this.Property(u => u.Id).HasColumnName("Id");
			this.Property(u => u.Name).HasColumnName("Name");
			this.Property(u => u.Hash).HasColumnName("Hash");
			this.Property(u => u.LastActivity).HasColumnName("LastActivity");
			this.Property(u => u.LastNudged).HasColumnName("LastNudged");
			this.Property(u => u.Status).HasColumnName("Status");
			this.Property(u => u.HashedPassword).HasColumnName("HashedPassword");
			this.Property(u => u.Salt).HasColumnName("Salt");
			this.Property(u => u.Note).HasColumnName("Note");
			this.Property(u => u.AfkNote).HasColumnName("AfkNote");
			this.Property(u => u.IsAfk).HasColumnName("IsAfk");
			this.Property(u => u.Flag).HasColumnName("Flag");
			this.Property(u => u.Identity).HasColumnName("Identity");
			this.Property(u => u.Email).HasColumnName("Email");
			this.Property(u => u.IsAdmin).HasColumnName("IsAdmin");
			this.Property(u => u.IsBanned).HasColumnName("IsBanned");

			this.HasMany(x => x.ReadMessages).WithMany(x => x.UsersWhoRead)
			.Map(m => m.MapLeftKey("User_Key").MapRightKey("Message_Key").ToTable("UserReadMessage"));
		}
	}
}
