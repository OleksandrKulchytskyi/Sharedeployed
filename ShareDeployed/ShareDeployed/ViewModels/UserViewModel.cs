﻿using ShareDeployed.Common.Models;
using System;

namespace ShareDeployed.ViewModels
{
	public class UserViewModel
	{
		public UserViewModel(MessangerUser user)
		{
			Name = user.Name;
			Hash = user.Hash;
			Active = user.Status == (int)UserStatus.Active;
			Status = ((UserStatus)user.Status).ToString();
			Note = user.Note;
			AfkNote = user.AfkNote;
			IsAfk = user.IsAfk;
			Flag = user.Flag;
			Country = Services.MessangerService.GetCountry(user.Flag);
			LastActivity = user.LastActivity;
			IsAdmin = user.IsAdmin;
			Id = user.Id;
		}

		public string Name { get; private set; }

		public string Hash { get; private set; }

		public bool Active { get; private set; }

		public string Status { get; private set; }

		public string Note { get; private set; }

		public string AfkNote { get; private set; }

		public bool IsAfk { get; private set; }

		public string Flag { get; private set; }

		public string Country { get; private set; }

		public DateTime LastActivity { get; private set; }

		public bool IsAdmin { get; private set; }

		public string Id { get; private set; }
	}
}