using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Interfaces
{
	public interface IContext : IDisposable
	{
		bool IsAuditEnabled { get; set; }
		IDbSet<T> GetEntitySet<T>() where T : class;
		void ChangeState<T>(T entity, EntityState state) where T : class;
		DbTransaction BeginTransaction();
		int Commit();
	}
}
