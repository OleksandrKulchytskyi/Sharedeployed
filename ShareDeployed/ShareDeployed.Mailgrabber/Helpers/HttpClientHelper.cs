using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;

namespace ShareDeployed.Mailgrabber.Helpers
{
	public static class HttpClientHelper
	{
		public static Out GetSimple<Out>(string baseUrl, string remainUrl)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.BaseAddress = new Uri(baseUrl);
					client.Timeout = TimeSpan.FromMinutes(2);
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, remainUrl))
					{
						request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

						using (HttpResponseMessage response = client.SendAsync(request).Result)
						{
							if (response.IsSuccessStatusCode)
							{
								return response.Content.ReadAsAsync<Out>().Result;
							}
							return default(Out);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mailgrabber.ViewModel.ViewModelLocator.Logger.Error("HttpClientHelper", ex);
				return default(Out);
			}
		}


		public static Out PostSimple<Out, In>(string baseUrl, string remainUrl, In input) where Out : class
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.BaseAddress = new Uri(baseUrl);
					client.Timeout = TimeSpan.FromMinutes(2);

					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, remainUrl))
					{
						request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
						MediaTypeFormatter mediaForm = new JsonMediaTypeFormatter();
						request.Content = new ObjectContent<In>(input, mediaForm);

						using (HttpResponseMessage response = client.SendAsync(request).Result)
						{
							if (response.IsSuccessStatusCode)
							{
								if (typeof(Out) == typeof(string))
									return response.Content.ReadAsStringAsync().Result as Out;
								else
									return response.Content.ReadAsAsync<Out>().Result;

							}
							return default(Out);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mailgrabber.ViewModel.ViewModelLocator.Logger.Error("HttpClientHelper", ex);
				return default(Out);
			}
		}

		public static Out PosteWithErrorInfo<Out, In>(string baseUrl, string remainUrl, In input, out string reason) where Out : class
		{
			reason = string.Empty;
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.BaseAddress = new Uri(baseUrl);
					client.Timeout = TimeSpan.FromMinutes(2);

					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, remainUrl))
					{
						request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
						MediaTypeFormatter mediaForm = new JsonMediaTypeFormatter();
						request.Content = new ObjectContent<In>(input, mediaForm);

						using (HttpResponseMessage response = client.SendAsync(request).Result)
						{
							if (response.IsSuccessStatusCode)
							{
								if (typeof(Out) == typeof(string))
									return response.Content.ReadAsStringAsync().Result as Out;
								else
									return response.Content.ReadAsAsync<Out>().Result;
							}
							else
								reason = response.ReasonPhrase;
							return default(Out);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mailgrabber.ViewModel.ViewModelLocator.Logger.Error("HttpClientHelper", ex);
				return default(Out);
			}
		}

		public static bool DeleteSimple(string baseUrl, string remainUrl)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.BaseAddress = new Uri(baseUrl);
					client.Timeout = TimeSpan.FromMinutes(2);
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, remainUrl))
					{
						request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

						using (HttpResponseMessage response = client.SendAsync(request).Result)
						{
							if (response.IsSuccessStatusCode)
								return true;

							return false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mailgrabber.ViewModel.ViewModelLocator.Logger.Error("HttpClientHelper", ex);
				return false;
			}
		}
	}
}
