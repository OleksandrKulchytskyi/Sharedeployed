using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using ShareDeployed.DataAccess;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using System.Data.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Objects.DataClasses;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Text;

namespace ShareDeployed.Repositories
{
	public class PersistedRepository : IMessangerRepository
	{
		bool _disposed = false;
		private readonly MessangerContext _dbMessanger;
		private readonly ShareDeployedContext _dbShared;

		private static readonly Func<MessangerContext, string, MessangerUser> getUserByName = (db, userName) => db.Users.FirstOrDefault(u => u.Name == userName);
		private static readonly Func<MessangerContext, string, MessangerUser> getUserById = (db, userId) => db.Users.FirstOrDefault(u => u.Id == userId);
		private static readonly Func<MessangerContext, string, MessangerUser> getUserByIdentity = (db, userIdentity) => db.Users.
																														FirstOrDefault(u => u.Identity == userIdentity);
		private static readonly Func<MessangerContext, string, MessangerGroup> getGroupByName = (db, groupName) => db.Groups.FirstOrDefault(r => r.Name == groupName);
		private static readonly Func<MessangerContext, string, MessangerClient> getClientById = (db, clientId) => db.Clients.FirstOrDefault(c => c.Id == clientId);
		private static readonly Func<MessangerContext, string, MessangerClient> getClientByIdWithUser = (db, clientId) => db.Clients.Include(c => c.User).
																														FirstOrDefault(u => u.Id == clientId);

		public PersistedRepository(MessangerContext db, ShareDeployedContext dbShare)
		{
			_dbMessanger = db;
			_dbShared = dbShare;
		}

		public IQueryable<Common.Models.MessangerGroup> Groups
		{
			get { return _dbMessanger.Groups; }
		}

		public IQueryable<Common.Models.MessangerUser> Users
		{
			get { return _dbMessanger.Users; }
		}

		public IQueryable<User> SharedUsers
		{
			get
			{
				return _dbShared.User;
			}
		}

		public IQueryable<MessangerApplication> Application
		{
			get { return _dbMessanger.Application; }
		}

		public IQueryable<MessageResponse> Response
		{
			get { return _dbMessanger.MessageResponse; }
		}

		public IQueryable<Common.Models.MessangerUser> GetOnlineUsers(Common.Models.MessangerGroup group)
		{
			return _dbMessanger.Entry(group).Collection(r => r.Users).Query().Online();
		}

		public IQueryable<Common.Models.MessangerUser> SearchUsers(string name)
		{
			return _dbMessanger.Users.Online().Where(u => u.Name.Contains(name));
		}

		public IQueryable<Common.Models.Message> GetMessagesByGroup(Common.Models.MessangerGroup group)
		{
			return _dbMessanger.Messages.Include(r => r.User).Where(r => r.GroupKey == group.Key);
		}

		private IQueryable<Message> GetMessagesByGroup(string groupName)
		{
			return _dbMessanger.Messages.Include(r => r.Group).Where(r => r.Group.Name == groupName);
		}

		public IQueryable<Common.Models.Message> GetPreviousMessages(string messageId)
		{
			var info = (from m in _dbMessanger.Messages.Include(m => m.Group)
						where m.Id == messageId
						select new
						{
							m.When,
							GroupName = m.Group.Name
						}).FirstOrDefault();

			return from m in GetMessagesByGroup(info.GroupName)
				   where m.When < info.When
				   select m;
		}

		public IQueryable<Common.Models.MessangerGroup> GetAllowedGroups(Common.Models.MessangerUser user)
		{
			// All public and private rooms the user can see.
			return _dbMessanger.Groups.Where(r =>
					   (!r.Private) || (r.Private && r.AllowedUsers.Any(u => u.Key == user.Key)));
		}

		public Common.Models.Message GetMessagesById(string id)
		{
			return _dbMessanger.Messages.FirstOrDefault(m => m.Id == id);
		}

		public Common.Models.MessangerUser GetUserById(string userId)
		{
			return getUserById(_dbMessanger, userId);
		}

