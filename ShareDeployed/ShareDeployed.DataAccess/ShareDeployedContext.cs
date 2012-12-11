using ShareDeployed.Common;
using ShareDeployed.Common.Models;
using ShareDeployed.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;

namespace ShareDeployed.DataAccess
{
	public sealed class ShareDeployedContext : DbContext, IContext, Common.IUnityOfWork
	{
		public ShareDeployedContext()
			: base("Somee")
		{
		}

		public ShareDeployedContext(string conName)
			: base(conName)
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}

		public DbSet<Revenue> Revenue { get; set; }
		public DbSet<Expense> Expense { get; set; }
		public DbSet<User> User { get; set; }
		public DbSet<Word> Word { get; set; }

		public void Commit()
		{
			int result = SaveChanges();
			//if (MvcApplication.Logger != null)
				//MvcApplication.Logger.InfoFormat("Affected rows {0}", result);
		}

		#region IContext

		public bool IsAuditEnabled
		{
			get;
			set;
		}

		public IDbSet<T> GetEntitySet<T>() where T : class
		{
			return Set<T>();
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

		private static bool IsChanged(DbEntityEntry entity)
		{
			return IsStateEqual(entity, EntityState.Added) ||
				   IsStateEqual(entity, EntityState.Deleted) ||
				   IsStateEqual(entity, EntityState.Modified);
		}

		private static bool IsStateEqual(DbEntityEntry entity, EntityState state)
		{
			return (entity.State & state) == state;
		}
	}
}