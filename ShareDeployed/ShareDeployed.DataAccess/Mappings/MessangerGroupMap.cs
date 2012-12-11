using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessangerGroupMap : System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<Common.Models.MessangerGroup>
	{
		public MessangerGroupMap()
		{
			// Primary Key
			this.HasKey(r => r.Key);

			// Properties
			this.Property(r => r.InviteCode)
				.IsFixedLength()
				.HasMaxLength(6);

			this.Property(r => r.Topic)
				.HasMaxLength(80);

			// Table & Column Mappings
			this.ToTable("MessangerGroups");
			this.Property(r => r.Key).HasColumnName("Key");
			this.Property(r => r.LastNudged).HasColumnName("LastNudged");
			this.Property(r => r.Name).HasColumnName("Name");
			this.Property(r => r.CreatorKey).HasColumnName("Creator_Key");
			this.Property(r => r.Private).HasColumnName("Private");
			this.Property(r => r.InviteCode).HasColumnName("InviteCode");
			this.Property(r => r.Closed).HasColumnName("Closed");
			this.Property(r => r.Topic).HasColumnName("Topic");
			this.Property(r => r.Welcome).HasColumnName("Welcome");

			// Relationships
			this.HasMany(r => r.AllowedUsers)
				.WithMany(u => u.AllowedGroups)
				.Map(m =>
					{
						m.ToTable("GroupMessageUser1");
						m.MapLeftKey("ChatRoom_Key");
						m.MapRightKey("ChatUser_Key");
					});

			this.HasMany(g => g.Owners)
				.WithMany(u => u.OwnedGroups)
				.Map(m =>
					{
						m.ToTable("GroupMessangerUsers");
						m.MapLeftKey("ChatRoom_Key");
						m.MapRightKey("ChatUser_Key");
					});

			this.HasMany(r => r.Users)
				.WithMany(u => u.Groups)
				.Map(m =>
					{
						m.ToTable("MessangerUserGroups");
						m.MapLeftKey("ChatRoom_Key");
						m.MapRightKey("ChatUser_Key");
					});

			this.HasOptional(r => r.Creator)
				.WithMany()
				.HasForeignKey(r => r.CreatorKey);
		}
	}
}
