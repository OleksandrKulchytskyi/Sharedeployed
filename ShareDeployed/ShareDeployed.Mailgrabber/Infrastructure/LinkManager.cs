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
		private readonly System.Collections.Concurrent.ConcurrentBag<Model.OutlookToServerLink> _container;

		private LinkManager()
		{
			_container = new ConcurrentBag<OutlookToServerLink>();
		}

		public void Add(string entryId, int Key, string msgId = null)
		{
			_container.Add(new OutlookToServerLink() { EntryId = entryId, Key = Key, MsgId = msgId });
		}

		public IEnumerable<OutlookToServerLink> GetAll()
		{
			return _container.AsEnumerable();
		}

		public OutlookToServerLink GetByMsgKey(int key)
		{
			return _container.FirstOrDefault(x => x.Key == key);
		}

		public void SaveToFile(string fname)
		{
			if (File.Exists(fname))
				File.Delete(fname);

			using (StreamWriter sw = new StreamWriter(fname))
			{
				sw.WriteLine(string.Format("EntryId, Key, MsgId"));
				foreach (var item in _container)
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

					_container.Add(new OutlookToServerLink() { EntryId = data[0], Key = int.Parse(data[1]), MsgId = data[2] });
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

				_container.Add(new OutlookToServerLink() { EntryId = data[0], Key = int.Parse(data[1]), MsgId = data[2] });
			},
			ex => MessageBox.Show(ex.Message),
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