		public Common.Models.MessangerGroup GetGroupByName(string groupName)
		{
			return getGroupByName(_dbMessanger, groupName);
		}

		public Common.Models.MessangerUser GetUserByName(string userName)
		{
			return getUserByName(_dbMessanger, userName);
		}

		public Common.Models.MessangerUser GetUserByClientId(string clientId)
		{
			var client = GetClientById(clientId, true);
			if (client != null)
			{
				return client.User;
			}
			return null;
		}

		public Common.Models.MessangerUser GetUserByIdentity(string userIdentity)
		{
			return getUserByIdentity(_dbMessanger, userIdentity);
		}

		public bool AuthenticateUserShared(string uid, string pass, out int userId)
		{
			userId = -1;
			if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(pass))
				return false;
			DoMembershipInitialization();
			if (!WebMatrix.WebData.WebSecurity.UserExists(uid))
				return false;

			if (WebMatrix.WebData.WebSecurity.Login(uid, pass))
			{
				userId = WebMatrix.WebData.WebSecurity.GetUserId(uid);
				return true;
			}

			return false;
		}

		private void DoMembershipInitialization()
		{
			Database.SetInitializer<Models.UsersContext>(null);

			try
			{
				using (var context = new Models.UsersContext())
				{
				}

				WebMatrix.WebData.WebSecurity.InitializeDatabaseConnection("Somee", "UserProfile", "UserId", "UserName", autoCreateTables: true);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("PersistedRepository.DoMembershipInitialization", ex);
				//throw new InvalidOperationException("The ASP.NET Simple Membership database could not be initialized."+
				//"For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588", ex);
			}
		}

		private void RunNonLazy(Action action)
		{
			bool old = _dbMessanger.Configuration.LazyLoadingEnabled;
			try
			{
				_dbMessanger.Configuration.LazyLoadingEnabled = false;
				action();
			}
			finally
			{
				_dbMessanger.Configuration.LazyLoadingEnabled = old;
			}
		}

		public MessangerClient GetClientById(string clientId, bool includeUser = false)
		{
			if (includeUser)
			{
				return getClientByIdWithUser(_dbMessanger, clientId);
			}

			return getClientById(_dbMessanger, clientId);
		}

