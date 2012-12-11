using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ShareDeployed.DataAccess.Interfaces;

namespace ShareDeployed.DataAccess.Implementation
{
	public class GenericRepository<T> : IRepository<T> where T : class
	{
		public GenericRepository(IContext context)
		{
			Context = context;
		}

		public IContext Context
		{
			get;
			set;
		}

		public void Add(T entity)
		{
			this.Context.GetEntitySet<T>().Add(entity);
		}

		public void Update(T entity)
		{
			this.Context.ChangeState(entity, System.Data.EntityState.Modified);
		}

		public void Delete(T entity)
		{
			this.Context.ChangeState(entity, System.Data.EntityState.Deleted);
		}

		public void AddBulk(IEnumerable<T> items)
		{
			foreach (var item in items)
				this.Context.GetEntitySet<T>().Add(item);
		}

		public T FindSingle(System.Linq.Expressions.Expression<Func<T, bool>> predicate = null, params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
		{
			var set = FindIncluding(includes);
			return (predicate == null) ?
				   set.FirstOrDefault() :
				   set.FirstOrDefault(predicate);
		}

		public IQueryable<T> Find(System.Linq.Expressions.Expression<Func<T, bool>> predicate = null, params System.Linq.Expressions.Expression<Func<T, object>>[] includes)
		{
			var set = FindIncluding(includes);
			return (predicate == null) ? set : set.Where(predicate);
		}

		public IQueryable<T> FindIncluding(params System.Linq.Expressions.Expression<Func<T, object>>[] includeProperties)
		{
			var set = this.Context.GetEntitySet<T>();

			if (includeProperties != null)
			{
				foreach (var include in includeProperties)
				{
					set.Include(include);
				}
			}
			return set.AsQueryable();
		}

		public int Count(System.Linq.Expressions.Expression<Func<T, bool>> predicate = null)
		{
			var set = this.Context.GetEntitySet<T>();
			return (predicate == null) ?
				   set.Count() :
				   set.Count(predicate);
		}

		public bool Exist(System.Linq.Expressions.Expression<Func<T, bool>> predicate = null)
		{
			var set = this.Context.GetEntitySet<T>();
			return (predicate == null) ? set.Any() : set.Any(predicate);
		}
	}
}
