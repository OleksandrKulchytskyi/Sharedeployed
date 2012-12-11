using ShareDeployed.Common.Caching;
using ShareDeployed.Common.Models;
using ShareDeployed.Repositories;
using ShareDeployed.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace ShareDeployed.Extension
{
	public static class RepositoryExtensions
	{
		public static IQueryable<MessangerUser> Online(this IQueryable<MessangerUser> source)
		{
			return source.Where(u => u.Status != (int)UserStatus.Offline);
		}

		public static IEnumerable<MessangerUser> Online(this IEnumerable<MessangerUser> source)
		{
			return source.Where(u => u.Status != (int)UserStatus.Offline);
		}

		public static IEnumerable<MessangerGroup> Allowed(this IEnumerable<MessangerGroup> rooms, string userId)
		{
			return from r in rooms
				   where !r.Private || r.Private && r.AllowedUsers.Any(u => u.Id == userId)
				   select r;
		}

		public static MessangerGroup VerifyUserGroup(this IMessangerRepository repository, ICache cache, MessangerUser user, string groupName)
		{
			if (String.IsNullOrEmpty(groupName))
			{
				throw new InvalidOperationException("Use '/join room' to join a room.");
			}

			groupName = MessangerService.NormalizeGroupName(groupName);

			MessangerGroup room = repository.GetGroupByName(groupName);

			if (room == null)
			{
				throw new InvalidOperationException(String.Format("You're in '{0}' but it doesn't exist.", groupName));
			}

			if (!repository.IsUserInGroup(cache, user, room))
			{
				throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", groupName));
			}

			return room;
		}

		public static bool IsUserInGroup(this IMessangerRepository repository, ICache cache, MessangerUser user, MessangerGroup group)
		{
			bool? cached = cache.IsUserInGroup(user, group);

			if (cached == null)
			{
				//TODO: resolve issue here
				cached = repository.IsUserInGroup(user, group);
				cache.SetUserInGroup(user, group, cached.Value);
			}

			return cached.Value;
		}

		public static MessangerUser VerifyUserId(this IMessangerRepository repository, string userId)
		{
			MessangerUser user = repository.GetUserById(userId);

			if (user == null)
				// The user isn't logged in 
				throw new InvalidOperationException("You're not logged in.");

			return user;
		}

		public static MessangerGroup VerifyGroup(this IMessangerRepository repository, string roomName, bool mustBeOpen = true)
		{
			if (String.IsNullOrWhiteSpace(roomName))
			{
				throw new InvalidOperationException("Room name cannot be blank!");
			}

			roomName = MessangerService.NormalizeGroupName(roomName);

			var room = repository.GetGroupByName(roomName);

			if (room == null)
			{
				throw new InvalidOperationException(String.Format("Unable to find room '{0}'", roomName));
			}

			if (room.Closed && mustBeOpen)
			{
				throw new InvalidOperationException(String.Format("The room '{0}' is closed", roomName));
			}

			return room;
		}

		public static MessangerUser VerifyUser(this IMessangerRepository repository, string userName)
		{
			userName = MessangerService.NormalizeUserName(userName);

			MessangerUser user = repository.GetUserByName(userName);

			if (user == null)
			{
				throw new InvalidOperationException(String.Format("Unable to find user '{0}'.", userName));
			}

			return user;
		}

		public static void LoadOwnedRooms(this IMessangerRepository repository, MessangerUser user)
		{
			if (!user.IsAdmin)
				return;

			user.OwnedGroups.AsQueryable().Select(g => g.CreatorKey == user.Key);
		}

		public static IQueryable<MessangerUser> GetUserBuIdentityQueryable(this IMessangerRepository repos, Expression<Func<MessangerUser, bool>> predicate)
		{
			return repos.Users.Where(predicate);
		}

		public static void MarkMessageAsRead(this IMessangerRepository repository, string userId, string msgId)
		{
			var user = repository.Users.FirstOrDefault(x => x.Id.Equals(userId, StringComparison.OrdinalIgnoreCase));
			if (user != null)
			{
				Message message = repository.GetMessagesById(msgId);
				if (message != null)
				{
					user.ReadMessages.Add(message);
					repository.CommitChanges();
				}


			}
		}

		public static dynamic GetAllNewMessgesForUser(this IMessangerRepository repository, MessangerUser usr)
		{
			return (from item in repository.Groups.Include(g => g.Messages).ToList()
					let grp = item
					//from user in item.Users
					//where user.Key == usr.Key && item.CreatorKey == usr.Key
					from msg in item.Messages
					where msg.IsNew
					let msgs = msg
					select new { Group = grp, Messages = msg } into tuple
					group tuple by tuple.Group into g
					select new
					{
						Group = g.Key,
						Messages = g.Select(m => m.Messages).AsEnumerable()
					}).ToList();
		}
	}
}