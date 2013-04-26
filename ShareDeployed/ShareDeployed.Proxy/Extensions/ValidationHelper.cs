using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Proxy
{
	public static class ValidationHelper
	{
		/// <summary>
		/// Throw ArgumentNullException in case when object is null.
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="obj">Target objetc</param>
		/// <param name="parameterName">Name of parameter</param>
		/// <param name="message">Exception messsage</param>
		public static void ThrowIfNull<T>(this T obj,string parameterName,string message ) where T:class
		{
			if (obj == default(T))
				throw new ArgumentNullException(parameterName, message);
		}
	}
}
