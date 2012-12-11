using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareDeployed.Services
{
	public interface INotificationService
	{
		void LogOn(MessangerUser user, string clientId);
		void LogOut(MessangerUser user, string clientId);

		void OnMessageReceived(Message newMsg);
		void OnUserJoinedGroup(MessangerUser user, MessangerGroup group);
	}
}