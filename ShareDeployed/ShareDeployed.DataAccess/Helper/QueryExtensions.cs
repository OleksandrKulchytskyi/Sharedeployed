using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ShareDeployed.DataAccess.Helper
{
	public static class QuerytExtensions
	{
		public static IQueryable<T> LocalOrDatabase<T>(this DbContext context, Expression<Func<T,bool>> expression) where T:class
		{
			IEnumerable<T> localResults= context.Set<T>().Local.Where(expression.Compile());
			if(localResults.Any())
				return localResults.AsQueryable();

			return  context.Set<T>().Where(expression);
		}
	}
}
