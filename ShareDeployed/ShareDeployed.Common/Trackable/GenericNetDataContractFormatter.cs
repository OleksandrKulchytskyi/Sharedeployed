using System.IO;
using System.Runtime.Serialization;

namespace ShareDeployed.Common.Trackable
{
	public class GenericNetDataContractFormatter : GenericFormatter<NetDataContractSerializer>
	{
	}

	public class GenericFormatter<F> : IGenericFormatter
	  where F : IFormatter, new()
	{
		private IFormatter m_Formatter = new F();

		#region IGenericFormatter Members

		public T Deserialize<T>(Stream serializationStream)
		{
			return (T)m_Formatter.Deserialize(serializationStream);
		}

		public void Serialize<T>(Stream serializationStream, T graph)
		{
			m_Formatter.Serialize(serializationStream, graph);
		}

		#endregion IGenericFormatter Members
	}
}