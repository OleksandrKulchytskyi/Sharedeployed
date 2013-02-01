using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Model
{
	public class LoginResult : ObservableObject
	{
		private string _authToken;
		public string AuthToken
		{
			get { return _authToken; }
			set { _authToken = value; RaisePropertyChanged(() => AuthToken); }
		}

		private int _userId;
		public int UserId
		{
			get { return _userId; }
			set { _userId = value; RaisePropertyChanged(() => UserId); }
		}

		private string _userName;
		public string UserName
		{
			get { return _userName; }
			set { _userName = value; RaisePropertyChanged(() => UserName); }
		}

		private string _userIdentity;
		public string UserIdentity
		{
			get { return _userIdentity; }
			set { _userIdentity = value; RaisePropertyChanged(() => UserIdentity); }
		}

		private bool _isAuthorized;
		public bool IsAuthorized
		{
			get { return _isAuthorized; }
			set { _isAuthorized = value; }
		}

		public override int GetHashCode()
		{
			return ShareDeployed.Common.Extensions.HashHelper.GetHashCode(AuthToken, UserId, UserIdentity, IsAuthorized);
		}
	}
}
