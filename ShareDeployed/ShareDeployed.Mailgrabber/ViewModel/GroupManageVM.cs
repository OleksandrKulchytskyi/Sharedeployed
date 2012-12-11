using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ShareDeployed.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;

namespace ShareDeployed.Mailgrabber.ViewModel
{
	public class GroupManageVM : ViewModelBase
	{
		public GroupManageVM()
			: base()
		{
		}

		ObservableCollection<MessangerGroup> _groups;
		public ObservableCollection<MessangerGroup> UserGroups
		{
			get { return _groups; }
			set { _groups = value; RaisePropertyChanged(() => UserGroups); }
		}

		private MessangerGroup _selectedGroup;
		public MessangerGroup SelectedGroup
		{
			get { return _selectedGroup; }
			set { _selectedGroup = value; RaisePropertyChanged(() => SelectedGroup); }
		}
		
		public RelayCommand DeleteGroupCommand
		{
			get { return new RelayCommand(DeleteGroup); }
		}

		void DeleteGroup()
		{
			if(SelectedGroup!=null)
			{
				if(Helpers.HttpClientHelper.DeleteSimple(ConfigurationManager.AppSettings["baseUrl"],
														string.Format("/api/messangergroup/delete?id={0}", SelectedGroup.Key)))
				{
					UserGroups.Remove(SelectedGroup);
					MessageBox.Show(App.Current.MainWindow.OwnedWindows.Cast<Window>().FirstOrDefault(),
									"Group has been successfully deleted.", "", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				else
				MessageBox.Show(App.Current.MainWindow.OwnedWindows.Cast<Window>().FirstOrDefault(),
									"Fail to delete group.", "", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		public RelayCommand MakeDefaultGroupCommand
		{
			get { return new RelayCommand(MakeDefaultGroup); }
		}

		void MakeDefaultGroup()
		{
			GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<Message.ChangeGroupMessage>(
				new Message.ChangeGroupMessage(SelectedGroup));
		}
	}
}
