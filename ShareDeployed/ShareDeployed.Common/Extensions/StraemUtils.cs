using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShareDeployed.Common.Extensions
{
	public static class StreamUtils
	{
		public static void CopyStream(this Stream input, Stream output)
		{
			byte[] buffer = new byte[8 * 1024];
			int len;
			while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, len);
			}
		}

		public static void CopyStreamToFile(this Stream input, string fileName)
		{
			using (Stream file = File.OpenWrite(fileName))
			{
				input.CopyStream(file);
			}
		}
	}
}
