using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using ShareDeployed.Common.Extensions;
using ShareDeployed.Common.Models;
using ShareDeployed.Mailgrabber.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	public sealed class MainViewModel : ViewModelBase, IDisposable
	{
		bool _disposed = false;
		private System.Windows.Forms.NotifyIcon m_notifyIcon = null;
		private WindowState m_storedWindowState = WindowState.Normal;
		Task _outlookInitTask = null;
		Task _afterloginTask = null;
		CancellationTokenSource _cts = null;
		Common.Outlook.OutlookManager _outlookManager = null;

		#region Properties
		private bool _isLogged;
		public bool IsLogged
		{
			get { return _isLogged; }
			set { _isLogged = value; base.RaisePropertyChanged(() => IsLogged); }
		}

		private Model.LoginModel _login;
		public Model.LoginModel LoginData
		{
			get { return _login; }
			set { _login = value; base.RaisePropertyChanged(() => LoginData); }
		}

		private Model.LoginResult _loginResult;
		public Model.LoginResult LoginResult
		{
			get { return _loginResult; }
			set { _loginResult = value; base.RaisePropertyChanged(() => LoginResult); }
		}

		private ObservableCollection<Common.Models.Message> _mails;
		public ObservableCollection<Common.Models.Message> NewMails
		{
			get { return _mails; }
			set
			{
				_mails = value;
				RaisePropertyChanged(() => NewMails);
			}
		}

		private MessangerUser _loggedUser;
		public MessangerUser LoggedUser
		{
			get { return _loggedUser; }
			set { _loggedUser = value; RaisePropertyChanged(() => LoggedUser); }
		}

		private ObservableCollection<MessangerGroup> _userGroups;
		public ObservableCollection<MessangerGroup> UserGroups
		{
			get { return _userGroups; }
			set { _userGroups = value; RaisePropertyChanged(() => UserGroups); }
		}

		private MessangerGroup _groupToSend;
		public MessangerGroup GroupToSend
		{
			get { return _groupToSend; }
			set { _groupToSend = value; RaisePropertyChanged(() => GroupToSend); }
		}

		private bool _enableGroups;
		public bool EnableGroupsMenu
		{
			get { return _enableGroups; }
			set { _enableGroups = value; RaisePropertyChanged(() => EnableGroupsMenu); }
		}

		#endregion
		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel()
		{
			GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
			EnableGroupsMenu = false;
			NewMails = new System.Collections.ObjectModel.ObservableCollection<Common.Models.Message>();
			Messagesinitializer();

			InitializeIconTray();
			_cts = new CancellationTokenSource();
			_outlookInitTask = new Task(new Action(ProcessNewMails), _cts.Token, TaskCreationOptions.LongRunning);
			_afterloginTask = new Task(InitializeLogdUser);
		}

		private void Messagesinitializer()
		{
			LocalStateContainer.LoginMessenger.Register<Message.LoginMessage>(this, data => LoginPerformed(data));
			LocalStateContainer.LoginMessenger.Register<Message.LoginResultMessage>(this, data => ProcessLoginResult(data));
			LocalStateContainer.LoginMessenger.Register<Message.NotAuthorizedMessage>(this, data => ProcessNonAuth(data));
			GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<Message.NewGroupMessage>(this, OnNewGroup);
			GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<Message.ChangeGroupMessage>(this, OnDefaultChanging);

			Messenger.Default.Register<DialogMessage>(this, "msg1", msg =>
			{
				var result = MessageBox.Show(msg.Content, msg.Caption, msg.Button);
				// Send callback
				msg.ProcessCallback(result);
			});
		}

		void ProcessNewMails()
		{
			bool isHandlerRegistered = false;
			try
			{
				_outlookManager = new Common.Outlook.OutlookManager();
				_outlookManager.SetFoldersToMonitor(System.Configuration.ConfigurationManager.AppSettings["MAPIFolders"].Split(
													new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

				_outlookManager.MailReceived += outlookManager_MailReceived;
				isHandlerRegistered = true;
			}
			catch (Exception)
			{
				if (_outlookManager != null)
				{
					if (isHandlerRegistered)
						_outlookManager.MailReceived -= outlookManager_MailReceived;
					_outlookManager.Dispose();
					_outlookManager = null;
				}
			}
			//bool isFirst = true;
			//while (!_cts.IsCancellationRequested)
			//{
			//	if (isFirst)
			//	{
			//		isFirst = false;
			//		Thread.Sleep(TimeSpan.FromSeconds(5));
			//	}

			//	_outlookManager.SimulateMailRecieving();
			//	Thread.Sleep(TimeSpan.FromSeconds(10));
			//}
		}

		#region broadcasted message handler
		private void LoginPerformed(Message.LoginMessage loginMsg)
		{
			if (loginMsg != null)
			{
				LoginData = loginMsg.Login;
				IsLogged = true;
			}

			var window = App.Current.MainWindow.OwnedWindows.Cast<Window>().FirstOrDefault();
			if (window != null)
				window.Close();
		}

		private void ProcessLoginResult(Message.LoginResultMessage message)
		{
			if (message != null && message.LoginResult != null)
			{
				LoginResult = message.LoginResult;
				if (message.LoginResult.IsAuthorized && _outlookInitTask.Status == TaskStatus.Created)
					_outlookInitTask.Start();

				_afterloginTask.Start();
			}
		}

		private void InitializeLogdUser()
		{
			MessangerUser user = HttpClientHelper.GetSimple<MessangerUser>(System.Configuration.ConfigurationManager.AppSettings["baseUrl"],
																string.Format("/api/messangeruser/GetByIdentity?userIdentity={0}", LoginResult.UserIdentity));
			if (user != null)
				GalaSoft.MvvmLight.Threading.DispatcherHelper.InvokeAsync(() =>
					{
						LoggedUser = user;
						EnableGroupsMenu = true;
					});

			List<MessangerGroup> userGroups = HttpClientHelper.GetSimple<List<MessangerGroup>>(System.Configuration.ConfigurationManager.AppSettings["baseUrl"],
																string.Format("/api/messangeruser/GetUserGoups?userIdentity={0}&allGroups=1", LoginResult.UserIdentity));
			if (userGroups != null)
				GalaSoft.MvvmLight.Threading.DispatcherHelper.InvokeAsync(() =>
				{
					UserGroups = new ObservableCollection<MessangerGroup>(userGroups);
				});

			if (userGroups != null && userGroups.Count > 0)
				GalaSoft.MvvmLight.Threading.DispatcherHelper.InvokeAsync(() =>
				{
					GroupToSend = userGroups.FirstOrDefault(x => x.Name.Equals("admin", StringComparison.OrdinalIgnoreCase));
				});
		}

		private void ProcessNonAuth(Message.NotAuthorizedMessage msg)
		{
			if (!msg.IsTriedToLog)
			{
				var dialogMsg = new GalaSoft.MvvmLight.Messaging.DialogMessage("You are not authorized.\n\r Continue authorization?", res =>
				{
					if (res == MessageBoxResult.OK)
						ProcessLoginCommand.Execute(null);
					else
					{
						//App.Current.MainWindow.OwnedWindows[0].Close();
						App.Current.Shutdown(0);
					}

				});
				dialogMsg.Button = MessageBoxButton.OKCancel;
				Messenger.Default.Send(dialogMsg, "msg1");
			}
		}

		private void OnNewGroup(Message.NewGroupMessage msg)
		{
			GalaSoft.MvvmLight.Threading.DispatcherHelper.InvokeAsync(() =>
			{
				var newGroupView = App.Current.MainWindow.OwnedWindows.Cast<Window>().FirstOrDefault();
				if (newGroupView != null)
					newGroupView.Close();

				if (msg.NewGroup != null)
					UserGroups.Add(msg.NewGroup);
			});
		}

		private void OnDefaultChanging(Message.ChangeGroupMessage msg)
		{
			if (msg.DefaultGroup != null)
			{
				GroupToSend = msg.DefaultGroup;
			}
		}
		#endregion

		void outlookManager_MailReceived(object sender, Common.Outlook.NewMailReceivedEventArgs e)
		{
			if (e != null)
			{
				Task.Factory.StartNew(() =>
				{
					var message = new Common.Models.Message();
					message.From = e.FromUser;
					message.FromEmail = e.FromEmail;
					message.Content = e.Body;
					message.Subject = e.Subject;
					message.When = DateTime.Now;
					message.IsNew = true;
					message.UserKey = LoggedUser.Key;
					if (GroupToSend != null)
						message.GroupKey = GroupToSend.Key;

					GalaSoft.MvvmLight.Threading.DispatcherHelper.InvokeAsync(() =>
					{
						NewMails.Add(message);
					});

					var result = HttpClientHelper.PostSimple<string, Common.Models.Message>(System.Configuration.ConfigurationManager.AppSettings["baseUrl"],
																				"/api/messanger/postmessage/", message);

				});
			}
		}

		#region Commands declaration

		public RelayCommand ProcessLoginCommand
		{
			get { return new RelayCommand(ProcessLogin); }
		}

		private void ProcessLogin()
		{
			var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			var file = Path.Combine(path, "data.dat");
			if (File.Exists(file))
			{
				var decoded = FileHelper.DecryptFromFile(file, "key1key2key3key4");
				var data = JsonConvert.DeserializeObject<Model.LoginResult>(decoded);
				ViewModelLocator.Logger.InfoFormat("Authentication info has been successfully retrieved from secure file.\n\r Acess token is {0}", data.AuthToken);
				if (data.IsAuthorized)
				{
					WebClient client = new WebClient();
					client.Headers.Add("uid", data.UserName);
					client.Headers.Add("userId", data.UserId.ToString());
					client.Headers.Add("authToken", data.AuthToken);
					client.Headers.Add("logonType", "1");

					byte[] bytes = client.DownloadData(System.Configuration.ConfigurationManager.AppSettings["loginUrl"]);
					string response = Encoding.Default.GetString(bytes);
					if (!string.IsNullOrEmpty(response))
					{
						dynamic authObj = JsonConvert.DeserializeObject<dynamic>(response);
						if (authObj.errorId == 0)
						{
							ViewModelLocator.Logger.WarnFormat("Authentication was succeed. {0}", authObj.error);

							IsLogged = true;
							data.IsAuthorized = true;
							LoginResult = data;

							if (LoginData == null)
								this.LoginData = new Model.LoginModel();
							LoginData.LoginName = data.UserName;
							LoginData.SaveCredentials = true;
							if (_outlookInitTask.Status == TaskStatus.Created)
								_outlookInitTask.Start();
							return;
						}
					}
				}
			}

			var loginView = new View.LoginView();
			loginView.Owner = App.Current.MainWindow;
			loginView.Show();
		}

		public RelayCommand StateChangedCommand
		{
			get { return new RelayCommand(ProcessStateChanged); }
		}

		void ProcessStateChanged()
		{
			if (App.Current.MainWindow.WindowState == WindowState.Minimized)
			{
				App.Current.MainWindow.Hide();
				if (m_notifyIcon != null)
				{
					m_notifyIcon.ShowBalloonTip(2000);
					m_notifyIcon.Visible = true;
				}
			}
			else
				m_storedWindowState = App.Current.MainWindow.WindowState;
		}

		public RelayCommand IsVisibleChangedCommand
		{
			get { return new RelayCommand(ProcessIsVisibleChanged); }
		}

		void ProcessIsVisibleChanged()
		{
			CheckTrayIcon();
		}

		public RelayCommand ClosingCommand
		{
			get { return new RelayCommand(ProcessClosingEvent); }
		}

		void ProcessClosingEvent()
		{
			base.Cleanup();
		}

		public RelayCommand ExitCommand
		{
			get { return new RelayCommand(ProcessExitOperation); }
		}

		void ProcessExitOperation()
		{
			App.Current.Shutdown(0);
		}

		public RelayCommand CreateGroupCommand
		{
			get { return new RelayCommand(ProcessGroupCreation); }
		}

		void ProcessGroupCreation()
		{
			View.CreateGroupView view = new View.CreateGroupView();
			view.Owner = App.Current.MainWindow;
			view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			view.WindowStyle = WindowStyle.ToolWindow;
			(view.DataContext as ViewModel.CreateGroupVM).NewGroup.CreatorIdentity = LoginResult.UserIdentity;
			(view.DataContext as ViewModel.CreateGroupVM).NewGroup.CreatorName = LoginResult.UserName;
			(view.DataContext as ViewModel.CreateGroupVM).NewGroup.CreatorKey = LoggedUser.Key;

			view.ShowDialog();
		}

		public RelayCommand ManageGroupsCommand
		{
			get { return new RelayCommand(ManageGroups); }
		}

		void ManageGroups()
		{
			View.ManageGroupView view = new View.ManageGroupView();
			view.Owner = App.Current.MainWindow;
			view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			view.WindowStyle = WindowStyle.ToolWindow;
			(view.DataContext as ViewModel.GroupManageVM).UserGroups = UserGroups;
			(view.DataContext as ViewModel.GroupManageVM).SelectedGroup = null;

			view.ShowDialog();
		}

		#endregion

		#region icon tray methods

		void InitializeIconTray()
		{
			m_notifyIcon = new System.Windows.Forms.NotifyIcon();
			m_notifyIcon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
			m_notifyIcon.BalloonTipTitle = "Mail grabber";
			m_notifyIcon.Text = "Mail grabber";
			using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().
									GetManifestResourceStream("ShareDeployed.Mailgrabber.Text.ico"))
			{
				m_notifyIcon.Icon = new System.Drawing.Icon(stream);
			}
			m_notifyIcon.Click += new EventHandler(notifyIconClicked);
			//m_notifyIcon.Visible = true;
		}

		void notifyIconClicked(object sender, EventArgs e)
		{
			App.Current.MainWindow.Show();
			App.Current.MainWindow.WindowState = m_storedWindowState;
		}

		void CheckTrayIcon()
		{
			ShowTrayIcon(!App.Current.MainWindow.IsVisible);
		}

		void ShowTrayIcon(bool show)
		{
			if (m_notifyIcon != null)
				m_notifyIcon.Visible = show;
		}

		private void DisposeTrayIcon()
		{
			if (m_notifyIcon != null)
			{
				m_notifyIcon.Click -= new EventHandler(notifyIconClicked);
				m_notifyIcon.Visible = false;

				m_notifyIcon.Dispose();
				m_notifyIcon = null;
			}
		}

		#endregion

		public override void Cleanup()
		{
			LocalStateContainer.LoginMessenger.Unregister<Message.LoginMessage>(this);
			LocalStateContainer.LoginMessenger.Unregister<Message.LoginResultMessage>(this);
			LocalStateContainer.LoginMessenger.Unregister<Message.NotAuthorizedMessage>(this);
			Messenger.Default.Unregister<DialogMessage>(this);
			Messenger.Default.Unregister<Message.NewGroupMessage>(this);
			Messenger.Default.Unregister<Message.ChangeGroupMessage>(this);
			Messenger.Reset();

			Dispose();

			base.Cleanup();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (_cts != null)
			{
				if (!_cts.IsCancellationRequested)
					_cts.Cancel();
				_cts.Dispose();
			}
			if (_outlookInitTask != null && _outlookInitTask.Status != TaskStatus.Created)
				_outlookInitTask.Dispose();

			if (_afterloginTask != null && _afterloginTask.Status != TaskStatus.Created)
				_afterloginTask.Dispose();

			if (_outlookManager != null)
			{
				_outlookManager.MailReceived -= outlookManager_MailReceived;
				_outlookManager.Dispose();
				_outlookManager = null;
			}

			DisposeTrayIcon();

			GC.SuppressFinalize(this);

			GC.Collect();
		}
	}
}