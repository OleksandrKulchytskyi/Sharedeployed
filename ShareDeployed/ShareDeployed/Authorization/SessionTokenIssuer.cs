using ShareDeployed.Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ShareDeployed.Authorization
{
	public class SessionTokenIssuer : SingletonBase<SessionTokenIssuer>, IDisposable
	{
		private volatile bool isDisposed = false;
		/// <summary>
		/// timeout in miliseconds
		/// </summary>
		private volatile int purgeTimeout = 15000;
		private readonly Task _purgeTask;
		private readonly ConcurrentDictionary<SessionInfo, string> _inner;
		private readonly ConcurrentDictionary<string, string> _userData;
		readonly CancellationTokenSource _cts;
		private readonly object _syncRoot = new object();

		private SessionTokenIssuer()
		{
			_inner = new ConcurrentDictionary<SessionInfo, string>(new SessionInfoComparer());
			_userData = new ConcurrentDictionary<string, string>();
			_cts = new CancellationTokenSource();
			_purgeTask = new Task(PerformePurge, _cts.Token, TaskCreationOptions.LongRunning);
			_purgeTask.Start();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timeout">timeout represented in miliseconds</param>
		public void SetPurgeTimeout(int timeout)
		{
			if (timeout <= 0)
				throw new ArgumentException("timeout must be greater than zero.");
			purgeTimeout = timeout;
		}

		public void SetPurgeTimeout(TimeSpan timeout)
		{
			if (timeout == null)
				throw new ArgumentNullException("timeout cannot be null.");
			purgeTimeout = (int)timeout.TotalMilliseconds;
		}

		private void PerformePurge()
		{
			int cycle = 0;
			var option = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };
			System.Diagnostics.Debug.WriteLine("\n Processor count is:  " + Environment.ProcessorCount + "\n");

			Thread.Sleep(500);

			while (!_cts.IsCancellationRequested)
			{
				int totalCycles = ((purgeTimeout) / 10);

				System.Diagnostics.Debug.WriteLine("!!!! \n Items count: " + _inner.Count);

				Parallel.ForEach(_inner.Keys, option, (x, state) =>
					{
						System.Diagnostics.Debug.WriteLine("Item: " + x.ToString());
						if (option.CancellationToken.IsCancellationRequested)
							state.Break();

						if (x.Expire < DateTime.UtcNow)
						{
							System.Diagnostics.Debug.WriteLine("Deleted. Item  expire:" + x.Expire.ToString());
							Remove(x);
						}
					});

				cycle = 0;
				while (cycle != totalCycles)
				{
					if (_cts.IsCancellationRequested)
						break;

					cycle++;
					Thread.Sleep(10);
				}
			}

			System.Diagnostics.Debug.WriteLine("Purge task has benn finished");
		}

		public void AddOrUpdate(SessionInfo info, string key)
		{
			if (_inner.ContainsKey(info))
			{
				info.UpdateActivity();
				_inner.TryUpdate(info, key, key);
			}
			else
			{
				_inner.TryAdd(info, key);
				_userData.TryAdd(key, string.Empty);
			}
		}

		public void AddOrUpdateUserName(string sessionKey, string userName)
		{
			if (_userData.ContainsKey(sessionKey))
				_userData.TryUpdate(sessionKey, userName, userName);
			else
				_userData.TryAdd(sessionKey, userName);
		}

		public string Get(SessionInfo info)
		{
			string key = string.Empty;
			if (_inner.ContainsKey(info))
			{
				_inner.TryGetValue(info, out key);
			}
			return key;
		}

		public string Get(string sessionId)
		{
			string key = string.Empty;
			var info = new SessionInfo { Session = sessionId };
			if (_inner.ContainsKey(info))
			{
				_inner.TryGetValue(info, out key);
			}
			return key;
		}

		public bool Remove(SessionInfo info)
		{
			string sesKey;
			string user;
			if (_inner.ContainsKey(info))
			{
				bool result = false;
				result = _inner.TryRemove(info, out sesKey);
				if (_userData.ContainsKey(sesKey))
					_userData.TryRemove(sesKey, out user);

				return result;
			}
			return false;
		}

		public bool CheckSessionToken(string sessionId, string authToken)
		{
			if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(authToken))
				return false;

			var sesInfo = new SessionInfo { Session = sessionId };
			if (_inner.ContainsKey(sesInfo))
			{
				return string.Equals(_inner[sesInfo], authToken, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public int Count
		{
			get { return _inner.Count; }
		}

		public int CountUser
		{
			get { return _userData.Count; }
		}

		public IEnumerable<string> GetAllSessions()
		{
			return _inner.Keys.Select(x => x.Session).AsEnumerable();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed)
				return;

			isDisposed = true;
			if (disposing)
			{
				_cts.Cancel();

				_purgeTask.Wait((int)TimeSpan.FromSeconds(.5).TotalMilliseconds);

				_purgeTask.Dispose();
				_cts.Dispose();
			}
		}
	}

	public class SessionInfo
	{
		public string Session { get; set; }
		public DateTime Expire { get; set; }

		public void UpdateActivity()
		{
			Expire = DateTime.UtcNow.AddMinutes(40);
		}

		public override string ToString()
		{
			return this.Session;
		}
	}

	public class SessionInfoComparer : IEqualityComparer<SessionInfo>
	{
		public bool Equals(SessionInfo x, SessionInfo y)
		{
			return (x.Session.Equals(y.Session, StringComparison.OrdinalIgnoreCase));
		}

		public int GetHashCode(SessionInfo obj)
		{
			return obj.Session.GetHashCode();
		}
	}
}