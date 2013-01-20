using ShareDeployed.Common.Models;

namespace ShareDeployed.Services
{
	public interface IMessangerService
	{
		// Users
		MessangerUser AddUser(string userName, string clientId, string userAgent, string password);

		MessangerUser AddUser(string userName, string identity, string email);

		MessangerClient AddClient(MessangerUser user, string clientId, string userAgent);

		void AuthenticateUser(string userName, string password);

		void ChangeUserName(MessangerUser user, string newUserName);

		void ChangeUserPassword(MessangerUser user, string oldPassword, string newPassword);

		void SetUserPassword(MessangerUser user, string password);

		void UpdateActivity(MessangerUser user, string clientId, string userAgent);

		string DisconnectClient(string clientId);

		// Groups
		MessangerGroup AddGroup(MessangerUser user, string groupName);

		void JoinGroup(MessangerUser user, MessangerGroup group, string inviteCode);

		void LeaveGroup(MessangerUser user, MessangerGroup group);

		void SetInviteCode(MessangerUser user, MessangerGroup group, string inviteCode);

		// Messages
		Message AddMessage(MessangerUser user, MessangerGroup group, string id, string content);

		// Owner commands
		void AddOwner(MessangerUser user, MessangerUser targetUser, MessangerGroup targetGroup);

		void RemoveOwner(MessangerUser user, MessangerUser targetUser, MessangerGroup targetGroup);

		void KickUser(MessangerUser user, MessangerUser targetUser, MessangerGroup targetGroup);

		void AllowUser(MessangerUser user, MessangerUser targetUser, MessangerGroup targetGroup);

		void UnallowUser(MessangerUser user, MessangerUser targetUser, MessangerGroup targetGroup);

		void LockGroup(MessangerUser user, MessangerGroup targetGroup);

		void CloseGroup(MessangerUser user, MessangerGroup targetGroup);

		void OpenGroup(MessangerUser user, MessangerGroup targetGroup);

		void ChangeTopic(MessangerUser user, MessangerGroup group, string newTopic);

		void ChangeWelcome(MessangerUser user, MessangerGroup group, string newWelcome);

		void AppendMessage(string id, string content);

		// Admin commands
		void AddAdmin(MessangerUser admin, MessangerUser targetUser);

		void RemoveAdmin(MessangerUser admin, MessangerUser targetUser);

		void BanUser(MessangerUser callingUser, MessangerUser targetUser);
	}
}