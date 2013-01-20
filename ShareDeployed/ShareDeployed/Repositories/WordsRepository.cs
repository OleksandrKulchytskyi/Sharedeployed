using ShareDeployed.Common;
using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShareDeployed.Repositories
{
	public class WordsRepository : IRepositoryEx<Word>, IDisposable
	{
		private bool _disposed = false;
		private readonly IUnityOfWork _unityOfWork;
		private readonly DataAccess.ShareDeployedContext _dbContext;

		public WordsRepository(IUnityOfWork unity)
		{
			_unityOfWork = unity;
			_dbContext = unity as DataAccess.ShareDeployedContext;
		}

		public void InsertOrUpdate(Word ent)
		{
			Word foundEntity = null;
			try
			{
				foundEntity = _dbContext.Word.Single(x => x.Id == ent.Id);
			}
			catch (InvalidOperationException)
			{
				_dbContext.Word.Add(ent);
				_unityOfWork.Commit();
				return;
			}

			if (foundEntity == null)
			{
				_dbContext.Word.Add(ent);
				_unityOfWork.Commit();
			}
			else
			{
				foundEntity.Translation = ent.Translation;
				foundEntity.ForeignWord = ent.ForeignWord;
				foundEntity.Complicated = ent.Complicated;
				foundEntity.UserId = ent.UserId;

				_unityOfWork.Commit();
			}
		}

		public void Delete(Word ent)
		{
			_dbContext.Entry<Word>(ent).State = System.Data.EntityState.Deleted;
			_unityOfWork.Commit();
		}

		public IQueryable<Word> GetAll()
		{
			return _dbContext.Word;
		}

		public IQueryable<Word> GetAll(System.Linq.Expressions.Expression<Func<Word, bool>> exp)
		{
			return _dbContext.Word.Where(exp);
		}

		public Word Find(System.Linq.Expressions.Expression<Func<Word, bool>> exp)
		{
			return _dbContext.Word.FirstOrDefault(exp);
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
					if (_dbContext != null)
						_dbContext.Dispose();

					GC.Collect();
				}
				_disposed = true;
			}
		}

		public void InsertBatch(IEnumerable<Word> entities)
		{
			if (entities == null)
				return;

			foreach (Word word in entities)
			{
				_dbContext.Word.Add(word);
			}
			_unityOfWork.Commit();
		}
	}
}