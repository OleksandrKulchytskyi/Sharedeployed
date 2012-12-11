using System.Security.Principal;

namespace ShareDeployed.Common.Security
{
	public interface IProvidePrincipal
	{
		IPrincipal CreatePrincipal(string username, string password);
	}
}
