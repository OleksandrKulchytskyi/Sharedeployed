using ShareDeployed.Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShareDeployed.Authorization
{
	public class SessionTokenIssuer : SingletonBase<SessionTokenIssuer>, IDisposable
	{
		private volatile bool isDisposed = false;

		/// <summary>
		/// timeout in miliseconds
		/// </summary>
		private static readonly TimeSpan _sweepInterval = TimeSpan.FromMinutes(10);

		private readonly ConcurrentDictionary<SessionInfo, string> _inner;
		private readonly ConcurrentDictionary<string, string> _userData;
		private readonly CancellationTokenSource _cts;
		private readonly ShareDeployed.Common.Timers.Timer _purgeTimer;

		private SessionTokenIssuer()
		{
			_inner = new ConcurrentDictionary<SessionInfo, string>(new SessionInfoComparer());
			_userData = new ConcurrentDictionary<string, string>();
			_cts = new CancellationTokenSource();
			_cts.Token.Register(() =>
			{
				_purgeTimer.Stop();
			});

			_purgeTimer = new Common.Timers.Timer(_sweepInterval, true);
			_purgeTimer.Elapsed += _purgeTimer_Elapsed;
			_purgeTimer.Start();
		}

		private void _purgeTimer_Elapsed(object sender, EventArgs e)
		{
			Task.Factory.StartNew(PerformePurge, _cts.Token);
		}

		protected void PerformePurge()
		{
			if (_cts.IsCancellationRequested)
				return;

			var option = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };
#if DEBUG
			System.Diagnostics.Debug.WriteLine("\nProcessor count is:  " + Environment.ProcessorCount + "\n");
			System.Diagnostics.Debug.WriteLine("\nItems count: " + _inner.Count);
#endif
			try
			{
				Parallel.ForEach(_inner.Keys, option, (x, state) =>
					{
						System.Diagnostics.Debug.WriteLine("Processing item: " + x.ToString());
						if (option.CancellationToken.IsCancellationRequested)
						{
							state.Stop();
						}

						if (x.Expire < DateTime.UtcNow)
						{
							System.Diagnostics.Debug.WriteLine("Deleted. Item  expire:" + x.Expire.ToString());
							Remove(x);
						}
					});
			}
			catch (OperationCanceledException) { }
			catch (AggregateException ex) { }

			if (_cts.IsCancellationRequested)
				return;
#if DEBUG
			System.Diagnostics.Debug.WriteLine("Purge task has been finished");
#endif
			GC.Collect();
		}

		/// <summary>
		/// Set timeout for timer callback to be invoked
		/// </summary>
		/// <param name="timeout">timeout represented in miliseconds</param>
		public void SetPurgeTimeout(int timeout)
		{
			if (timeout <= 0)
				throw new ArgumentException("timeout must be greater than zero.");

			_purgeTimer.Period = timeout;
		}

		/// <summary>
		/// Set timeout for timer callback to be invoked
		/// </summary>
		/// <param name="timeout">timeout represented in miliseconds</param>
		public void SetPurgeTimeout(TimeSpan timeout)
		{
			if (timeout == null)
				throw new ArgumentNullException("timeout cannot be null.");

			_purgeTimer.Period = (int)timeout.TotalMilliseconds;
		}

		public void AddOrUpdate(SessionInfo info, string key)
		{
			CheckForDisposing();

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
			CheckForDisposing();

			if (_userData.ContainsKey(sessionKey))
				_userData.TryUpdate(sessionKey, userName, userName);
			else
				_userData.TryAdd(sessionKey, userName);
		}

		public string Get(SessionInfo info)
		{
			CheckForDisposing();

			string key = string.Empty;
			if (_inner.ContainsKey(info))
			{
				_inner.TryGetValue(info, out key);
			}
			return key;
		}

		public string Get(string sessionId)
		{
			CheckForDisposing();
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
			CheckForDisposing();

			string sesKey;
			string user;
			if (_inner.ContainsKey(info))
			{
				bool result = false;
				result = _inner.TryRemove(info, out sesKey);

				if (_userData.ContainsKey(sesKey))
				{
					_userData.TryRemove(sesKey, out user);
				}

				return result;
			}
			return false;
		}

		public bool CheckSessionToken(string sessionId, string authToken)
		{
			CheckForDisposing();

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
			CheckForDisposing();
			return _inner.Keys.Select(x => x.Session).AsEnumerable();
		}

		public void Dispose()
		{
			CheckForDisposing();

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
				if (!_cts.IsCancellationRequested)
					_cts.Cancel();

				_purgeTimer.Dispose();
				_cts.Dispose();
			}
		}

		private void CheckForDisposing()
		{
			if (isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
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