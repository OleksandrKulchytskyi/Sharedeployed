using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using ShareDeployed.Common.Caching;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common.Models;
using ShareDeployed.Extension;
using ShareDeployed.Repositories;
using ShareDeployed.Services;
using ShareDeployed.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ShareDeployed.Hubs
{
	[Authorize()]
	[HubName("messangerHub")]
	public sealed class MessangerHub : Hub, INotificationService
	{
		private readonly IMessangerService _service;
		private readonly IMessangerRepository _repository;
		private readonly IAspUserRepository _aspUserRepos;
		private readonly IAppSettings _settings;
		private readonly ICache _cache;

		private static readonly Version _version = typeof(MessangerHub).Assembly.GetName().Version;
		private static readonly string _versionString = _version.ToString();

		public MessangerHub(IAppSettings settings, IMessangerService service, IMessangerRepository repository, ICache cache,
							IAspUserRepository aspUsrRepos)//IResourceProcessor resourceProcessor)
		{
			_settings = settings;

			//_resourceProcessor = resourceProcessor;
			_service = service;
			_repository = repository;
			_cache = cache;
			_aspUserRepos = aspUsrRepos;
		}

		private string UserAgent
		{
			get
			{
				if (Context.Headers != null)
				{
					return Context.Headers["User-Agent"];
				}
				return null;
			}
		}

		private bool OutOfSync
		{
			get
			{
				string version = Clients.Caller.version;
				return String.IsNullOrEmpty(version) || new Version(version) != _version;
			}
		}

		public bool Join()
		{
			SetVersion();
			ClientState clientState = GetClientState();
			MessangerUser user = _repository.GetUserById(clientState.UserId);

			// Threre's no user being tracked
			if (user == null)
				return false;

			if (!String.IsNullOrEmpty(_settings.AuthApiKey) &&
				String.IsNullOrEmpty(user.Identity))
				return false;

			_service.UpdateActivity(user, Context.ConnectionId, UserAgent);
			_repository.CommitChanges();

			OnUserInitialize(clientState, user);
			return true;
		}

		private void OnUserInitialize(ClientState clientState, MessangerUser user)
		{
			// Update the active room on the client (only if it's still a valid room)
			if (user.Groups.Any(room => room.Name.Equals(clientState.ActiveGroup, StringComparison.OrdinalIgnoreCase)))
			{
				// Update the active room on the client (only if it's still a valid room)
				Clients.Caller.activeRoom = clientState.ActiveGroup;
			}

			(this as INotificationService).LogOn(user, Context.ConnectionId);
		}

		public bool CheckStatus()
		{
			bool outOfSync = OutOfSync;

			SetVersion();

			return outOfSync;
		}

		public bool SendMsg(Message message)
		{
			bool outOfSync = OutOfSync;

			SetVersion();

			// Sanitize the content (strip and bad html out)
			message.Content = HttpUtility.HtmlEncode(message.Content);

			string id = GetUserId();

			MessangerUser user = _repository.VerifyUserId(id);
			MessangerGroup group = _repository.VerifyUserGroup(_cache, user, message.Group.Name);

			// REVIEW: Is it better to use _repository.VerifyRoom(message.Room, mustBeOpen: false) here?
			if (group.Closed)
				throw new InvalidOperationException(String.Format("You cannot post messages to '{0}'. The group is closed.", message.Group.Name));

			// Update activity *after* ensuring the user, this forces them to be active
			UpdateActivity(user, group);

			Message chatMessage = _service.AddMessage(user, group, message.Id, message.Content);

			var messageViewModel = new MessageViewModel(chatMessage);
			Clients.Group(group.Name).addMessage(messageViewModel, group.Name);

			_repository.CommitChanges();

			string clientMessageId = chatMessage.Id;

			// Update the id on the message
			chatMessage.Id = Guid.NewGuid().ToString("d");
			_repository.CommitChanges();

			return outOfSync;
		}

		public void CheckForNewMessages()
		{
			var msngUsr = _repository.GetUserByClientId(Context.ConnectionId);
			if (msngUsr == null)
				return;

			var data = _repository.GetAllNewMessgesForUser(msngUsr);
			if (data != null)
			{
				List<ViewModels.GroupMessagesVM> container = new List<GroupMessagesVM>();
				foreach (dynamic itm in data)
				{
					var containerItem = new ViewModels.GroupMessagesVM();
					containerItem.GroupName = itm.Group.Name;
					if (itm.Messages != null)
					{
						foreach (dynamic msg in itm.Messages)
						{
							containerItem.Messages.Add(new MessageViewModel(msg));
						}
					}
					container.Add(containerItem);
				}
				Clients.Caller.OnNewMessagesResponse(container);
			}
		}

		public void DeleteMessage(string msgId)
		{
			var msngUsr = _repository.GetUserByClientId(Context.ConnectionId);
			if (msngUsr == null)
				return;
			if (msngUsr.IsAdmin)
			{
				var message = _repository.GetMessagesById(msgId);
				if (message == null)
					return;

				if (message.Group != null &&
					message.Group.Creator.Key == msngUsr.Key)
				{
					_repository.Remove(message);
				}
			}
		}

		public void ResponseToMessage(string msgId, string responseText)
		{
			try
			{
				var message = _repository.GetMessagesById(msgId);
				if (message == null)
					return;

				message.Response = new MessageResponse() { ResponseText = responseText };

				_repository.Update(message);
			}
			catch (Exception ex)
			{
				MvcApplication.Logger.Error("MessangerHub.ResponseToMessage", ex);
			}
		}

		#region methods which has been overridden

		public override Task OnConnected()
		{
			if (Context.User.Identity.IsAuthenticated)
			{
				string username = Context.User.Identity.Name;
				Clients.Caller.onStatus("Connecting...").Wait();

				var aspUser = _aspUserRepos.GetByName(username);
				if (aspUser == null)
				{
					Clients.Caller.onStatus("Fail to connect, no such user. Internal server hub error").Wait();
					throw new InvalidOperationException(string.Format("Asp.Net user with specified name {0} hasn't been found", username));
				}

				string identity = string.Format("{0}_{1}", username, aspUser.UserId);
				MessangerUser user = _repository.GetUserBuIdentityQueryable(x => x.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase)).
													Include(x => x.OwnedGroups).FirstOrDefault();
				if (user != null)
				{
					if (!String.IsNullOrEmpty(user.Email) && String.IsNullOrEmpty(user.Hash))
					{
						user.Hash = user.Email.ToMD5();
						_repository.CommitChanges();
					}
				}
				else
				{
					username = username.FixUserName();
					user = _service.AddUser(username, username, aspUser.Email);
				}
				var clientId = Context.ConnectionId;
				Clients.Caller.onStatus(string.Format("User {0} has been successfully connected, id: {1}", Context.User.Identity.Name, clientId)).Wait();
			}//end if authenticated

			return base.OnConnected();
		}

		public override Task OnReconnected()
		{
			if (!Context.User.Identity.IsAuthenticated)
				return null;

			Clients.Caller.onStatus("Reconnecting...").Wait();

			string id = GetUserId();

			if (String.IsNullOrEmpty(id))
				return null;

			MessangerUser user = _repository.VerifyUserId(id);

			// Make sure this client is being tracked
			_service.AddClient(user, Context.ConnectionId, UserAgent);

			var currentStatus = (UserStatus)user.Status;
			if (currentStatus == UserStatus.Offline)
			{
				// Mark the user as inactive
				user.Status = (int)UserStatus.Inactive;
				_repository.CommitChanges();

				// If the user was offline that means they are not in the user list so we need to tell everyone the user is really in the group
				var userViewModel = new UserViewModel(user);

				foreach (var group in user.Groups)
				{
					var isOwner = user.OwnedGroups.Contains(group);

					// Tell the people in this group that you've joined
					//Clients.Group(group.Name).addUser(userViewModel, group.Name, isOwner).Wait();

					// Add the caller to the group so they receive messages
					if (Groups != null)
						Groups.Add(Context.ConnectionId, group.Name).Wait();
				}
			}

			Clients.Caller.onStatus("User has been successfully reconnected.").Wait();
			return base.OnReconnected();
		}

		public override Task OnDisconnected()
		{
			if (!Context.User.Identity.IsAuthenticated)
				return null;

			Clients.Caller.onStatus("Disconnecting...");
			DisconnectClient(Context.ConnectionId);

			return base.OnDisconnected();
		}

		protected override void Dispose(bool disposing)
		{
			if (_repository != null)
				_repository.Dispose();

			base.Dispose(disposing);
		}

		#endregion methods which has been overridden

		#region private methods

		private void SetVersion()
		{
			// Set the version on the client
			Clients.Caller.version = _versionString;
		}

		private ClientState GetClientState()
		{
			// New client state
			var jabbrState = GetCookieValue("messanger.state");

			ClientState clientState = null;

			if (String.IsNullOrEmpty(jabbrState))
				clientState = new ClientState();
			else
			{
				try
				{
					clientState = JsonConvert.DeserializeObject<ClientState>(jabbrState);
				}
				catch (Exception ex)
				{
					MvcApplication.Logger.Error("Hub -> GetClientState", ex);
				}
			}

			// Read the id from the caller if there's no cookie
			clientState.UserId = clientState.UserId ?? Clients.Caller.uid;

			return clientState;
		}

		private string GetCookieValue(string key)
		{
			var cookie = Context.RequestCookies[key];
			string value = cookie != null ? cookie.Value : null;
			return value != null ? HttpUtility.UrlDecode(value) : null;
		}

		private string GetUserId()
		{
			ClientState state = GetClientState();
			return state.UserId;
		}

		private void DisconnectClient(string clientId)
		{
			string userId = _service.DisconnectClient(clientId);

			if (string.IsNullOrEmpty(userId))
				return;

			// Query for the user to get the updated status
			MessangerUser user = _repository.GetUserById(userId);

			// There's no associated user for this client id
			if (user == null)
				return;

			// The user will be marked as offline if all clients leave
			if (user.Status == (int)UserStatus.Offline)
			{
				foreach (var group in user.Groups)
				{
					var userViewModel = new UserViewModel(user);

					//Clients.Group(group.Name).leave(userViewModel, group.Name).Wait();
					Groups.Remove(clientId, group.Name);
				}
			}
		}

		private void UpdateActivity(MessangerUser user, MessangerGroup group)
		{
			UpdateActivity(user);

			OnUpdateActivity(user, group);
		}

		private void OnUpdateActivity(MessangerUser user, MessangerGroup group)
		{
			var userViewModel = new UserViewModel(user);
			Clients.Group(group.Name).updateActivity(userViewModel, group.Name);
		}

		private void UpdateActivity(MessangerUser user)
		{
			_service.UpdateActivity(user, Context.ConnectionId, UserAgent);
			_repository.CommitChanges();
		}

		#endregion private methods

		#region INotification service

		void INotificationService.LogOn(MessangerUser user, string clientId)
		{
			if (user.OwnedGroups.Count == 0)
				user.OwnedGroups = (from item in _repository.Groups
									where item.CreatorKey == user.Key
									select item).ToList();

			//var groups = new List<ViewModels.GroupViemModel>();
			// Update the client state
			Clients.Caller.uid = user.Id;
			Clients.Caller.name = user.Name;
			Clients.Caller.hash = user.Hash;
			Clients.Caller.aspUserId = string.IsNullOrEmpty(user.Identity) ? -1 : int.Parse(user.Identity.Split('_')[1]);

			foreach (var group in user.OwnedGroups)
			{
				// Tell the people in this group that you've joined
				//Clients.Group(room.Name).addUser(userViewModel, room.Name, isOwner).Wait();

				// Add the caller to the group so they receive messages
				//TODO: check here When user navigates to differ link it adds the same client ID to existing groups....
				try
				{
					Groups.Add(clientId, group.Name).Wait();
				}
				catch (Exception ex)
				{
					if ((ex is AggregateException) &&
						!((ex as AggregateException).InnerException is System.Threading.Tasks.TaskCanceledException))
					{
						MvcApplication.Logger.Error("INotificationService.LogOn", ex.InnerException);
					}
				}

				//groups.Add(new RoomViewModel
				//{
				//	Name = room.Name,
				//	Private = room.Private,
				//	Closed = room.Closed
				//});
			}
			_service.UpdateActivity(user, clientId, UserAgent);

			// Initialize the chat with the groups the user is in
			//Clients.Caller.logOn(groups);
		}

		void INotificationService.LogOut(MessangerUser user, string clientId)
		{
			DisconnectClient(clientId);

			var groups = user.Groups.Select(g => g.Name);
			Clients.Caller.logOut(groups);
		}

		void INotificationService.OnMessageReceived(Message newMsg)
		{
			if (newMsg == null)
				throw new ArgumentNullException("newMsg");

			if (!newMsg.IsNew)
				return;

			Clients.Group(newMsg.Group.Name).newMessageArrived(newMsg);
		}

		void INotificationService.OnUserJoinedGroup(MessangerUser user, MessangerGroup group)
		{
			if (user == null)
				throw new ArgumentNullException("user");

			if (group == null)
				throw new ArgumentNullException("group");

			Clients.Group(group.Name).userJoinedGroup(user);
		}

		#endregion INotification service
	}
}