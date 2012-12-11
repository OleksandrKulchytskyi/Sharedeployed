using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ShareDeployed.Common
{
	public interface IUnityOfWork
	{
		void Commit();
	}

	public interface IObjectContext : IDisposable
	{
		int SaveAllChanges();

		IDbSet<T> CreateObjectSet<T>() where T : class;
	}

	public interface IReadOnlyRepository<T> where T : class
	{
		IQueryable<T> GetAll();
		IQueryable<T> GetAll(Expression<Func<T, bool>> exp);

		T Find(Expression<Func<T, bool>> exp);
	}

	public interface IRepository<T> : IReadOnlyRepository<T> where T : class
	{
		void InsertOrUpdate(T ent);
		void Delete(T ent);
	}

	public interface IRepositoryEx<T>:IRepository<T> where T:class
	{
		void InsertBatch(IEnumerable<T> entities);
	}
}
