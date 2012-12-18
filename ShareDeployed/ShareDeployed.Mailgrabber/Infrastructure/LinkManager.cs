using ShareDeployed.Common.Extensions;
using ShareDeployed.Mailgrabber.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Windows;

namespace ShareDeployed.Mailgrabber.Infrastructure
{
	internal class LinkManager : SingletonBase<LinkManager>
	{
		private readonly System.Collections.Concurrent.ConcurrentDictionary<int, Model.OutlookToServerLink> _container;

		private LinkManager()
		{
			_container = new ConcurrentDictionary<int, OutlookToServerLink>();
		}

		public void Add(string entryId, int Key, string msgId = null)
		{
			if (!_container.ContainsKey(Key))
				_container.TryAdd(Key, new OutlookToServerLink() { EntryId = entryId, Key = Key, MsgId = msgId });
		}

		public void Remove(int msgKey)
		{
			OutlookToServerLink link;
			if (_container.ContainsKey(msgKey))
				_container.TryRemove(msgKey, out link);
		}

		public IEnumerable<OutlookToServerLink> GetAll()
		{
			return _container.Values.AsEnumerable();
		}

		public OutlookToServerLink GetByMsgKey(int key)
		{
			return _container[key];
		}

		public void SaveToFile(string fname)
		{
			if (File.Exists(fname))
				File.Delete(fname);

			using (StreamWriter sw = new StreamWriter(fname))
			{
				sw.WriteLine(string.Format("EntryId, Key, MsgId"));
				foreach (var item in _container.Values)
				{
					sw.WriteLine(string.Format("{0},{1},{2}", item.EntryId, item.Key, item.MsgId == null ? string.Empty : item.MsgId));
				}
				sw.Flush();
			}
		}

		[Obsolete("For better performance use LoadFromFileAsync, especially in case of large link files")]
		internal void LoadFromFile(string fname)
		{
			if (!File.Exists(fname))
				return;

			using (StreamReader sr = new StreamReader(fname))
			{
				bool isFirstLine = true;
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					if (isFirstLine)
					{
						isFirstLine = false;
						continue;
					}
					string[] data = line.Split(',');

					int key = int.Parse(data[1]);
					if (!_container.ContainsKey(key))
						_container.TryAdd(key, new OutlookToServerLink() { EntryId = data[0], Key = key, MsgId = data[2] });
				}
			}
		}

		internal void LoadFromFileAsync(string fname)
		{
			if (!File.Exists(fname))
				return;
			ObserveLines(fname).Skip(1).Subscribe(line =>
			{
				string[] data = line.Split(',');

				int key = int.Parse(data[1]);
				if (!_container.ContainsKey(key))
					_container.TryAdd(key, new OutlookToServerLink() { EntryId = data[0], Key = key, MsgId = data[2] });
			},
			() => MessageBox.Show("Loading complete"));
		}

		IEnumerable<string> ReadLines(Stream stream)
		{
			using (StreamReader reader = new StreamReader(stream))
			{
				while (!reader.EndOfStream)
					yield return reader.ReadLine();
			}
		}

		IEnumerable<string> ReadLines(string fname)
		{
			return ReadLines(File.OpenRead(fname));
		}

		IObservable<string> ObserveLines(string inputFile)
		{
			return ReadLines(inputFile).ToObservable(Scheduler.NewThread);
		}

		IObservable<string> ObserveLines(Stream inputStream)
		{
			return ReadLines(inputStream).ToObservable(Scheduler.NewThread);
		}
	}
}
