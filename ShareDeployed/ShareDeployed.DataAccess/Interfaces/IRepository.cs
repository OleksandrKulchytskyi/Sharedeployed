using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ShareDeployed.DataAccess.Interfaces
{
	public interface IRepository<T> where T : class
	{
		IContext Context { get; set; }

		void AddBulk(IEnumerable<T> items);
		void Add(T entity);

		void Update(T entity);
		void Delete(T entity);

		T FindSingle(Expression<Func<T, bool>> predicate = null, params Expression<Func<T, object>>[] includes);

		IQueryable<T> Find(Expression<Func<T, bool>> predicate = null, params Expression<Func<T, object>>[] includes);
		IQueryable<T> FindIncluding(params Expression<Func<T, object>>[] includeProperties);

		int Count(Expression<Func<T, bool>> predicate = null);
		bool Exist(Expression<Func<T, bool>> predicate = null);
	}
}
