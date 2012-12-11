using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Mailgrabber.Model
{
	public class LoginModel : GalaSoft.MvvmLight.ObservableObject
	{
		private string _loginName;
		public string LoginName
		{
			get { return _loginName; }
			set { _loginName = value; base.RaisePropertyChanged(() => LoginName); }
		}

		private string _password;
		public string Password
		{
			get { return _password; }
			set { _password = value; base.RaisePropertyChanged(() => Password); }
		}

		private bool _save;
		public bool SaveCredentials
		{
			get { return _save; }
			set { _save = value; base.RaisePropertyChanged(() => SaveCredentials); }
		}

	}
}
