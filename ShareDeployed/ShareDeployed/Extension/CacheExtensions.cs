using ShareDeployed.Common.Caching;
using ShareDeployed.Common.Models;
using System;

namespace ShareDeployed.Extension
{
	public static class CacheExtensions
	{
		public static bool? IsUserInGroup(this ICache cache, MessangerUser user, MessangerGroup room)
		{
			string key = CacheKeys.GetUserInGroup(user, room);

			return (bool?)cache.Get(key);
		}

		public static void SetUserInGroup(this ICache cache, MessangerUser user, MessangerGroup room, bool value)
		{
			string key = CacheKeys.GetUserInGroup(user, room);

			// Cache this forever since people don't leave rooms often
			cache.Set(key, value, TimeSpan.FromDays(365));
		}

		public static void RemoveUserInGroup(this ICache cache, MessangerUser user, MessangerGroup room)
		{
			cache.Remove(CacheKeys.GetUserInGroup(user, room));
		}

		private static class CacheKeys
		{
			public static string GetUserInGroup(MessangerUser user, MessangerGroup room)
			{
				return "UserInRoom" + user.Key + "_" + room.Key;
			}
		}
	}
}