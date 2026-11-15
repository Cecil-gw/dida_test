using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TickTickUtils.Lang;

public class UtilHttp
{
	public static async Task<T> SendFileAsync<T>(HttpClient http, string uri, params (string, string)[] files)
	{
		using MultipartFormDataContent body = new MultipartFormDataContent();
		for (int i = 0; i < files.Length; i++)
		{
			(string, string) tuple = files[i];
			body.Add(new StreamContent(File.OpenRead(tuple.Item1)), "file", Path.GetFileName(tuple.Item2));
		}
		using HttpResponseMessage response = await http.PostAsync(uri, body);
		return await ToJsonResult<T>(response);
	}

	public static async Task<T> PostFormAsync<T>(HttpClient http, string uri, params (string name, string value, bool isFile)[] param)
	{
		using MultipartFormDataContent body = new MultipartFormDataContent();
		for (int i = 0; i < param.Length; i++)
		{
			(string, string, bool) tuple = param[i];
			if (tuple.Item3)
			{
				body.Add(new StreamContent(File.OpenRead(tuple.Item2)), tuple.Item1, Path.GetFileName(tuple.Item2));
			}
			else
			{
				body.Add(new StringContent(tuple.Item2, Encoding.UTF8, "application/json"), tuple.Item1);
			}
		}
		using HttpResponseMessage response = await http.PostAsync(uri, body);
		return await ToJsonResult<T>(response);
	}

	public static async Task<string> GetAsync(HttpClient http, string uri)
	{
		using HttpResponseMessage response = await http.GetAsync(uri);
		return (await ToStringResponseAsync(response)).Content;
	}

	public static async Task<T> GetJsonAsync<T>(HttpClient http, string uri)
	{
		using HttpResponseMessage response = await http.GetAsync(uri);
		return await ToJsonResult<T>(response);
	}

	public static async Task<bool> GetSuccessAsync(HttpClient http, string uri)
	{
		using HttpResponseMessage response = await http.GetAsync(uri);
		if (response.StatusCode == HttpStatusCode.InternalServerError)
		{
			HttpResponse<string> httpResponse = await ToStringResponseAsync(response);
			if (httpResponse != null && httpResponse.Content != null && httpResponse.Content.Contains("errorCode"))
			{
				throw new ApiException(ToApiError(httpResponse));
			}
			return false;
		}
		return response.StatusCode == HttpStatusCode.OK;
	}

	public static async Task<bool> PostJsonSuccessAsync(HttpClient http, string uri, string jsonStr)
	{
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await http.PostAsync(uri, body);
		if (response.StatusCode == HttpStatusCode.InternalServerError)
		{
			HttpResponse<string> httpResponse = await ToStringResponseAsync(response);
			if (httpResponse != null && httpResponse.Content != null && httpResponse.Content.Contains("errorCode"))
			{
				throw new ApiException(ToApiError(httpResponse));
			}
			return false;
		}
		return response.StatusCode == HttpStatusCode.OK;
	}

	public static async Task<T> PostJsonStrAsync<T>(HttpClient http, string uri, string jsonStr)
	{
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await http.PostAsync(uri, body);
		return await ToJsonResult<T>(response);
	}

