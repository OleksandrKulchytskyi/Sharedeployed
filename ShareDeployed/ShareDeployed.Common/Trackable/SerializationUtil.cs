using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace ShareDeployed.Common.Trackable
{
	/// <summary>
	/// Serialization helper class
	/// </summary>
	public static class SerializationUtil
	{
		#region Public methods

		/// <summary>
		/// Creates a deep copy of (all serializable properties of) a class
		/// </summary>
		/// <typeparam name="T">The class of the object to copy</typeparam>
		/// <param name="source">The object to copy</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Thrown if <paramref name="source"/> is not a WCF data contract or else
		/// is not ISerializable
		/// </exception>
		public static T Clone<T>(T source)
		{
			if (!(IsDataContract(typeof(T)) || typeof(T).IsSerializable))
			{
				throw new ArgumentOutOfRangeException("source","Object to clone must be a WCF data contract or ISerializable");
			}

			IGenericFormatter formatter = new GenericNetDataContractFormatter();
			Stream stream = new MemoryStream();

			using (stream)
			{
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);

				T clone = formatter.Deserialize<T>(stream);
				return clone;
			}
		}

		/// <summary>
		/// Returns true if the data contract serializable members of two objects have equivalent values
		/// </summary>
		/// <typeparam name="T">The type of object being compared</typeparam>
		/// <param name="thing1">The first object to compare</param>
		/// <param name="thing2">The second object to compare</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Thrown if <paramref name="thing1"/> or <paramref name="thing2"/> are not WCF data contracts
		/// (or else ISerializable)
		/// </exception>
		public static bool IsEqual<T>(T thing1, T thing2)
		{
			string thing1Hash = GetHash<T>(thing1);
			string thing2Hash = GetHash<T>(thing2);

			bool isEqual = (thing1Hash == thing2Hash);

			return isEqual;
		}

		/// <summary>
		/// Returns a unique hash for a data contract serializable object
		/// </summary>
		/// <typeparam name="T">The type of object being compared</typeparam>
		/// <param name="source">The object for which the hash is required</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Thrown if <paramref name="source"/> is not a WCF data contract or else
		/// is not ISerializable
		/// </exception>
		public static string GetHash<T>(T source)
		{
			if (!(IsDataContract(typeof(T)) || typeof(T).IsSerializable))
			{
				throw new ArgumentOutOfRangeException("source",
					"Object to get has for must be a WCF data contract or ISerializable");
			}

			string hash = null;

			using (Stream stream = new MemoryStream())
			{
				IGenericFormatter formatter = new GenericNetDataContractFormatter();
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(stream))
				{
					var sourceAsString = reader.ReadToEnd();
					hash = GetMD5Hash(sourceAsString);
				}
			}

			return hash;
		}

		/// <summary>
		/// Gets whether a particular data type is serializable through WCF data contracts
		/// </summary>
		/// <param name="type">The class to test for serializable capability</param>
		public static bool IsDataContract(Type type)
		{
			object[] attributeArray = type.GetCustomAttributes(
				typeof(DataContractAttribute), true);

			if (attributeArray.Length > 0)
			{
				return true;
			}

			return false;
		}

		#endregion Public methods

		#region Private methods

		private static string GetMD5Hash(string stringToHash)
		{
			string hash = string.Empty;

			using (var md5Obj = new MD5CryptoServiceProvider())
			{
				byte[] bytesToHash = Encoding.ASCII.GetBytes(stringToHash);
				bytesToHash = md5Obj.ComputeHash(bytesToHash);

				foreach (var currentByte in bytesToHash)
				{
					hash += currentByte.ToString("x2");
				}
			}

			return hash;
		}

		#endregion Private methods
	}
}