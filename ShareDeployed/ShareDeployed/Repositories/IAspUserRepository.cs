using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Repositories
{
	public interface IAspUserRepository : IDisposable
	{
		IQueryable<Models.UserProfile> UserProfiles { get; }

		bool Exist(string name);
		Models.UserProfile GetById(int id);
		Models.UserProfile GetByName(string name);

		Models.webpages_Roles GetRole(string roleName);
		List<Models.webpages_Roles> GetRoles();
	}
}
