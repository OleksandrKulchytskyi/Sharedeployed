using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessangerAppMap:EntityTypeConfiguration<MessangerApplication>
	{
		public MessangerAppMap()
		{
			this.ToTable("Applications");
			this.HasKey(m => m.Key);
			this.Property(x => x.AppId).IsRequired();

			this.Property(x => x.LastLoggedIn).IsOptional();
			this.Property(x => x.MachineName).IsOptional();
		}
	}
}
