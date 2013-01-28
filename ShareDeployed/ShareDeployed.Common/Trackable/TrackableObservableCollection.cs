using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ShareDeployed.Common.Trackable
{
	public class TrackableObservableCollection<T> : ObservableCollection<T>
	   where T : class, INotifyPropertyChanged, new()
	{
		#region Private member variables

		private List<TrackableObject<T>> _trackingList;
		private bool _isDirty;

		#endregion Private member variables

		#region Constructor

		public TrackableObservableCollection(IList<T> initialCollection)
		{
			if (initialCollection == null)
			{
				throw new ArgumentNullException("initialCollection");
			}

			_trackingList = new List<TrackableObject<T>>();

			foreach (var item in initialCollection)
			{
				try
				{
					var trackableObject = new TrackableObject<T>(item);
					trackableObject.OnDirtyStatusChanged += trackableObject_OnDirtyStatusChanged;
					_trackingList.Add(trackableObject);
					this.Add(item);
				}
				catch(Exception ex)
				{
				}
			}

			_isDirty = false;
			CollectionChanged += CustomObservableCollection_CollectionChanged;
		}

		#endregion Constructor

		#region Public properties

		public bool IsDirty
		{
			get
			{
				return _isDirty;
			}
			private set
			{
				_isDirty = value;
				RaisePropertyChanged("IsDirty");
			}
		}

		#endregion Public properties

		#region Private methods

		private void trackableObject_OnDirtyStatusChanged(object sender, EventArgs e)
		{
			UpdateDirtyStatus();
		}

		private void CustomObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Remove ||
				e.Action == NotifyCollectionChangedAction.Replace ||
				e.Action == NotifyCollectionChangedAction.Reset)
			{
				var trackersToUnhook = (from t in _trackingList
										join m in this
										on t.Data equals m into cl
										from deldata in cl.DefaultIfEmpty()
										where deldata == null
										select t);

				foreach (var trackableObject in trackersToUnhook)
				{
					trackableObject.OnDirtyStatusChanged -= trackableObject_OnDirtyStatusChanged;
				}
			}

			UpdateDirtyStatus();
		}

		private void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;

			if ((propertyChanged != null))
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private void UpdateDirtyStatus()
		{
			bool isDirty = true;

			if (base.Count == _trackingList.Count)
			{
				var matchCount = (from t in _trackingList
								  join m in this
								  on t.Data equals m
								  select t).Count();

				isDirty = (matchCount != _trackingList.Count);

				if (!isDirty)
				{
					var dirtyCount = (from t in _trackingList
									  where t.IsDirty
									  select t).Count();

					isDirty = (dirtyCount != 0);
				}
			}

			this.IsDirty = isDirty;
		}

		#endregion Private methods

		#region Protected members

		protected override event PropertyChangedEventHandler PropertyChanged;

		#endregion Protected members
	}
}