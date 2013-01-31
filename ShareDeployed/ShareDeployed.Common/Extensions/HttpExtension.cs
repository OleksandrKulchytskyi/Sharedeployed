using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShareDeployed.Common.Extensions
{
	internal static class HttpExtension
	{
		public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
		{
			try
			{
				return Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
			}
			catch (Exception ex)
			{
				return TaskAsyncHelper.FromError<HttpWebResponse>(ex);
			}
		}

		public static Task<Stream> GetHttpStreamAsync(this HttpWebRequest request)
		{
			try
			{
				return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
			}
			catch (Exception ex)
			{
				return TaskAsyncHelper.FromError<Stream>(ex);
			}
		}

	}
}
