using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ShareDeployed.Common.Trackable;
using System.Collections.Generic;

namespace ShareDeployed.Test
{
	[TestClass]
	public class TrackableUnitTest
	{
		[TestMethod]
		public void TestMethodTrackableObsCol()
		{
			List<Person> personList = new List<Person>() { new Person(1, "Cawa"), new Person(2, "Alex") };
			TrackableObservableCollection<Person> data = new TrackableObservableCollection<Person>(personList);

			data.CollectionChanged += data_CollectionChanged;

			data[0] = new Person(1, "Data");

			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

			data[1].Name = "Yeepppp";

			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
		}

		void data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(e.Action.ToString() + "   " + e.NewItems == null ? "0" : e.NewItems.Count.ToString());
		}

		[TestMethod]
		public void TestMethodTrackableObj()
		{
			TrackableObject<Person> trackable = new TrackableObject<Person>(new Person(1, "1`2123"));
			trackable.OnDirtyStatusChanged += trackable_OnDirtyStatusChanged;
			trackable.Data.Name = "Kirill";
		}

		void trackable_OnDirtyStatusChanged(object sender, DirtyEventArgs<TrackableUnitTest.Person> e)
		{
			if (e.NewValue != null)
			{
				if (e.OldValue != null)
				{
					if (e.OldValue.Name.Equals(e.NewValue.Name))
						Assert.Fail();
				}
			}
		}

		[System.Runtime.Serialization.DataContract()]
		private class Person : BaseINPC
		{
			public Person()
			{
			}

			public Person(int id, string name)
			{
				Id = id; Name = name;
			}

			#region Property Id
			private int _Id;
			[System.Runtime.Serialization.DataMember]
			public int Id
			{
				get
				{
					return _Id;
				}
				set
				{
					if (_Id != value)
					{
						_Id = value;
						RaisePropertyChanged("Id");
					}
				}
			}
			#endregion

			#region Property Name
			private string _Name;
			[System.Runtime.Serialization.DataMember]
			public string Name
			{
				get
				{
					return _Name;
				}
				set
				{
					if (_Name != value)
					{
						_Name = value;
						RaisePropertyChanged("Name");
					}
				}
			}
			#endregion

		}

		[System.Runtime.Serialization.DataContract]
		public class BaseINPC : System.ComponentModel.INotifyPropertyChanged
		{
			#region INotifyPropertyChanged Members

			[field: NonSerialized()]
			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			protected void RaisePropertyChanged(string prop)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
				}
			}

			#endregion
		}
	}
}