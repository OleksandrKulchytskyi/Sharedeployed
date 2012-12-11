using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessageMap : EntityTypeConfiguration<Message>
	{
		public MessageMap()
		{
			// Primary Key
			this.HasKey(m => m.Key);

			// Properties
			// Table & Column Mappings
			this.ToTable("Messages");
			this.Property(m => m.Content).HasColumnName("Content");
			this.Property(m => m.Subject).HasColumnName("Subject");
			this.Property(m => m.From).HasColumnName("From");
			this.Property(m => m.FromEmail).HasColumnName("FromEmail");
			this.Property(m => m.CC).HasColumnName("CC");
			this.Property(m => m.IsNew).HasColumnName("IsNew");
			this.Property(m => m.Id).HasColumnName("Id");
			this.Property(m => m.When).HasColumnName("When");
			this.Property(m => m.GroupKey).HasColumnName("Room_Key");
			this.Property(m => m.UserKey).HasColumnName("User_Key");

			// Relationships
			this.HasOptional(m => m.Group).WithMany(r => r.Messages).HasForeignKey(m => m.GroupKey);

			this.HasOptional(m => m.User).WithMany().HasForeignKey(m => m.UserKey);
		}
	}
}
