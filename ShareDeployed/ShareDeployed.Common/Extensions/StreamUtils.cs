using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShareDeployed.Common.Extensions
{
	public static class StreamUtils
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetDiskFreeSpaceW")]
		private static extern bool GetDiskFreeSpace(string lpRootName, out int lpSectorsPerCluster, out int lpBytesPerSector,
													out int lpNiumberOfFreeClusters, out int lpTotalNumberOfClusters);

		public static int GetClusterSize(string path)
		{
			int sectorsPerCluster;
			int bytesPerSector;
			int freeClusters;
			int totalClusters;
			int clusterSize = 0;
			if (GetDiskFreeSpace(Path.GetPathRoot(path), out sectorsPerCluster, out bytesPerSector, out freeClusters, out totalClusters))
				clusterSize = bytesPerSector * sectorsPerCluster;
			return clusterSize;
		}

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

		public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
		{
			try
			{
				return Task.Factory.FromAsync((cb, state) => stream.BeginRead(buffer, 0, buffer.Length, cb, state), ar => stream.EndRead(ar), null);
			}
			catch (System.Exception ex)
			{
				return TaskAsyncHelper.FromError<int>(ex);
			}
		}

		public static Task WriteAsync(this Stream stream, byte[] buffer)
		{
			try
			{
				return Task.Factory.FromAsync((cb, state) => stream.BeginWrite(buffer, 0, buffer.Length, cb, state), ar => stream.EndWrite(ar), null);
			}
			catch (System.Exception ex)
			{
				return TaskAsyncHelper.FromError(ex);
			}
		}

		public static string FormatBytesLen(long bytes)
		{
			const int scale = 1024;
			string[] orders = { "GB", "MB", "KB", "Bytes" };
			long max = (long)System.Math.Pow(scale, orders.Length - 1);

			foreach (var order in orders)
			{
				if (bytes > max)
					return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

				max /= scale;
			}
			return "0 Bytes";
		}
	}
}