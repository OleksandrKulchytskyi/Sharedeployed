using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class HashHelper
	{
		private static readonly int multiplier = 31;

		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return multiplier * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = multiplier * hash + arg2.GetHashCode();
				return multiplier * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = multiplier * hash + arg2.GetHashCode();
				hash = multiplier * hash + arg3.GetHashCode();
				return multiplier * hash + arg4.GetHashCode();
			}
		}

		public static int GetHashCode<T>(T[] list)
		{
			unchecked
			{
				int hash = 0;
				foreach (var item in list)
				{
					hash = multiplier * hash + item.GetHashCode();
				}
				return hash;
			}
		}

		public static int GetHashCode<T>(IEnumerable<T> list)
		{
			unchecked
			{
				int hash = 0;
				foreach (var item in list)
				{
					hash = multiplier * hash + item.GetHashCode();
				}
				return hash;
			}
		}

		/// <summary>
		/// Gets a hashcode for a collection for that the order of items 
		/// does not matter.
		/// So {1, 2, 3} and {3, 2, 1} will get same hash code.
		/// </summary>
		public static int GetHashCodeForOrderNoMatterCollection<T>(IEnumerable<T> list)
		{
			unchecked
			{
				int hash = 0;
				int count = 0;
				foreach (var item in list)
				{
					hash += item.GetHashCode();
					count++;
				}
				return multiplier * hash + count.GetHashCode();
			}
		}

		/// <summary>
		/// Alternative way to get a hashcode is to use a fluent 
		/// interface like this:<br />
		/// return 0.CombineHashCode(field1).CombineHashCode(field2).
		///     CombineHashCode(field3);
		/// </summary>
		public static int CombineHashCode<T>(this int hashCode, T arg)
		{
			unchecked
			{
				return multiplier * hashCode + arg.GetHashCode();
			}
		}
	}
}
