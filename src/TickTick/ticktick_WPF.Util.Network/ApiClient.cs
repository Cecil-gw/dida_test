using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Network;

public class ApiClient
{
	private static readonly TimeSpan ProxyTimeout = TimeSpan.FromSeconds(5.0);

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30.0);

	private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(100.0);

	private const int UploadType = 1;

	private static ProxyModel _proxyModel;

	private static HttpClient _http;

	private static HttpClient _uploadHttp;

	public static Task<bool> DeleteAsync(string uri)
	{
		return UtilHttp.DeleteAsync(GetHttpClient(), uri);
	}

	internal static Task<bool> DeleteAsync(HttpClient http, string uri)
	{
		return UtilHttp.DeleteAsync(http, uri);
	}

	public static Task<bool> DeleteJsonAsync(string uri, object jsonObj)
	{
		return UtilHttp.DeleteJsonAsync(GetHttpClient(), uri, JsonConvert.SerializeObject(jsonObj));
	}

	public static Task<T> SendFileAsync<T>(string uri, params (string, string)[] files)
	{
		return UtilHttp.SendFileAsync<T>(GetHttpClient(1), uri, files);
	}

	public static Task<T> PostFormAsync<T>(string uri, params (string name, string value, bool isFile)[] param)
	{
		return UtilHttp.PostFormAsync<T>(GetHttpClient(1), uri, param);
	}

	public static Task<bool> PostJsonSuccessAsync(string uri, object jsonObj)
	{
		return UtilHttp.PostJsonSuccessAsync(GetHttpClient(), uri, JsonConvert.SerializeObject(jsonObj));
	}

	public static Task<T> PostJsonAsync<T>(string uri, object jsonObj, HttpClient client = null)
	{
		return UtilHttp.PostJsonStrAsync<T>(client ?? GetHttpClient(), uri, JsonConvert.SerializeObject(jsonObj));
	}

	public static Task<T> PostJsonStrAsync<T>(string uri, string json)
	{
		return UtilHttp.PostJsonStrAsync<T>(GetHttpClient(), uri, json);
	}

	public static Task<T> PutJsonAsync<T>(string uri, object jsonObj)
	{
		return UtilHttp.PutJsonStrAsync<T>(GetHttpClient(), uri, JsonConvert.SerializeObject(jsonObj));
	}

	public static Task<bool> PutJsonSuccessAsync(string uri, object jsonObj)
	{
		return UtilHttp.PutJsonSuccessAsync(GetHttpClient(), uri, JsonConvert.SerializeObject(jsonObj));
	}

	public static async Task PostJsonStreamAsync(string uri, string json, BlockingCollection<string> collection, CancellationToken? ct)
	{
		await UtilHttp.PostJsonStreamAsync(GetHttpClient(), uri, json, collection, ct);
	}

	public static Task<string> GetAsync(string uri, HttpClient client = null)
	{
		return UtilHttp.GetAsync(client ?? GetHttpClient(), uri);
	}

	public static Task<T> GetJsonAsync<T>(string uri, HttpClient client = null)
	{
		return UtilHttp.GetJsonAsync<T>(client ?? GetHttpClient(), uri);
	}

	public static Task<bool> GetSuccessAsync(string uri)
	{
		return UtilHttp.GetSuccessAsync(GetHttpClient(), uri);
	}

	public static Task<string> GetFile(string uri, string fileName)
	{
		return UtilHttp.GetFile(GetHttpClient(1), uri, fileName);
	}

	public static async Task<string> TestProxy(ProxyModel proxyModel)
	{
		Stopwatch watch = Stopwatch.StartNew();
		string uri = BaseUrl.GetApiDomain() + "/about/status";
		UtilLog.Info("TestProxy proxyModel=" + proxyModel?.ToString() + ": " + uri);
		ApiMessageProcessingHandler handler = CreateApiMessageProcessingHandler(proxyModel);
		using HttpClient http = new HttpClient(handler)
		{
			Timeout = ProxyTimeout
		};
		_ = 2;
		try
		{
			using HttpResponseMessage response = await http.GetAsync(uri);
			bool flag = response.StatusCode == HttpStatusCode.OK;
			if (!flag)
			{
				flag = await response.Content.ReadAsStringAsync() == "ok";
			}
			if (flag)
			{
				return "ok";
			}
			string text = await response.Content.ReadAsStringAsync();
			UtilLog.Info("TestProxy test(" + watch.ElapsedMilliseconds + "ms): " + uri + ", result: " + text);
			return text;
		}
		catch (Exception ex)
		{
			watch.Stop();
			UtilLog.Warn("TestProxy test(" + watch.ElapsedMilliseconds + "ms) failed: " + uri + ", ex: " + ex);
			throw;
		}
	}

	public static HttpClient GetDownloadClient()
	{
		return new HttpClient(CreateApiMessageProcessingHandler(GetProxyModel()))
		{
			Timeout = UploadTimeout
		};
	}

	public static HttpClient GetHttpClient(List<(string, string)> extraHeaders)
	{
		return new HttpClient(CreateApiMessageProcessingHandler(GetProxyModel(), extraHeaders))
		{
			Timeout = Timeout
		};
	}

	public static HttpClient GetUploadClient()
	{
		return GetHttpClient(1);
	}

	private static HttpClient GetHttpClient(int type = 0)
	{
		ProxyModel proxyModel = GetProxyModel();
		if (!proxyModel.IsSame(_proxyModel))
		{
			UtilLog.Info("GetHttpClient create HttpClient use " + proxyModel?.ToString() + ", old: " + _proxyModel);
			ApiMessageProcessingHandler handler = CreateApiMessageProcessingHandler(proxyModel);
			_http = new HttpClient(handler)
			{
				Timeout = Timeout
			};
			_uploadHttp = new HttpClient(handler)
			{
				Timeout = UploadTimeout
			};
			_proxyModel = proxyModel;
		}
		if (type != 1)
		{
			return _http;
		}
		return _uploadHttp;
	}

	private static ApiMessageProcessingHandler CreateApiMessageProcessingHandler(ProxyModel proxyModel, List<(string, string)> extraHeaders = null)
	{
		return new ApiMessageProcessingHandler(extraHeaders)
		{
			InnerHandler = new HttpClientHandler
			{
				Proxy = proxyModel?.proxy,
				UseProxy = (proxyModel?.proxy != null)
			}
		};
	}

	private static ProxyModel GetProxyModel()
	{
		LocalSettings settings = LocalSettings.Settings;
		return ProxyModel.Create(settings.ProxyType, settings.ProxyAddress, settings.ProxyPort, settings.ProxyUsername, settings.ProxyPassword, settings.ProxyDomain);
	}
}
