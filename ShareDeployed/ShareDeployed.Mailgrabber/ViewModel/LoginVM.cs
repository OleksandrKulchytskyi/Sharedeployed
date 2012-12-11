using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShareDeployed.Mailgrabber.Model;
using ShareDeployed.Common.Extensions;
using GalaSoft.MvvmLight.Command;
using System.Net;
using Newtonsoft.Json;
using System.Configuration;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	public class LoginVM : ViewModelBase
	{
		public LoginVM()
			: base()
		{
			LoginData = new LoginModel();
		}

		private bool _LoginPressed;
		public bool IsLoginButtonPressed
		{
			get { return _LoginPressed; }
			set
			{
				_LoginPressed = value;
				RaisePropertyChanged(() => IsLoginButtonPressed);
			}
		}

		private LoginModel myVar;
		public LoginModel LoginData
		{
			get { return myVar; }
			set { myVar = value; base.RaisePropertyChanged(() => LoginData); }
		}

		public RelayCommand DoLoginCommand
		{
			get { return new RelayCommand(DoLogin); }
		}

		private void DoLogin()
		{
			IsLoginButtonPressed = true;
			if (LoginData == null)
				return;
			WebClient client = new WebClient();
			client.Headers.Add("uid", LoginData.LoginName);
			client.Headers.Add("pass", LoginData.Password);
			client.Headers.Add("logonType", "0");

			byte[] data = null;
			try
			{
				data = client.DownloadData(ConfigurationManager.AppSettings["loginUrl"]);
			}
			catch(System.Net.WebException)
			{
				System.Windows.MessageBox.Show("Fail to authenticate");
				return;
			}

			string response = Encoding.Default.GetString(data);
			if (!string.IsNullOrEmpty(response))
			{
				dynamic authObj = JsonConvert.DeserializeObject<dynamic>(response);
				if (authObj.error != null)
				{
					ViewModelLocator.Logger.WarnFormat("Authentication has failed. {0}", authObj.error);

					LocalStateContainer.LoginMessenger.Send<Message.LoginResultMessage>(new Message.LoginResultMessage(
																						new LoginResult()
																						{
																							IsAuthorized = false,
																							UserName = LoginData.LoginName
																						}));
					return;
				}

				var loginResult = new LoginResult()
				{
					IsAuthorized = true,
					UserId = authObj.userId,
					AuthToken = authObj.authToken,
					UserName = LoginData.LoginName,
					UserIdentity = authObj.userIdentity
				};

				if (LoginData.SaveCredentials)
				{
					var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
					var file = System.IO.Path.Combine(path, "data.dat");
					if (System.IO.File.Exists(file))
						System.IO.File.Delete(file);
					var json = JsonConvert.SerializeObject(loginResult);

					json.EncryptToFile(file, "key1key2key3key4");
				}

				LocalStateContainer.LoginMessenger.Send<Message.LoginMessage>(new Message.LoginMessage(LoginData));
				LocalStateContainer.LoginMessenger.Send<Message.LoginResultMessage>(new Message.LoginResultMessage(loginResult));
			}
		}

		public RelayCommand ClosingCommand
		{
			get { return new RelayCommand(OnClosing); }
		}

		void OnClosing()
		{
			if (!IsLoginButtonPressed)
				LocalStateContainer.LoginMessenger.Send<Message.NotAuthorizedMessage>(new Message.NotAuthorizedMessage(false));
		}
	}
}
