using ShareDeployed.Common.Models;
using ShareDeployed.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace ShareDeployed.DataAccess.Implementation
{
	public class UnityOfWork : IUnityOfWork
	{
		bool _disposed = false;
		readonly IContext _context;

		private Dictionary<Type, object> _repositories = null;
		private DbTransaction _transaction = null;

		public UnityOfWork(IRepository<User> _userRepo, IRepository<Expense> _expenseRepo,
							IRepository<Revenue> _revenueRepo, IRepository<Word> _wordsRepo, IContext context)
		{
			_context = context;
			_repositories = new Dictionary<Type, object>();

			AddRepository<Word>(_wordsRepo);
			AddRepository<Expense>(_expenseRepo);
			AddRepository<Revenue>(_revenueRepo);
			AddRepository<User>(_userRepo);
		}

		public bool EnableAuditLog
		{
			get { return _context.IsAuditEnabled; }
			set { _context.IsAuditEnabled = value; }
		}

		public int Commit()
		{
			return _context.Commit();
		}

		public System.Data.Common.DbTransaction BeginTransaction()
		{
			_transaction = _context.BeginTransaction();
			return _transaction;
		}

		public IRepository<TSet> GetRepository<TSet>() where TSet : class
		{
			if (_repositories.Keys.Contains(typeof(TSet)))
				return _repositories[typeof(TSet)] as IRepository<TSet>;
			return null;
		}

		public void AddRepository<TSet>(object repository) where TSet : class
		{
			if (!_repositories.ContainsKey(typeof(TSet)))
			{
				if (!object.ReferenceEquals((repository as IRepository<TSet>).Context, this._context))
					(repository as IRepository<TSet>).Context = _context;
				_repositories.Add(typeof(TSet), repository);
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_repositories.Clear();

					if (null != _transaction && null != _transaction.Connection)
						_transaction.Connection.Dispose();

					if (null != _context && null != _context)
						_context.Dispose();
				}
				_disposed = true;
			}
		}
	}
}
