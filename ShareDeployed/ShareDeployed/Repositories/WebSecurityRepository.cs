using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ShareDeployed.Models;

namespace ShareDeployed.Repositories
{
	public class WebSecurityRepository : IAspUserRepository
	{
		//smal trick to workaround exception
		//(when user closed tab in browser without performing logout operation, on next web navigation exception will be thrown)
		static WebSecurityRepository()
		{
			System.Data.Entity.Database.SetInitializer<UsersContext>(null);
			try
			{
				using (var context = new UsersContext())
				{
				}
				WebMatrix.WebData.WebSecurity.InitializeDatabaseConnection("Somee", "UserProfile", "UserId", "UserName", autoCreateTables: true);
			}
			catch { }
		}

		bool _disposed = false;
		private readonly UsersContext _db;
		private static readonly Func<UsersContext, string, UserProfile> getUserByName = (db, name) => db.UserProfiles.FirstOrDefault(u => u.UserName == name);
		private static readonly Func<UsersContext, int, UserProfile> getUserById = (db, id) => db.UserProfiles.FirstOrDefault(u => u.UserId == id);
		private static readonly Func<UsersContext, List<webpages_Roles>> getRoles = (db) => db.webpages_Roles.ToList();
		private static readonly Func<UsersContext, string, webpages_Roles> getRole = (db, roleName) => db.webpages_Roles.
																					FirstOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

		public WebSecurityRepository(UsersContext context)
		{
			_db = context;
		}

		public IQueryable<Models.UserProfile> UserProfiles
		{
			get { return _db.UserProfiles; }
		}

		public bool Exist(string name)
		{
			return getUserByName(_db, name) != null;
		}

		public UserProfile GetById(int id)
		{
			return getUserById(_db, id);
		}

		public UserProfile GetByName(string name)
		{
			return getUserByName(_db, name);
		}

		public webpages_Roles GetRole(string roleName)
		{
			return getRole(_db, roleName);
		}

		public List<webpages_Roles> GetRoles()
		{
			return getRoles(_db);
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
					if (_db != null && _db is IDisposable)
					{
						(_db as IDisposable).Dispose();
						GC.Collect();
					}
				}
				_disposed = true;
			}
		}
	}
}