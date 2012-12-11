using ShareDeployed.Common.Models;
using ShareDeployed.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.DataAccess.Implementation
{
	public class WordRepostitory : GenericRepository<Word>
	{
		public WordRepostitory(IContext context)
			: base(context)
		{
		}
	}

	public class RevenueRepostitory : GenericRepository<Revenue>
	{
		public RevenueRepostitory(IContext context)
			: base(context)
		{
		}
	}

	public class ExpenseRepostitory : GenericRepository<Expense>
	{
		public ExpenseRepostitory(IContext context)
			: base(context)
		{
		}
	}

	public class UserRepostitory : GenericRepository<User>
	{
		public UserRepostitory(IContext context)
			: base(context)
		{
		}
	}
}
