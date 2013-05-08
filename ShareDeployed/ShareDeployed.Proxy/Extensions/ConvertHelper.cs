using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShareDeployed.Proxy
{
	public static class ConvertHelper
	{
		public static object CreateType(string type, string value)
		{
			switch(type)
			{
				case "int":
					return Convert.ToInt32(value);

				case "long":
					return Convert.ToInt64(value);

				case "double":
					return Convert.ToDouble(value);

				case "string":
					return value;

				default :
					return Type.GetType(type).GetDefaultValue();
			}
		}
	}
}
