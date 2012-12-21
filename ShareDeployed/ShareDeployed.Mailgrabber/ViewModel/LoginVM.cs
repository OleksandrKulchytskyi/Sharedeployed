using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShareDeployed.Mailgrabber.Model;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common.Helper;
using GalaSoft.MvvmLight.Command;
using System.Net;
using Newtonsoft.Json;
using System.Configuration;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	public class LoginVM : ViewModelBase
	{
		IDisposable subscription = null;

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
			if (LoginData == null) return;

			WebClient webClient = new WebClient();
			webClient.Headers.Add("uid", LoginData.LoginName);
			webClient.Headers.Add("pass", LoginData.Password);
			webClient.Headers.Add("logonType", "0");
			GenericWeakReference<WebClient> weakClient = new GenericWeakReference<WebClient>(webClient);
			
			var eventStream = Observable.FromEventPattern<DownloadDataCompletedEventArgs>(weakClient.Target, "DownloadDataCompleted").
				SubscribeOn(Scheduler.NewThread).Select(newData => newData.EventArgs.Result);

			subscription = eventStream.ObserveOn(System.Threading.SynchronizationContext.Current).Subscribe(OnDatareceived,
				//on error
				ex =>
				{
					System.Windows.MessageBox.Show(ex.Message);
					ViewModel.ViewModelLocator.Logger.Error(string.Empty, ex);
				});

			webClient.DownloadDataAsync(new Uri(ConfigurationManager.AppSettings["loginUrl"]));
			weakClient.Dispose();
			weakClient = null;
		}

		void OnDatareceived(byte[] data)
		{
			if (subscription != null)
			{
				subscription.Dispose();
				subscription = null;
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