		public void AddUserGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup group)
		{
			RunNonLazy(() => group.Users.Add(user));
		}

		public void RemoveUserGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup group)
		{
			RunNonLazy(() =>
			{
				// The hack from hell to attach the user to room.Users so delete is tracked
				ObjectContext context = ((IObjectContextAdapter)_dbMessanger).ObjectContext;
				RelationshipManager manger = context.ObjectStateManager.GetRelationshipManager(group);
				IRelatedEnd end = manger.GetRelatedEnd("ShareDeployed.DataAccess.Models.Group_MessangerUsers", "Group_MessangerUsers_Target");
				end.Attach(user);
				group.Users.Remove(user);
			});
		}

		public void Add(Common.Models.MessangerClient client)
		{
			if (client == null)
				throw new ArgumentNullException("client");

			try
			{
				_dbMessanger.Clients.Add(client);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Add(Common.Models.Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			try
			{
				_dbMessanger.Messages.Add(message);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Add(MessangerApplication app)
		{
			if (app == null)
				throw new ArgumentNullException("app");

			try
			{
				_dbMessanger.Application.Add(app);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Add(MessageResponse response)
		{
			if (response == null)
				throw new ArgumentNullException("response");

			try
			{
				_dbMessanger.MessageResponse.Add(response);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Add(Common.Models.MessangerGroup group)
		{
			if (group == null)
				throw new ArgumentNullException("group");

			try
			{
				_dbMessanger.Groups.Add(group);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Add(Common.Models.MessangerUser user)
		{
			try
			{
				_dbMessanger.Users.Add(user);
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
		}

		public void Remove(Common.Models.MessangerClient client)
		{
			_dbMessanger.Clients.Remove(client);
			_dbMessanger.SaveChanges();
		}

		public void Remove(Common.Models.MessangerGroup group)
		{
			_dbMessanger.Groups.Remove(group);
			_dbMessanger.SaveChanges();
		}

		public void Remove(Common.Models.MessangerUser user)
		{
			_dbMessanger.Users.Remove(user);
			_dbMessanger.SaveChanges();
		}

		public void Remove(Common.Models.Message message)
		{
			_dbMessanger.Messages.Remove(message);
			_dbMessanger.SaveChanges();
		}

		public void RemoveAllClients()
		{
			foreach (var c in _dbMessanger.Clients)
			{
				_dbMessanger.Clients.Remove(c);
			}
		}

		public bool IsUserInGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup group)
		{
			return _dbMessanger.Entry(user)
					.Collection(r => r.Groups)
					.Query()
					.Where(r => r.Key == group.Key)
					.Select(r => r.Name)
					.FirstOrDefault() != null;
		}

		public void CommitChanges()
		{
			if (_dbMessanger != null)
				_dbMessanger.SaveChanges();
			if (_dbShared != null)
				_dbShared.SaveChanges();
		}

		public IQueryable<Message> GetAllNewMessges()
		{
			return (from item in _dbMessanger.Messages.Include(x => x.Group) where item.IsNew orderby item.Group.Name select item);
		}

		private string LogValidationErrors(DbEntityValidationException ex)
		{
			StringBuilder messageBuilder = new StringBuilder();
			foreach (DbEntityValidationResult validation
				in ex.EntityValidationErrors)
			{
				messageBuilder.AppendFormat("Error in entity {0} \n\r", validation.Entry.Entity);

				foreach (DbValidationError propertyError in validation.ValidationErrors)
				{
					messageBuilder.AppendFormat("Error in property {0}: {1} \n\r",
												propertyError.PropertyName, propertyError.ErrorMessage);
				}
			}
			string result = messageBuilder.ToString();
			messageBuilder.Clear();
			messageBuilder = null;
			Debug.WriteLine(result);
			return result;
		}

		public DbSet<T> GetDbSet<T>() where T : class
		{
			return _dbMessanger.Set<T>();
		}

		public MessangerApplication GetApplicationByAppId(string appId)
		{
			return _dbMessanger.Application.FirstOrDefault(x => x.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase));
		}

		public void Update(MessangerApplication application)
		{
			try
			{
				DbEntityEntry<MessangerApplication> entry = _dbMessanger.Entry(application);
				if (entry.State == System.Data.EntityState.Detached)
				{
					MessangerApplication attachedEntity = _dbMessanger.Set<MessangerApplication>().Find(application.Key);

					if (attachedEntity == null)
						entry.State = System.Data.EntityState.Modified;
					else
						_dbMessanger.Entry(attachedEntity).CurrentValues.SetValues(application);
				}
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public void Update(Message message)
		{
			try
			{
				DbEntityEntry<Message> entry = _dbMessanger.Entry(message);
				if (entry.State == System.Data.EntityState.Detached)
				{
					Message attachedEntity = _dbMessanger.Set<Message>().Find(message.Key);

					if (attachedEntity == null)
						entry.State = System.Data.EntityState.Modified;
					else
						_dbMessanger.Entry(attachedEntity).CurrentValues.SetValues(message);
				}
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public void Update(MessageResponse response)
		{
			try
			{
				DbEntityEntry<MessageResponse> entry = _dbMessanger.Entry(response);
				if (entry.State == System.Data.EntityState.Detached)
				{
					MessageResponse attachedEntity = _dbMessanger.Set<MessageResponse>().Find(response.Key);

					if (attachedEntity == null)
						entry.State = System.Data.EntityState.Modified;
					else
						_dbMessanger.Entry(attachedEntity).CurrentValues.SetValues(response);
				}
				_dbMessanger.SaveChanges();
			}
			catch (DbEntityValidationException ex)
			{
				string data = LogValidationErrors(ex);
				ex.Data.Add("EntityValidationErrors", data);
				throw;
			}
			catch (Exception ex)
			{
				throw;
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
					if (_dbMessanger != null)
						_dbMessanger.Dispose();

					if (_dbShared != null)
						_dbShared.Dispose();

					GC.Collect();
				}
				_disposed = true;
			}
		}
	}
}