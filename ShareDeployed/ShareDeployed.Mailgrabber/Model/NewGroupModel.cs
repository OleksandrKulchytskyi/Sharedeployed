using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Model
{
	public class NewGroupModel : ObservableObject
	{
		private string _groupName;
		public string GroupName
		{
			get { return _groupName; }
			set { _groupName = value; RaisePropertyChanged(() => GroupName); }
		}

		private int _creatorKey;
		public int CreatorKey
		{
			get { return _creatorKey; }
			set { _creatorKey = value; RaisePropertyChanged(() => CreatorKey); }
		}

		private string _creatorName;
		public string CreatorName
		{
			get { return _creatorName; }
			set { _creatorName = value; RaisePropertyChanged(() => CreatorName); }
		}

		private string _creatorIdentity;
		public string CreatorIdentity
		{
			get { return _creatorIdentity; }
			set { _creatorIdentity = value; RaisePropertyChanged(() => CreatorIdentity); }
		}

		private bool _private;

		public bool Private
		{
			get { return _private; }
			set { _private = value; RaisePropertyChanged(() => Private); }
		}

		private bool _AddUsers;
		public bool AddUsers
		{
			get { return _AddUsers; }
			set { _AddUsers = value; RaisePropertyChanged(() => AddUsers); }
		}

		ObservableCollection<Common.Models.MessangerUser> _users;
		public ObservableCollection<Common.Models.MessangerUser> Users
		{
			get { return _users; }
			set { _users = value; RaisePropertyChanged(() => Users); }
		}
	}
}
