using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ShareDeployed.Mailgrabber.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	public class CreateGroupVM : ViewModelBase
	{
		public CreateGroupVM()
		{
			NewGroup = new Model.NewGroupModel();
		}

		private Model.NewGroupModel _newGroup;
		public Model.NewGroupModel NewGroup
		{
			get { return _newGroup; }
			set { _newGroup = value; RaisePropertyChanged(() => NewGroup); }
		}

		private bool _isCreated;
		public bool IsCreated
		{
			get { return _isCreated; }
			set { _isCreated = value; RaisePropertyChanged(() => IsCreated); }
		}

		public RelayCommand CreateCommand
		{
			get { return new RelayCommand(ProcessNewGroup); }
		}

		void ProcessNewGroup()
		{
			if (string.IsNullOrEmpty(NewGroup.GroupName))
			{
				System.Windows.MessageBox.Show("Group name cannot be empty");
				return;
			}

			var newGroup = new Common.Models.MessangerGroup()
			{
				CreatorKey = NewGroup.CreatorKey,
				Private = NewGroup.Private,
				Closed = false,
				Name = NewGroup.GroupName,
				Topic = string.Format("Topic of {0} group", NewGroup.GroupName),
				Welcome = string.Format("Welcome to {0}", NewGroup.GroupName),
				LastNudged = DateTime.Now
			};

			string reason;
			var result = HttpClientHelper.PosteWithErrorInfo<string, Common.Models.MessangerGroup>(System.Configuration.ConfigurationManager.AppSettings["baseUrl"],
			string.Format("/api/messangergroup/PostGroupExtended?userIdentity={0}", NewGroup.CreatorIdentity), newGroup, out reason);
			if (string.IsNullOrEmpty(result))
				MessageBox.Show(reason, "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
			else
			{
				IsCreated = true;
				var workflowWind = App.Current.MainWindow.OwnedWindows.Cast<System.Windows.Window>().FirstOrDefault();
				MessageBox.Show(workflowWind, "Group has been successfully created", "", MessageBoxButton.OK, MessageBoxImage.Information);
				Task.Factory.StartNew(() =>
				{
					var newlyCreatedGroup = HttpClientHelper.GetSimple<Common.Models.MessangerGroup>(System.Configuration.ConfigurationManager.AppSettings["baseUrl"],
												string.Format("/api/messangergroup/GetByName?groupName={0}", NewGroup.GroupName));
					GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<Message.NewGroupMessage>(new Message.NewGroupMessage(newlyCreatedGroup));
				});
			}
		}
	}
}
