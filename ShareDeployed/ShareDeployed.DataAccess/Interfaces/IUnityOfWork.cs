using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareDeployed.DataAccess.Interfaces
{
	public interface IUnityOfWork:IDisposable
	{
		bool EnableAuditLog { get; set; }

		int Commit();
		IRepository<TSet> GetRepository<TSet>() where TSet : class;
		void AddRepository<TSet>(object repository) where TSet : class;
		DbTransaction BeginTransaction();
	}
}
