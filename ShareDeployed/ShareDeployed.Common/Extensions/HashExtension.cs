using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static partial class HashCodeExt
	{
		public static int CombineHashCodes(params int[] hashes)
		{
			int hash = 0;

			for (int index = 0; index < hashes.Length; index++)
			{
				hash <<= 5;
				hash ^= hashes[index];
			}

			return hash;
		}

		public static int CombineHashCodes(params object[] objects)
		{
			int hash = 0;

			for (int index = 0; index < objects.Length; index++)
			{
				int entryHash = 0x61E04917; // slurped from .Net runtime internals...
				object entry = objects[index];

				if (entry != null)
				{
					object[] subObjects = entry as object[];

					if (subObjects != null)
					{
						entryHash = HashCodeExt.CombineHashCodes(subObjects);
					}
					else
					{
						entryHash = entry.GetHashCode();
					}
				}

				hash <<= 5;
				hash ^= entryHash;
			}

			return hash;
		}

		public static int CombineHashCodes(int hash1, int hash2)
		{
			return (hash1 << 5)
				   ^ hash2;
		}

		public static int CombineHashCodes(int hash1, int hash2, int hash3)
		{
			return (((hash1 << 5)
					 ^ hash2) << 5)
				   ^ hash3;
		}

		public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4)
		{
			return (((((hash1 << 5)
					   ^ hash2) << 5)
					 ^ hash3) << 5)
				   ^ hash4;
		}

		public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4, int hash5)
		{
			return (((((((hash1 << 5)
						 ^ hash2) << 5)
					   ^ hash3) << 5)
					 ^ hash4) << 5)
				   ^ hash5;
		}

		public static int CombineHashCodes(object object1, object object2)
		{
			return CombineHashCodes(object1.GetHashCode()
				, object2.GetHashCode());
		}

		public static int CombineHashCodes(object object1, object object2, object object3)
		{
			return CombineHashCodes(object1.GetHashCode()
				, object2.GetHashCode()
				, object3.GetHashCode());
		}

		public static int CombineHashCodes(object object1, object object2, object object3, object object4)
		{
			return CombineHashCodes(object1.GetHashCode()
				, object2.GetHashCode()
				, object3.GetHashCode()
				, object4.GetHashCode());
		}
	}
}