	public static async Task PostJsonStreamAsync(HttpClient http, string uri, string jsonStr, BlockingCollection<string> collection, CancellationToken? ct)
	{
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
		request.Content = body;
		HttpResponseMessage response = null;
		try
		{
			response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			if (response.StatusCode != HttpStatusCode.OK)
			{
				Console.WriteLine(await response.Content.ReadAsStringAsync());
				collection.CompleteAdding();
				return;
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			collection.CompleteAdding();
			return;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				using Stream stream = await response.Content.ReadAsStreamAsync();
				using StreamReader reader = new StreamReader(stream);
				string item;
				while ((item = await reader.ReadLineAsync()) != null && (!ct.HasValue || !ct.Value.IsCancellationRequested))
				{
					collection.Add(item);
				}
			}
			finally
			{
				response?.Dispose();
				collection.CompleteAdding();
			}
		});
	}

	public static async Task<bool> PutJsonSuccessAsync(HttpClient http, string uri, string jsonStr)
	{
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await http.PutAsync(uri, body);
		if (response.StatusCode == HttpStatusCode.InternalServerError)
		{
			HttpResponse<string> httpResponse = await ToStringResponseAsync(response);
			if (httpResponse != null && httpResponse.Content != null && httpResponse.Content.Contains("errorCode"))
			{
				throw new ApiException(ToApiError(httpResponse));
			}
			return false;
		}
		return response.StatusCode == HttpStatusCode.OK;
	}

	public static async Task<T> PutJsonStrAsync<T>(HttpClient http, string uri, string jsonStr)
	{
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpResponseMessage response = await http.PutAsync(uri, body);
		return await ToJsonResult<T>(response);
	}

	public static async Task<bool> DeleteAsync(HttpClient http, string uri)
	{
		using HttpResponseMessage httpResponseMessage = await http.DeleteAsync(uri, default(CancellationToken));
		return httpResponseMessage.IsSuccessStatusCode;
	}

	public static async Task<bool> DeleteJsonAsync(HttpClient http, string uri, string jsonStr)
	{
		CancellationToken cancellationToken = default(CancellationToken);
		using StringContent body = new StringContent(jsonStr, Encoding.UTF8, "application/json");
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri)
		{
			Content = body
		};
		using HttpResponseMessage httpResponseMessage = await http.SendAsync(request, cancellationToken);
		return httpResponseMessage.IsSuccessStatusCode;
	}

	private static async Task<T> ToJsonResult<T>(HttpResponseMessage response)
	{
		HttpResponse<string> httpResponse = await ToStringResponseAsync(response);
		if (httpResponse.IsSuccessStatusCode)
		{
			return UtilJson.DeserializeObject<T>(httpResponse.Content);
		}
		throw new ApiException(ToApiError(httpResponse));
	}

	private static ApiError ToApiError(HttpResponse<string> response)
	{
		try
		{
			ApiError apiError = UtilJson.DeserializeObject<ApiError>(response.Content);
			if (apiError != null)
			{
				apiError.StatusCode = response.StatusCode;
			}
			return apiError;
		}
		catch (Exception)
		{
			return new ApiError
			{
				ErrorMessage = response.Content
			};
		}
	}

	public static async Task<HttpResponse<string>> ToStringResponseAsync(HttpResponseMessage response)
	{
		HttpStatusCode statusCode = response.StatusCode;
		using HttpContent content = response.Content;
		return new HttpResponse<string>(statusCode, await content.ReadAsStringAsync());
	}

	public static async Task<string> GetFile(HttpClient http, string uri, string filename)
	{
		using HttpResponseMessage response = await http.GetAsync(uri);
		if (response.StatusCode != HttpStatusCode.OK)
		{
			return await response.Content.ReadAsStringAsync();
		}
		using Stream contentStream = await response.Content.ReadAsStreamAsync();
		using Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 10000000, useAsync: true);
		await contentStream.CopyToAsync(stream);
		return string.Empty;
	}

	public static void FixContent(HttpRequestMessage request)
	{
		if (!(request.Content is MultipartFormDataContent multipartFormDataContent))
		{
			return;
		}
		foreach (HttpContent item in multipartFormDataContent)
		{
			FixFileName(item);
		}
	}

	private static void FixFileName(HttpContent content)
	{
		HttpContentHeaders headers = content.Headers;
		ContentDispositionHeaderValue contentDisposition = headers.ContentDisposition;
		if (contentDisposition == null)
		{
			return;
		}
		string fileName = contentDisposition.FileName;
		if (!string.IsNullOrEmpty(fileName))
		{
			string text = UtilString.ToUtf8(fileName);
			if (!fileName.Equals(text))
			{
				UtilLog.Info("FixFileName: " + fileName + " = utf8 => " + text);
				headers.ContentDisposition = null;
				headers.Add("Content-Disposition", contentDisposition.DispositionType + "; name=\"" + contentDisposition.Name + "\"; filename=\"" + text + "\"");
			}
		}
	}
}
