using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;

namespace ShareDeployed.Common.Security
{
	public class DummyPrincipalProvider : IProvidePrincipal
	{
		private const string Username = "username";
		private const string Password = "password";

		public IPrincipal CreatePrincipal(string username, string password)
		{
			if (username != Username || password != Password)
			{
				return null;
			}

			var identity = new GenericIdentity(Username);
			IPrincipal principal = new GenericPrincipal(identity, new[] { "User" });
			return principal;
		}
	}
}
