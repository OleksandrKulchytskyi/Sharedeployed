using System.Collections.Generic;
using System.Web.Http;

namespace ShareDeployed.Controllers.Api
{
	[Authorize(Roles = "Admin")]
	public class UserRolesController : ApiController
	{
		private readonly Repositories.IAspUserRepository _repo;

		public UserRolesController(Repositories.IAspUserRepository repository)
		{
			_repo = repository;
		}

		public List<Models.webpages_Roles> GetAll()
		{
			return _repo.GetRoles();
		}

		public Models.webpages_Roles Get(string roleName)
		{
			return _repo.GetRole(roleName);
		}

		protected override void Dispose(bool disposing)
		{
			if (_repo != null)
				_repo.Dispose();

			base.Dispose(disposing);
		}
	}
}