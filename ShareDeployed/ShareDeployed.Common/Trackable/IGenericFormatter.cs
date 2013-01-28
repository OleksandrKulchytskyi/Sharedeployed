using System.IO;

namespace ShareDeployed.Common.Trackable
{
	public interface IGenericFormatter
	{
		T Deserialize<T>(Stream serializationStream);

		void Serialize<T>(Stream serializationStream, T graph);
	}
}