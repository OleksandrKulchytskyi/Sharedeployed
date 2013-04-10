using System.Web;

namespace ShareDeployed.Services
{
	public interface IVirtualPathUtil
	{
		string ToAbsolute(string relativePath);
	}

	public sealed class VirtualPathUtil : IVirtualPathUtil
	{
		public string ToAbsolute(string virtualPath)
		{
			return VirtualPathUtility.ToAbsolute(virtualPath);
		}
	}
}