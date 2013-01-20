using ShareDeployed.Common.Caching;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using ShareDeployed.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShareDeployed.Services
{
	public class MessangerService : IMessangerService
	{
		private readonly ICache _cache;
		private readonly ICryptoService _crypto;
		private readonly IMessangerRepository _repository;

		private const int NoteMaximumLength = 140;
		private const int TopicMaximumLength = 80;
		private const int WelcomeMaximumLength = 200;

		public MessangerService(ICache cache, ICryptoService crypto, IMessangerRepository repo)
		{
			_cache = cache;
			_crypto = crypto;
			_repository = repo;
		}

		// Iso reference: http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2

		#region countries mapping

		private static readonly IDictionary<string, string> CountriesMap = new Dictionary<string, string>
                                                                            {
                                                                                {"ad", "Andorra"},
                                                                                {"ae", "United Arab Emirates"},
                                                                                {"af", "Afghanistan"},
                                                                                {"ag", "Antigua and Barbuda"},
                                                                                {"ai", "Anguilla"},
                                                                                {"al", "Albania"},
                                                                                {"am", "Armenia"},
                                                                                {"ao", "Angola"},
                                                                                {"aq", "Antarctica"},
                                                                                {"ar", "Argentina"},
                                                                                {"as", "American Samoa"},
                                                                                {"at", "Austria"},
                                                                                {"au", "Australia"},
                                                                                {"aw", "Aruba"},
                                                                                {"ax", "Åland Islands"},
                                                                                {"az", "Azerbaijan"},
                                                                                {"ba", "Bosnia and Herzegovina"},
                                                                                {"bb", "Barbados"},
                                                                                {"bd", "Bangladesh"},
                                                                                {"be", "Belgium"},
                                                                                {"bf", "Burkina Faso"},
                                                                                {"bg", "Bulgaria"},
                                                                                {"bh", "Bahrain"},
                                                                                {"bi", "Burundi"},
                                                                                {"bj", "Benin"},
                                                                                {"bl", "Saint Barthélemy"},
                                                                                {"bm", "Bermuda"},
                                                                                {"bn", "Brunei Darussalam"},
                                                                                {"bo", "Bolivia"},
                                                                                {"bq","Bonaire, Sint Eustatius and Saba"},
                                                                                {"br", "Brazil"},
                                                                                {"bs", "Bahamas"},
                                                                                {"bt", "Bhutan"},
                                                                                {"bv", "Bouvet Island"},
                                                                                {"bw", "Botswana"},
                                                                                {"by", "Belarus"},
                                                                                {"bz", "Belize"},
                                                                                {"ca", "Canada"},
                                                                                {"cc", "Cocos (Keeling) Islands"},
                                                                                {"cd","Congo, the Democratic Republic of the"},
                                                                                {"cf", "Central African Republic"},
                                                                                {"cg", "Congo"},
                                                                                {"ch", "Switzerland"},
                                                                                {"ci", "Côte d'Ivoire"},
                                                                                {"ck", "Cook Islands"},
                                                                                {"cl", "Chile"},
                                                                                {"cm", "Cameroon"},
                                                                                {"cn", "China"},
                                                                                {"co", "Colombia"},
                                                                                {"cr", "Costa Rica"},
                                                                                {"cu", "Cuba"},
                                                                                {"cv", "Cape Verde"},
                                                                                {"cw", "Curaçao"},
                                                                                {"cx", "Christmas Island"},
                                                                                {"cy", "Cyprus"},
                                                                                {"cz", "Czech Republic"},
                                                                                {"de", "Germany"},
                                                                                {"dj", "Djibouti"},
                                                                                {"dk", "Denmark"},
                                                                                {"dm", "Dominica"},
                                                                                {"do", "Dominican Republic"},
                                                                                {"dz", "Algeria"},
                                                                                {"ec", "Ecuador"},
                                                                                {"ee", "Estonia"},
                                                                                {"eg", "Egypt"},
                                                                                {"eh", "Western Sahara"},
                                                                                {"er", "Eritrea"},
                                                                                {"es", "Spain"},
                                                                                {"et", "Ethiopia"},
                                                                                {"fi", "Finland"},
                                                                                {"fj", "Fiji"},
                                                                                {"fk", "Falkland Islands (Malvinas)"},
                                                                                {"fm", "Micronesia, Federated States of"},
                                                                                {"fo", "Faroe Islands"},
                                                                                {"fr", "France"},
                                                                                {"ga", "Gabon"},
                                                                                {"gb", "United Kingdom"},
                                                                                {"gd", "Grenada"},
                                                                                {"ge", "Georgia"},
                                                                                {"gf", "French Guiana"},
                                                                                {"gg", "Guernsey"},
                                                                                {"gh", "Ghana"},
                                                                                {"gi", "Gibraltar"},
                                                                                {"gl", "Greenland"},
                                                                                {"gm", "Gambia"},
                                                                                {"gn", "Guinea"},
                                                                                {"gp", "Guadeloupe"},
                                                                                {"gq", "Equatorial Guinea"},
                                                                                {"gr", "Greece"},
                                                                                {"gs","South Georgia and the South Sandwich Islands"},
                                                                                {"gt", "Guatemala"},
                                                                                {"gu", "Guam"},
                                                                                {"gw", "Guinea-Bissau"},
                                                                                {"gy", "Guyana"},
                                                                                {"hk", "Hong Kong"},
                                                                                {"hm","Heard Island and McDonald Islands"},
                                                                                {"hn", "Honduras"},
                                                                                {"hr", "Croatia"},
                                                                                {"ht", "Haiti"},
                                                                                {"hu", "Hungary"},
                                                                                {"id", "Indonesia"},
                                                                                {"ie", "Ireland"},
                                                                                {"il", "Israel"},
                                                                                {"im", "Isle of Man"},
                                                                                {"in", "India"},
                                                                                {"io", "British Indian Ocean Territory"},
                                                                                {"iq", "Iraq"},
                                                                                {"ir", "Iran, Islamic Republic of"},
                                                                                {"is", "Iceland"},
                                                                                {"it", "Italy"},
                                                                                {"je", "Jersey"},
                                                                                {"jm", "Jamaica"},
                                                                                {"jo", "Jordan"},
                                                                                {"jp", "Japan"},
                                                                                {"ke", "Kenya"},
                                                                                {"kg", "Kyrgyzstan"},
                                                                                {"kh", "Cambodia"},
                                                                                {"ki", "Kiribati"},
                                                                                {"km", "Comoros"},
                                                                                {"kn", "Saint Kitts and Nevis"},
                                                                                {"kp","Korea, Democratic People's Republic of"},
                                                                                {"kr", "Korea, Republic of"},
                                                                                {"kw", "Kuwait"},
                                                                                {"ky", "Cayman Islands"},
                                                                                {"kz", "Kazakhstan"},
                                                                                {"la","Lao People's Democratic Republic"},
                                                                                {"lb", "Lebanon"},
                                                                                {"lc", "Saint Lucia"},
                                                                                {"li", "Liechtenstein"},
                                                                                {"lk", "Sri Lanka"},
                                                                                {"lr", "Liberia"},
                                                                                {"ls", "Lesotho"},
                                                                                {"lt", "Lithuania"},
                                                                                {"lu", "Luxembourg"},
                                                                                {"lv", "Latvia"},
                                                                                {"ly", "Libya"},
                                                                                {"ma", "Morocco"},
                                                                                {"mc", "Monaco"},
                                                                                {"md", "Moldova, Republic of"},
                                                                                {"me", "Montenegro"},
                                                                                {"mf", "Saint Martin (French part)"},
                                                                                {"mg", "Madagascar"},
                                                                                {"mh", "Marshall Islands"},
                                                                                {"mk","Macedonia, the former Yugoslav Republic of"},
                                                                                {"ml", "Mali"},
                                                                                {"mm", "Myanmar"},
                                                                                {"mn", "Mongolia"},
                                                                                {"mo", "Macao"},
                                                                                {"mp", "Northern Mariana Islands"},
                                                                                {"mq", "Martinique"},
                                                                                {"mr", "Mauritania"},
                                                                                {"ms", "Montserrat"},
                                                                                {"mt", "Malta"},
                                                                                {"mu", "Mauritius"},
                                                                                {"mv", "Maldives"},
                                                                                {"mw", "Malawi"},
                                                                                {"mx", "Mexico"},
                                                                                {"my", "Malaysia"},
                                                                                {"mz", "Mozambique"},
                                                                                {"na", "Namibia"},
                                                                                {"nc", "New Caledonia"},
                                                                                {"ne", "Niger"},
                                                                                {"nf", "Norfolk Island"},
                                                                                {"ng", "Nigeria"},
                                                                                {"ni", "Nicaragua"},
                                                                                {"nl", "Netherlands"},
                                                                                {"no", "Norway"},
                                                                                {"np", "Nepal"},
                                                                                {"nr", "Nauru"},
                                                                                {"nu", "Niue"},
                                                                                {"nz", "New Zealand"},
                                                                                {"om", "Oman"},
                                                                                {"pa", "Panama"},
                                                                                {"pe", "Peru"},
                                                                                {"pf", "French Polynesia"},
                                                                                {"pg", "Papua New Guinea"},
                                                                                {"ph", "Philippines"},
                                                                                {"pk", "Pakistan"},
                                                                                {"pl", "Poland"},
                                                                                {"pm", "Saint Pierre and Miquelon"},
                                                                                {"pn", "Pitcairn"},
                                                                                {"pr", "Puerto Rico"},
                                                                                {"ps", "Palestinian Territory, Occupied"},
                                                                                {"pt", "Portugal"},
                                                                                {"pw", "Palau"},
                                                                                {"py", "Paraguay"},
                                                                                {"qa", "Qatar"},
                                                                                {"re", "Réunion"},
                                                                                {"ro", "Romania"},
                                                                                {"rs", "Serbia"},
                                                                                {"ru", "Russian Federation"},
                                                                                {"rw", "Rwanda"},
                                                                                {"sa", "Saudi Arabia"},
                                                                                {"sb", "Solomon Islands"},
                                                                                {"sc", "Seychelles"},
                                                                                {"sd", "Seychelles"},
                                                                                {"se", "Sweden"},
                                                                                {"sg", "Singapore"},
                                                                                {"sh","Saint Helena, Ascension and Tristan da Cunha"},
                                                                                {"si", "Slovenia"},
                                                                                {"sj", "Svalbard and Jan Mayen"},
                                                                                {"sk", "Slovakia"},
                                                                                {"sl", "Sierra Leone"},
                                                                                {"sm", "San Marino"},
                                                                                {"sn", "Senegal"},
                                                                                {"so", "Somalia"},
                                                                                {"sr", "Suriname"},
                                                                                {"ss", "South Sudan"},
                                                                                {"st", "Sao Tome and Principe"},
                                                                                {"sv", "El Salvador"},
                                                                                {"sx", "Sint Maarten (Dutch part)"},
                                                                                {"sy", "Syrian Arab Republic"},
                                                                                {"sz", "Swaziland"},
                                                                                {"tc", "Turks and Caicos Islands"},
                                                                                {"td", "Chad"},
                                                                                {"tf", "French Southern Territories"},
                                                                                {"tg", "Togo"},
                                                                                {"th", "Thailand"},
                                                                                {"tj", "Tajikistan"},
                                                                                {"tk", "Tokelau"},
                                                                                {"tl", "Timor-Leste"},
                                                                                {"tm", "Turkmenistan"},
                                                                                {"tn", "Tunisia"},
                                                                                {"to", "Tonga"},
                                                                                {"tr", "Turkey"},
                                                                                {"tt", "Trinidad and Tobago"},
                                                                                {"tv", "Tuvalu"},
                                                                                {"tw", "Taiwan, Province of China"},
                                                                                {"tz", "Tanzania, United Republic of"},
                                                                                {"ua", "Ukraine"},
                                                                                {"ug", "Uganda"},
                                                                                {"um","United States Minor Outlying Islands"},
                                                                                {"us", "United States"},
                                                                                {"uy", "Uruguay"},
                                                                                {"uz", "Uzbekistan"},
                                                                                {"va", "Holy See (Vatican City State)"},
                                                                                {"vc","Saint Vincent and the Grenadines"},
                                                                                {"ve","Venezuela, Bolivarian Republic of"},
                                                                                {"vg", "Virgin Islands, British"},
                                                                                {"vi", "Virgin Islands, U.S."},
                                                                                {"vn", "Viet Nam"},
                                                                                {"vu", "Vanuatu"},
                                                                                {"wf", "Wallis and Futuna"},
                                                                                {"ws", "Samoa"},
                                                                                {"ye", "Yemen"},
                                                                                {"yt", "Mayotte"},
                                                                                {"za", "South Africa"},
                                                                                {"zm", "Zambia"},
                                                                                {"zw", "Zimbabwe"}
                                                  };

		#endregion countries mapping

		#region IMessangerService

		public Common.Models.MessangerUser AddUser(string userName, string clientId, string userAgent, string password)
		{
			if (!IsValidUserName(userName))
				throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", userName));

			if (String.IsNullOrEmpty(password))
				ThrowPasswordIsRequired();

			EnsureUserNameIsAvailable(userName);

			var user = new MessangerUser
			{
				Name = userName,
				Status = (int)UserStatus.Active,
				Id = Guid.NewGuid().ToString("d"),
				Salt = _crypto.CreateSalt(),
				LastActivity = DateTime.UtcNow
			};

			ValidatePassword(password);
			user.HashedPassword = password.ToSha256(user.Salt);

			_repository.Add(user);

			AddClient(user, clientId, userAgent);

			return user;
		}

		public Common.Models.MessangerUser AddUser(string userName, string identity, string email)
		{
			if (!IsValidUserName(userName))
				throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", userName));

			// This method is used in the auth workflow. If the username is taken it will add a number to the user name.
			if (UserExists(userName))
			{
				var usersWithNameLikeMine = _repository.Users.Count(u => u.Name.StartsWith(userName));
				userName += usersWithNameLikeMine;
			}

			var user = new MessangerUser
			{
				Name = userName,
				Status = (int)UserStatus.Active,
				Email = email,
				Hash = email.ToMD5(),
				Identity = identity,
				Id = Guid.NewGuid().ToString("d"),
				LastActivity = DateTime.UtcNow
			};

			_repository.Add(user);
			_repository.CommitChanges();

			return user;
		}

		public Common.Models.MessangerClient AddClient(Common.Models.MessangerUser user, string clientId, string userAgent)
		{
			MessangerClient client = _repository.GetClientById(clientId);
			if (client != null)
				return client;

			client = new MessangerClient
			{
				Id = clientId,
				User = user,
				UserAgent = userAgent,
				LastActivity = DateTimeOffset.UtcNow
			};

			_repository.Add(client);
			_repository.CommitChanges();

			return client;
		}

		public void AuthenticateUser(string userName, string password)
		{
			MessangerUser user = _repository.VerifyUser(userName);

			if (user.HashedPassword == null)
				throw new InvalidOperationException(String.Format("The nick '{0}' is unclaimable", userName));

			if (user.HashedPassword != password.ToSha256(user.Salt))
				throw new InvalidOperationException(String.Format("Unable to claim '{0}'.", userName));

			EnsureSaltedPassword(user, password);
		}

		public void ChangeUserName(Common.Models.MessangerUser user, string newUserName)
		{
			if (!IsValidUserName(newUserName))
				throw new InvalidOperationException(String.Format("'{0}' is not a valid user name.", newUserName));

			if (user.Name.Equals(newUserName, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("That's already your username...");

			EnsureUserNameIsAvailable(newUserName);

			// Update the user name
			user.Name = newUserName;
		}

		public void ChangeUserPassword(Common.Models.MessangerUser user, string oldPassword, string newPassword)
		{
			if (user.HashedPassword != oldPassword.ToSha256(user.Salt))
				throw new InvalidOperationException("Passwords don't match.");

			ValidatePassword(newPassword);

			EnsureSaltedPassword(user, newPassword);
		}

		public void SetUserPassword(Common.Models.MessangerUser user, string password)
		{
			ValidatePassword(password);
			user.HashedPassword = password.ToSha256(user.Salt);
		}

		public void UpdateActivity(Common.Models.MessangerUser user, string clientId, string userAgent)
		{
			user.Status = (int)UserStatus.Active;
			user.LastActivity = DateTime.UtcNow;

			MessangerClient client = AddClient(user, clientId, userAgent);
			client.UserAgent = userAgent;
			client.LastActivity = DateTimeOffset.UtcNow;

			// Remove any Afk notes.
			if (user.IsAfk)
			{
				user.AfkNote = null;
				user.IsAfk = false;
			}
		}

		public string DisconnectClient(string clientId)
		{
			// Remove this client from the list of user's clients
			MessangerClient client = _repository.GetClientById(clientId, includeUser: true);

			// No client tracking this user
			if (client == null)
				return null;

			// Get the user for this client
			MessangerUser user = client.User;

			if (user != null)
			{
				user.ConnectedClients.Remove(client);

				if (!user.ConnectedClients.Any())

					// If no more clients mark the user as offline
					user.Status = (int)UserStatus.Offline;

				_repository.Remove(client);
				_repository.CommitChanges();
			}

			return user.Id;
		}

		public Common.Models.MessangerGroup AddGroup(Common.Models.MessangerUser user, string groupName)
		{
			if (groupName.Equals("Lobby", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Lobby is not a valid chat room.");
			}

			if (!IsValidGroupName(groupName))
			{
				throw new InvalidOperationException(String.Format("'{0}' is not a valid group name.", groupName));
			}

			var room = new MessangerGroup
			{
				Name = groupName,
				Creator = user
			};

			room.Owners.Add(user);

			_repository.Add(room);

			user.OwnedGroups.Add(room);

			return room;
		}

		public void JoinGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup group, string inviteCode)
		{
			// Throw if the room is private but the user isn't allowed
			if (group.Private)
			{
				// First, check if the invite code is correct
				if (!String.IsNullOrEmpty(inviteCode) && String.Equals(inviteCode, group.InviteCode, StringComparison.OrdinalIgnoreCase))

					// It is, add the user to the allowed users so that future joins will work
					group.AllowedUsers.Add(user);

				if (!IsUserAllowed(group, user))
					throw new InvalidOperationException(String.Format("Unable to join {0}. This group is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command", group.Name));
			}

			// Add this user to the room
			_repository.AddUserGroup(user, group);

			// Clear the cache
			_cache.RemoveUserInGroup(user, group);
		}

		public void LeaveGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup group)
		{
			// Update the cache
			_cache.RemoveUserInGroup(user, group);

			// Remove the user from this group
			_repository.RemoveUserGroup(user, group);
		}

		public void SetInviteCode(Common.Models.MessangerUser user, Common.Models.MessangerGroup group, string inviteCode)
		{
			EnsureOwnerOrAdmin(user, group);
			if (!group.Private)
				throw new InvalidOperationException("Only private rooms can have invite codes");

			// Set the invite code and save
			group.InviteCode = inviteCode;
			_repository.CommitChanges();
		}

		public Common.Models.Message AddMessage(Common.Models.MessangerUser user, Common.Models.MessangerGroup group, string id, string content)
		{
			var message = new Message
			{
				Id = id,
				User = user,
				Content = content,
				When = DateTimeOffset.UtcNow,
				Group = group
			};

			_repository.Add(message);

			return message;
		}

		public void AddOwner(Common.Models.MessangerUser user, Common.Models.MessangerUser targetUser, Common.Models.MessangerGroup targetGroup)
		{
			// Ensure the user is owner of the target room
			EnsureOwnerOrAdmin(targetUser, targetGroup);

			if (targetGroup.Owners.Contains(targetUser))

				// If the target user is already an owner, then throw
				throw new InvalidOperationException(String.Format("'{0}' is already an owner of '{1}'.", targetUser.Name, targetGroup.Name));

			// Make the user an owner
			targetGroup.Owners.Add(targetUser);
			targetUser.OwnedGroups.Add(targetGroup);

			if (targetGroup.Private)
			{
				if (!targetGroup.AllowedUsers.Contains(targetUser))
				{
					// If the room is private make this user allowed
					targetGroup.AllowedUsers.Add(targetUser);
					targetUser.AllowedGroups.Add(targetGroup);
				}
			}
		}

		public void RemoveOwner(Common.Models.MessangerUser user, Common.Models.MessangerUser targetUser, Common.Models.MessangerGroup targetGroup)
		{
			// must be admin OR creator
			EnsureCreatorOrAdmin(targetUser, targetGroup);

			// ensure acting user is owner
			EnsureOwnerOrAdmin(targetUser, targetGroup);

			if (!targetGroup.Owners.Contains(targetUser))

				// If the target user is not an owner, then throw
				throw new InvalidOperationException(String.Format("'{0}' is not an owner of '{1}'.", targetUser.Name, targetGroup.Name));

			// Remove user as owner of room
			targetGroup.Owners.Remove(targetUser);
			targetUser.OwnedGroups.Remove(targetGroup);
		}

		public void KickUser(Common.Models.MessangerUser user, Common.Models.MessangerUser targetUser, Common.Models.MessangerGroup targetGroup)
		{
			EnsureOwnerOrAdmin(user, targetGroup);

			if (targetUser == user)
				throw new InvalidOperationException("Why would you want to kick yourself?");

			if (!_repository.IsUserInGroup(_cache, targetUser, targetGroup))
				throw new InvalidOperationException(String.Format("'{0}' isn't in '{1}'.", targetUser.Name, targetGroup.Name));

			// only admin can kick admin
			if (!user.IsAdmin && targetUser.IsAdmin)
				throw new InvalidOperationException("You cannot kick an admin. Only admin can kick admin.");

			// If this user isn't the creator/admin AND the target user is an owner then throw
			if (targetGroup.Creator != user && targetGroup.Owners.Contains(targetUser) && !user.IsAdmin)
				throw new InvalidOperationException("Owners cannot kick other owners. Only the room creator can kick an owner.");

			LeaveGroup(targetUser, targetGroup);
		}

		public void AllowUser(Common.Models.MessangerUser user, Common.Models.MessangerUser targetUser, Common.Models.MessangerGroup targetGroup)
		{
			EnsureOwnerOrAdmin(user, targetGroup);

			if (!targetGroup.Private)
				throw new InvalidOperationException(String.Format("{0} is not a private room.", targetGroup.Name));

			if (targetUser.AllowedGroups.Contains(targetGroup))
				throw new InvalidOperationException(String.Format("{0} is already allowed for {1}.", targetUser.Name, targetGroup.Name));

			targetGroup.AllowedUsers.Add(targetUser);
			targetUser.AllowedGroups.Add(targetGroup);

			_repository.CommitChanges();
		}

		public void UnallowUser(Common.Models.MessangerUser user, Common.Models.MessangerUser targetUser, Common.Models.MessangerGroup targetGroup)
		{
			EnsureOwnerOrAdmin(user, targetGroup);

			if (targetUser == user)
				throw new InvalidOperationException("Why would you want to unallow yourself?");

			if (!targetGroup.Private)
				throw new InvalidOperationException(String.Format("{0} is not a private room.", targetGroup.Name));

			if (!targetUser.AllowedGroups.Contains(targetGroup))
				throw new InvalidOperationException(String.Format("{0} isn't allowed to access {1}.", targetUser.Name, targetGroup.Name));

			// only admin can unallow admin
			if (!user.IsAdmin && targetUser.IsAdmin)
				throw new InvalidOperationException("You cannot unallow an admin. Only admin can unallow admin.");

			// If this user isn't the creator and the target user is an owner then throw
			if (targetGroup.Creator != user && targetGroup.Owners.Contains(targetUser) && !user.IsAdmin)
				throw new InvalidOperationException("Owners cannot unallow other owners. Only the room creator can unallow an owner.");

			targetGroup.AllowedUsers.Remove(targetUser);
			targetUser.AllowedGroups.Remove(targetGroup);

			// Make the user leave the room
			LeaveGroup(targetUser, targetGroup);
			_repository.CommitChanges();
		}

		public void LockGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup targetGroup)
		{
			EnsureOwnerOrAdmin(user, targetGroup);

			if (targetGroup.Private)
				throw new InvalidOperationException(String.Format("{0} is already locked.", targetGroup.Name));

			targetGroup.Private = true;// Make the room private
			targetGroup.AllowedUsers.Add(user);// Add the creator to the allowed list
			user.AllowedGroups.Add(targetGroup);// Add the room to the users' list

			// Make all users in the current room allowed
			foreach (var u in targetGroup.Users.Online())
			{
				u.AllowedGroups.Add(targetGroup);
				targetGroup.AllowedUsers.Add(u);
			}

			_repository.CommitChanges();
		}

		public void CloseGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup targetGroup)
		{
			GenericOpenOrCloseGroup(user, targetGroup, true);
		}

		public void OpenGroup(Common.Models.MessangerUser user, Common.Models.MessangerGroup targetGroup)
		{
			GenericOpenOrCloseGroup(user, targetGroup, false);
		}

		private void GenericOpenOrCloseGroup(MessangerUser user, MessangerGroup targetGroup, bool close)
		{
			EnsureOwnerOrAdmin(user, targetGroup);

			if (!close && !targetGroup.Closed)
				throw new InvalidOperationException(string.Format("{0} is already open.", targetGroup.Name));

			else if (close && targetGroup.Closed)
				throw new InvalidOperationException(String.Format("{0} is already closed.", targetGroup.Name));

			targetGroup.Closed = close;
			_repository.CommitChanges();
		}

		public void ChangeTopic(Common.Models.MessangerUser user, Common.Models.MessangerGroup group, string newTopic)
		{
		}

		public void ChangeWelcome(Common.Models.MessangerUser user, Common.Models.MessangerGroup group, string newWelcome)
		{
		}

		public void AppendMessage(string id, string content)
		{
		}

		public void AddAdmin(Common.Models.MessangerUser admin, Common.Models.MessangerUser targetUser)
		{
			GenericAddRemoveAdmin(admin, targetUser, true);
		}

		public void RemoveAdmin(Common.Models.MessangerUser admin, Common.Models.MessangerUser targetUser)
		{
			GenericAddRemoveAdmin(admin, targetUser, false);
		}

		private void GenericAddRemoveAdmin(Common.Models.MessangerUser admin, Common.Models.MessangerUser targetUser, bool isAdmin)
		{
			EnsureAdmin(admin);

			if (isAdmin && targetUser.IsAdmin)// If the target user is already an admin, then throw
				throw new InvalidOperationException(String.Format("'{0}' is already an admin.", targetUser.Name));

			if (!isAdmin && !targetUser.IsAdmin)// If the target user is NOT an admin, then throw
				throw new InvalidOperationException(String.Format("'{0}' is not an admin.", targetUser.Name));

			targetUser.IsAdmin = isAdmin;
			_repository.CommitChanges();
		}

		public void BanUser(Common.Models.MessangerUser callingUser, Common.Models.MessangerUser targetUser)
		{
			EnsureAdmin(callingUser);

			if (targetUser.IsAdmin)
				throw new InvalidOperationException("You cannot ban another Admin.");

			targetUser.IsBanned = true;
			_repository.CommitChanges();
		}

		private void EnsureUserNameIsAvailable(string userName)
		{
			if (UserExists(userName))
			{
				ThrowUserExists(userName);
			}
		}

		private void EnsureSaltedPassword(MessangerUser user, string password)
		{
			if (String.IsNullOrEmpty(user.Salt))
			{
				user.Salt = _crypto.CreateSalt();
			}
			user.HashedPassword = password.ToSha256(user.Salt);
		}

		private bool IsUserAllowed(MessangerGroup group, MessangerUser user)
		{
			return group.AllowedUsers.Contains(user) || user.IsAdmin;
		}

		#endregion IMessangerService

		internal static void ThrowUserExists(string userName)
		{
			throw new InvalidOperationException(String.Format("Username {0} already taken, please pick a new one using '/nick nickname'.", userName));
		}

		internal static string GetCountry(string isoCode)
		{
			if (String.IsNullOrEmpty(isoCode))
			{
				return null;
			}

			string country;
			return CountriesMap.TryGetValue(isoCode, out country) ? country : null;
		}

		internal static string NormalizeUserName(string userName)
		{
			return userName.StartsWith("@") ? userName.Substring(1) : userName;
		}

		internal static string NormalizeGroupName(string groupName)
		{
			return groupName.StartsWith("#") ? groupName.Substring(1) : groupName;
		}

		internal static void ThrowPasswordIsRequired()
		{
			throw new InvalidOperationException("A password is required.");
		}

		private bool UserExists(string userName)
		{
			return _repository.Users.Any(u => u.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsValidUserName(string name)
		{
			return !String.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[\\w-_.]{1,30}$");
		}

		private static bool IsValidGroupName(string name)
		{
			return !String.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[\\w-_]{1,30}$");
		}

		private static void EnsureAdmin(MessangerUser user)
		{
			if (!user.IsAdmin)
			{
				throw new InvalidOperationException("You are not an admin");
			}
		}

		private static void EnsureCreatorOrAdmin(MessangerUser user, MessangerGroup group)
		{
			if (user != group.Creator && !user.IsAdmin)
			{
				throw new InvalidOperationException("You are not the creator of room '" + group.Name + "'");
			}
		}

		private static void EnsureOwnerOrAdmin(MessangerUser user, MessangerGroup room)
		{
			if (!room.Owners.Contains(user) && !user.IsAdmin)
			{
				throw new InvalidOperationException("You are not an owner of room '" + room.Name + "'");
			}
		}

		private static void ValidatePassword(string password)
		{
			if (String.IsNullOrEmpty(password) || password.Length < 6)
			{
				throw new InvalidOperationException("Your password must be at least 6 characters.");
			}
		}
	}
}