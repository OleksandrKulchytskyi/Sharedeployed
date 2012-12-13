using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ShareDeployed.Common.Models;

namespace ShareDeployed.Repositories
{
	public interface IMessangerRepository : IDisposable
	{
		IQueryable<MessangerGroup> Groups { get; }
		IQueryable<MessangerUser> Users { get; }
		IQueryable<User> SharedUsers { get; }
		IQueryable<MessangerApplication> Application { get; }
		IQueryable<MessageResponse> Response { get; }

		IQueryable<MessangerUser> GetOnlineUsers(MessangerGroup room);

		IQueryable<MessangerUser> SearchUsers(string name);
		IQueryable<Message> GetMessagesByGroup(MessangerGroup room);
		IQueryable<Message> GetPreviousMessages(string messageId);
		IQueryable<MessangerGroup> GetAllowedGroups(MessangerUser user);
		IQueryable<Message> GetAllNewMessges();

		Message GetMessagesById(string id);

		MessangerUser GetUserById(string userId);
		MessangerGroup GetGroupByName(string roomName);

		MessangerUser GetUserByName(string userName);
		MessangerUser GetUserByClientId(string clientId);
		MessangerUser GetUserByIdentity(string userIdentity);

		MessangerClient GetClientById(string clientId, bool includeUser = false);

		MessangerApplication GetApplicationByAppId(string appId);

		void AddUserGroup(MessangerUser user, MessangerGroup room);
		void RemoveUserGroup(MessangerUser user, MessangerGroup room);

		void Add(MessangerClient client);
		void Add(Message message);
		void Add(MessangerGroup room);
		void Add(MessangerUser user);
		void Add(MessangerApplication app);
		void Add(MessageResponse response);

		void Update(MessangerApplication application);
		void Update(Message message);
		void Update(MessageResponse response);

		void Remove(MessangerClient client);
		void Remove(MessangerGroup room);
		void Remove(MessangerUser user);
		void Remove(Message message);
		void RemoveAllClients();
		void CommitChanges();

		bool IsUserInGroup(MessangerUser user, MessangerGroup room);

		bool AuthenticateUserShared(string uid, string pass, out int userId);

		System.Data.Entity.DbSet<T> GetDbSet<T>() where T:class;
	}
}