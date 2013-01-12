using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareDeployed.Authorization;
using System.Threading;
using System.Threading.Tasks;

namespace ShareDeployed.Test
{
	[TestClass]
	public class SessionIssuerTest
	{
		[TestMethod]
		public void TestSessionIssuerMethod()
		{
			//SessionTokenIssuer.Instance.SetPurgeTimeout(2000);
			SessionTokenIssuer.Instance.SetPurgeTimeout(new TimeSpan(0,0,2));

			var session1 = new SessionInfo { Expire = DateTime.UtcNow.AddMinutes(1), Session = Guid.NewGuid().ToString() };
			SessionTokenIssuer.Instance.AddOrUpdate(session1, Guid.NewGuid().ToString());
			var data = SessionTokenIssuer.Instance.Get(session1);
			Assert.IsFalse(string.IsNullOrEmpty(data));
			Assert.IsTrue(SessionTokenIssuer.Instance.Remove(session1));

			Task.Factory.StartNew(Producer);

			Thread.Sleep(65000);

			Assert.IsTrue(SessionTokenIssuer.Instance.Count == 0);

			Assert.IsTrue(SessionTokenIssuer.Instance.CountUser == 0);

			SessionTokenIssuer.Instance.Dispose();

			SessionTokenIssuer.Instance.Dispose();
		}

		private void Producer()
		{
			Random r = new Random();

			for (int i = 0; i < 800; i++)
			{
				int val = r.Next(1000, 19000);
				System.Diagnostics.Debug.WriteLine("Random value is:" + val);
				SessionTokenIssuer.Instance.AddOrUpdate(new SessionInfo { Expire = DateTime.UtcNow.AddMilliseconds(val), Session = Guid.NewGuid().ToString() },
					Guid.NewGuid().ToString());

				Thread.Sleep(43);
			}
		}

		[TestMethod]
		public void TestAuthTokenExMethod()
		{
			AuthTokenManagerEx.Instance.SetPurgeTimeout(new TimeSpan(0, 0, 2));
			Thread.Sleep(5000);
			using (AuthTokenManagerEx.Instance)
			{
				
				Assert.IsTrue(AuthTokenManagerEx.Instance.Count == 0);
				Task.Factory.StartNew(Producer2);
				Thread.Sleep(1000);

				Assert.IsTrue(AuthTokenManagerEx.Instance.Count > 0);

				Thread.Sleep(60000);

				Assert.IsTrue(AuthTokenManagerEx.Instance.Count > 0);
			}

			AuthTokenManagerEx.Instance.Dispose();
		}

		void Producer2()
		{
			for (int i = 1; i <= 1000; i++)
			{
				AuthTokenManagerEx.Instance.Generate("127.0.0." + i, "Test" + i, 0);

				Thread.Sleep(56);
			}
		}
	}
}
