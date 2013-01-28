using System;
using System.ComponentModel;

namespace ShareDeployed.Common.Trackable
{
	public class TrackableObject<T> : INotifyPropertyChanged
	   where T : class, INotifyPropertyChanged, new()
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion INotifyPropertyChanged

		#region Private member variables

		private bool _isDirty;
		private T _original;
		private T _current;

		#endregion Private member variables

		#region Constructor

		public TrackableObject(T objectToTrack)
		{
			if (objectToTrack == null)
			{
				throw new ArgumentNullException("objectToTrack");
			}

			_isDirty = false;
			this.Data = objectToTrack;
			_original = SerializationUtil.Clone<T>(objectToTrack);
		}

		#endregion Constructor

		#region Public properties

		public T Data
		{
			get
			{
				return _current;
			}
			private set
			{
				_current = value;
				_current.PropertyChanged += (s, e) =>
				{
					bool isDirty = (!SerializationUtil.IsEqual(_current, _original));

					if (isDirty != this.IsDirty)
					{
						this.IsDirty = isDirty;
					}
				};

				RaisePropertyChanged("Data");
			}
		}

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

				if (OnDirtyStatusChanged != null)
				{
					OnDirtyStatusChanged(this, new DirtyEventArgs<T>(_original, Data));
				}
			}
		}

		#endregion Public properties

		#region Public events

		public event EventHandler<DirtyEventArgs<T>> OnDirtyStatusChanged;

		#endregion Public events

		#region Private methods

		private void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = PropertyChanged;

			if ((propertyChanged != null))
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion Private methods
	}

	public class DirtyEventArgs<T> : EventArgs where T : class
	{
		public DirtyEventArgs()
		{
		}

		public DirtyEventArgs(T old, T newData)
		{
			OldValue = old;
			NewValue = newData;
		}

		public T OldValue { get; set; }

		public T NewValue { get; set; }
	}
}