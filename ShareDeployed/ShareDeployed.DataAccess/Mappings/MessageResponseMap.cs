using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShareDeployed.Common.Models;
using System.Data.Entity.ModelConfiguration;

namespace ShareDeployed.DataAccess.Mappings
{
	public class MessageResponseMap:EntityTypeConfiguration<MessageResponse>
	{
		public MessageResponseMap()
		{
			this.ToTable("MesageResponse");
			this.HasKey(mr => mr.Key);

			this.Property(mr => mr.IsSent).IsRequired();
			this.Property(mr => mr.ResponseText).IsRequired().HasColumnType("text");
		}
	}
}
