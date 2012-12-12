using ShareDeployed.Common.Models;
using ShareDeployed.DataAccess.Interfaces;
using ShareDeployed.DataAccess.Mappings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess
{
	public class MessangerContext : DbContext, IContext
	{
		public static bool TraceEnabled = false;

		public MessangerContext()
			: base("Messanger")
		{
			// enable sql tracing
			if (TraceEnabled)
			{
				///((IObjectContextAdapter)this).ObjectContext
			}
		}

		public MessangerContext(string conName)
			: base(conName)
		{ }

		//private static DbConnection CreateConnection(string nameOrConnectionString)
		//{
		//	// does not support entity connection strings
		////	EFTracingProviderFactory.Register();

		//	ConnectionStringSettings connectionStringSetting =
		//		ConfigurationManager.ConnectionStrings[nameOrConnectionString];
		//	string connectionString;
		//	string providerName;

		//	if (connectionStringSetting != null)
		//	{
		//		connectionString = connectionStringSetting.ConnectionString;
		//		providerName = connectionStringSetting.ProviderName;
		//	}
		//	else
		//	{
		//		providerName = "System.Data.SqlClient";
		//		connectionString = nameOrConnectionString;
		//	}

		//	return CreateConnection(connectionString, providerName);
		//}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add(new MessangerClientMap());
			modelBuilder.Configurations.Add(new MessageMap());
			modelBuilder.Configurations.Add(new MessangerGroupMap());
			modelBuilder.Configurations.Add(new MessangerUserMap());
			modelBuilder.Configurations.Add(new MessangerAppMap());
			modelBuilder.Configurations.Add(new MessageResponseMap());

			base.OnModelCreating(modelBuilder);
		}

		public DbSet<MessangerClient> Clients { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessangerGroup> Groups { get; set; }
		public DbSet<MessangerUser> Users { get; set; }
		public DbSet<MessangerApplication> Application { get;set;}
		public DbSet<MessageResponse> MessageResponse { get; set; }

		#region IContext
		bool isAuditEnabled = false;
		public bool IsAuditEnabled
		{
			get
			{
				return isAuditEnabled;
			}
			set
			{
				isAuditEnabled = value;
			}
		}

		public IDbSet<T> GetEntitySet<T>() where T : class
		{
			return this.GetEntitySet<T>();
		}

		public void ChangeState<T>(T entity, System.Data.EntityState state) where T : class
		{
			Entry<T>(entity).State = state;
		}

		public System.Data.Common.DbTransaction BeginTransaction()
		{
			var connection = this.Database.Connection;
			if (connection.State != ConnectionState.Open)
			{
				connection.Open();
			}

			return connection.BeginTransaction(IsolationLevel.ReadCommitted);
		}

		int IContext.Commit()
		{
			return SaveChanges();
		}
		#endregion

		public override int SaveChanges()
		{
			
			return base.SaveChanges();
		}
	}
}
