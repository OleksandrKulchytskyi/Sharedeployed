using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessangerClientMap : EntityTypeConfiguration<MessangerClient>
	{
		public MessangerClientMap()
		{
			// Primary Key
			this.HasKey(c => c.Key);

			// Properties
			// Table & Column Mappings
			this.ToTable("MessangerClients");
			this.Property(c => c.Key).HasColumnName("Key");
			this.Property(c => c.Id).HasColumnName("Id");
			this.Property(c => c.UserKey).HasColumnName("User_Key");
			this.Property(c => c.UserAgent).HasColumnName("UserAgent");
			this.Property(c => c.LastActivity).HasColumnName("LastActivity");

			// Relationships
			this.HasRequired(c => c.User)
				.WithMany(u => u.ConnectedClients)
				.HasForeignKey(c => c.UserKey);

		}
	}
}
