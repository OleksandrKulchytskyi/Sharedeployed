using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ShareDeployed.Authorization
{
	public sealed class AuthTokenManagerEx : IDisposable
	{
		bool _disposed = false;
		private volatile int waitTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
		private static readonly object _locker = null;
		private static readonly AuthTokenManagerEx _instance = null;

		private Task _threadClean;
		private CancellationTokenSource _cts = null;

		private System.Collections.Concurrent.ConcurrentDictionary<AuthClientData, AuthTokenValueEx> _container = null;
		private System.Collections.Concurrent.ConcurrentDictionary<ClientInfo, string> _clientsAccessTokens = null;

		private AuthTokenManagerEx()
		{
			_clientsAccessTokens = new System.Collections.Concurrent.ConcurrentDictionary<ClientInfo, string>(new ClientInfoComparer());
			_container = new System.Collections.Concurrent.ConcurrentDictionary<AuthClientData, AuthTokenValueEx>(new AuthClientDataComparer());
			_cts = new CancellationTokenSource();
			_threadClean = new Task(new Action(PerformPurge), _cts.Token, TaskCreationOptions.LongRunning);
			_threadClean.Start();
		}

		static AuthTokenManagerEx()
		{
			_instance = new AuthTokenManagerEx();
			_locker = new object();
		}

		public static AuthTokenManagerEx Instance
		{
			get
			{
				lock (_locker)
				{
					return _instance;
				}
			}
		}

		public int Count { get { return _container.Count; } }

		public void SetPurgeTimeout(TimeSpan timeout)
		{
			waitTimeout = (int)timeout.TotalMilliseconds;
		}

		public AuthTokenValueEx this[AuthClientData data]
		{
			get
			{
				if (_container.ContainsKey(data))
					return _container[data];
				return null;
			}

			set
			{
				_container[data] = value;
			}
		}

		public string Generate(AuthClientData session)
		{
			if (_container.ContainsKey(session) && _container[session] != null)
				return _container[session].ToString();

			AuthTokenValueEx val = new AuthTokenValueEx();
			val.ExpireOn = DateTime.UtcNow.AddMinutes(39).AddSeconds(30);
			val.GuidKey = Guid.NewGuid();

			if (!_container.TryAdd(session, val))
				throw new InvalidOperationException("Fail to authToken");

			return _container[session].GuidKey.ToString("d");
		}

		public string Generate(string ip, string clientName, int timeoutMin = 39)
		{
			if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(clientName))
				throw new ArgumentException("One of the arguments in invalid");

			AuthClientData session = new AuthClientData(ip, clientName);

			if (_container.ContainsKey(session) && _container[session] != null)
				return _container[session].ToString();

			AuthTokenValueEx val = new AuthTokenValueEx();
			val.ExpireOn = DateTime.UtcNow.AddMinutes(timeoutMin).AddSeconds(30);
			val.GuidKey = Guid.NewGuid();

			if (!_container.TryAdd(session, val))
				throw new InvalidOperationException("Fail to authToken");

			return _container[session].GuidKey.ToString();
		}

		public bool CheckIfSessionIsAuthenticated(AuthClientData session)
		{
			return _container.ContainsKey(session);
		}

		public bool CheckIfSessionIsAuthenticated(AuthClientData session, string authKey, out int userId)
		{
			userId = -1;
			if (_container.ContainsKey(session) && _container[session] != null
				&& _container[session].GuidKey.Equals(Guid.Parse(authKey)))
			{
				userId = _container[session].UserId;
				return true;
			}

			return false;
		}

		public void RemoveToken(AuthClientData session)
		{
			if (!CheckIfSessionIsAuthenticated(session))
				return;

			AuthTokenValueEx value;
			if (!_container.TryRemove(session, out value))
				throw new InvalidOperationException("Fail to remove auth data");
		}

		#region client login operations
		public void AddClientInfo(ClientInfo info, string authToken)
		{
			if (_clientsAccessTokens.ContainsKey(info))
				_clientsAccessTokens[info] = authToken;
			else
				_clientsAccessTokens.TryAdd(info, authToken);
		}

		public void RemoveClientInfo(ClientInfo info)
		{
			string token;
			if (_clientsAccessTokens.ContainsKey(info))
				_clientsAccessTokens.TryRemove(info, out token);
		}

		public bool CheckClientToken(string authToken)
		{
			return _clientsAccessTokens.Values.Any(x => x.Equals(authToken, StringComparison.OrdinalIgnoreCase));
		}

		public string this[ClientInfo cInfo]
		{
			get
			{
				if (_clientsAccessTokens.ContainsKey(cInfo))
					return _clientsAccessTokens[cInfo];
				return null;
			}

			set
			{
				_clientsAccessTokens[cInfo] = value;
			}
		}
		#endregion

		void PerformPurge()
		{

			int cycle = 0;
			int totalCycles = (waitTimeout / 20);
			while (!_disposed && _cts != null && !_cts.IsCancellationRequested)
			{
				cycle = 0;
				totalCycles = (waitTimeout / 20);
				while (cycle != totalCycles)
				{
					if (_cts.IsCancellationRequested)
						break;

					cycle++;
					Thread.Sleep(20);
				}

				if (_container.Count == 0)
					continue;

				if (_cts == null || _cts.IsCancellationRequested)
					break;

				lock (_locker)
				{
					var option = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };
					Parallel.ForEach(_container.Keys, option, (key, state) =>
					{
						if (option.CancellationToken.IsCancellationRequested)
							state.Break();

						if (_container[key].ExpireOn < DateTime.UtcNow)
						{
							AuthTokenValueEx value;
							if (_container.TryRemove(key, out value))
							{
								var cInfo = new ClientInfo() { Id = value.UserId, UserName = value.UserName };
								RemoveClientInfo(cInfo);
							}
						}
					});

					Monitor.Pulse(_locker);
				}
			}
		}

		public void StopCleaning()
		{
			if (_cts != null && !_cts.IsCancellationRequested)
				_cts.Cancel();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			StopCleaning();

			if (_threadClean != null)
			{
				_threadClean.Wait(TimeSpan.FromMilliseconds(250));
				_threadClean.Dispose();
			}

			if (_cts != null)
			{
				_cts.Dispose();
				_cts = null;
			}

			GC.SuppressFinalize(this);
			GC.Collect();
		}
	}

	public class AuthClientData
	{
		public AuthClientData()
		{
			MachineName = string.Empty;
			IpAddress = string.Empty;
		}
		public AuthClientData(string ip, string machineName)
			: base()
		{
			MachineName = machineName;
			IpAddress = ip;
		}

		public string IpAddress { get; set; }
		public string MachineName { get; set; }
	}

	public class ClientInfo
	{
		public ClientInfo()
		{
			UserName = string.Empty;
		}

		public int Id { get; set; }
		public string UserName { get; set; }
	}

	public class AuthTokenValueEx : AuthTokenValue
	{
		public AuthTokenValueEx()
			: base()
		{
			UserName = string.Empty;
		}

		public int UserId { get; set; }
		public string UserName { get; set; }
	}

	internal class AuthClientDataComparer : IEqualityComparer<AuthClientData>
	{
		public bool Equals(AuthClientData x, AuthClientData y)
		{
			if (x == null || y == null)
				return false;

			else if (object.ReferenceEquals(y, x))
				return true;

			return (x.IpAddress.Equals(y.IpAddress, StringComparison.InvariantCulture) &&
				x.MachineName.Equals(y.MachineName, StringComparison.InvariantCulture));
		}

		public int GetHashCode(AuthClientData obj)
		{
			return obj.IpAddress.Length + obj.MachineName.Length;
		}
	}

	internal class ClientInfoComparer : IEqualityComparer<ClientInfo>
	{
		public bool Equals(ClientInfo x, ClientInfo y)
		{
			if (x == null || y == null)
				return false;

			else if (object.ReferenceEquals(x, null))
				return false;

			else if (object.ReferenceEquals(y, x))
				return true;

			return (x.UserName.Equals(y.UserName, StringComparison.InvariantCulture) ||
				x.Id == y.Id);
		}

		public int GetHashCode(ClientInfo obj)
		{
			return obj.Id + obj.UserName.Length;
		}
	}

	public class AuthTokenValue : IEquatable<AuthTokenValue>
	{
		public Guid GuidKey { get; set; }
		public DateTime ExpireOn { get; set; }

		public bool Equals(AuthTokenValue other)
		{
			if (other == null)
				return false;
			else if (object.ReferenceEquals(this, other))
				return true;

			return (this.GuidKey.Equals(other.GuidKey) && this.ExpireOn.Equals(other.ExpireOn));
		}
	}

	[Serializable]
	public class AuthTokenManagerException : ApplicationException
	{
		public AuthTokenManagerException()
			: base()
		{

		}

		public AuthTokenManagerException(string msg)
			: base(msg)
		{

		}

		public AuthTokenManagerException(string msg, Exception innerExc)
			: base(msg, innerExc)
		{

		}
	}
}
