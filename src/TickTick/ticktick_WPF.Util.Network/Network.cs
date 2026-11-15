using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Network;

public class Network
{
	private static int _times;

	private static int _errortimes;

	public static async Task<bool> GetHttpWebRequestSuccess(string api, string content = "", List<KeyValuePair<string, string>> paramList = null, string auth = null, string mode = "POST", bool fulluri = false, bool isNeedErrorReturn = false, string filePath = null, bool needReconnect = true, bool defaultApi = false, bool useMs = false)
	{
		return (await GetHttpWebRequestResult(api, content, paramList, auth, mode, fulluri, isNeedErrorReturn, filePath, needReconnect, defaultApi, useMs)).Item2;
	}

	public static async Task<string> GetHttpWebRequest(string api, string content = "", List<KeyValuePair<string, string>> paramList = null, string auth = null, string mode = "POST", bool fulluri = false, bool isNeedErrorReturn = false, string filePath = null, bool needReconnect = true, bool defaultApi = false, bool useMs = false)
	{
		return (await GetHttpWebRequestResult(api, content, paramList, auth, mode, fulluri, isNeedErrorReturn, filePath, needReconnect, defaultApi, useMs)).Item1;
	}

	public static async Task<(string, bool)> GetHttpWebRequestResult(string api, string content = "", List<KeyValuePair<string, string>> paramList = null, string auth = null, string mode = "POST", bool fulluri = false, bool isNeedErrorReturn = false, string filePath = null, bool needReconnect = true, bool defaultApi = false, bool useMs = false)
	{
		string responseReturn = "";
		bool success = false;
		if (auth == string.Empty)
		{
			return (responseReturn, success);
		}
		return await Task.Run(async delegate
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			try
			{
				using HttpClient httpClient = HttpClientHelper.GetHttpClient();
				HttpResponseMessage response = null;
				try
				{
					HttpRequestMessage httpReqMsg = HttpClientHelper.GetHttpReqMsg(auth);
					string text = BaseUrl.GetApiDomain(defaultApi);
					if (useMs)
					{
						text = text.Replace("api", "ms");
					}
					string uriString = (fulluri ? api : (text + api));
					httpReqMsg.RequestUri = new Uri(uriString);
					switch (mode)
					{
					case "POST":
						if (paramList == null || paramList.Count == 0)
						{
							if (filePath != null)
							{
								string fileName = Path.GetFileName(filePath);
								MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
								FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
								StreamContent content2 = new StreamContent(fileStream, (int)fileStream.Length);
								multipartFormDataContent.Add(content2, "file", fileName);
								httpReqMsg.Method = HttpMethod.Post;
								httpReqMsg.Content = multipartFormDataContent;
								response = await httpClient.SendAsync(httpReqMsg);
								fileStream.Close();
							}
							else
							{
								httpReqMsg.Method = HttpMethod.Post;
								httpReqMsg.Content = new StringContent(content, Encoding.UTF8, "application/json");
								response = await httpClient.SendAsync(httpReqMsg);
							}
						}
						else
						{
							httpReqMsg.Method = HttpMethod.Post;
							httpReqMsg.Content = new FormUrlEncodedContent(paramList);
							response = await httpClient.SendAsync(httpReqMsg);
						}
						break;
					case "GET":
						httpReqMsg.Method = HttpMethod.Get;
						response = await httpClient.SendAsync(httpReqMsg);
						break;
					case "PUT":
						httpReqMsg.Method = HttpMethod.Put;
						httpReqMsg.Content = new StringContent(content, Encoding.UTF8, "application/json");
						response = await httpClient.SendAsync(httpReqMsg);
						break;
					case "DELETE":
						httpReqMsg.Method = HttpMethod.Delete;
						httpReqMsg.Content = new StringContent(content, Encoding.UTF8, "application/json");
						response = await httpClient.SendAsync(httpReqMsg);
						break;
					}
					success = response != null && response.StatusCode == HttpStatusCode.OK;
					if (response != null && (response.StatusCode == HttpStatusCode.OK || isNeedErrorReturn))
					{
						responseReturn = (await UtilHttp.ToStringResponseAsync(response)).Content;
						if (App.ProExpiredSyncError)
						{
							App.ProExpiredSyncError = false;
						}
					}
					else if (response != null && response.StatusCode == HttpStatusCode.InternalServerError)
					{
						responseReturn = (await UtilHttp.ToStringResponseAsync(response)).Content;
						if (!string.IsNullOrEmpty(responseReturn))
						{
							HandleApiError(responseReturn);
						}
					}
					else if (response != null && (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden))
					{
						if (response.StatusCode == HttpStatusCode.Forbidden)
						{
							_times++;
							if (_times < 10)
							{
								return (responseReturn: responseReturn, success: success);
							}
						}
						_times = 0;
						UtilLog.Info("log out " + response.StatusCode.ToString() + "   " + api);
						MainWindowManager.TokenOutDate();
					}
				}
				finally
				{
					response?.Dispose();
				}
			}
			catch (Exception ex)
			{
				sw.Stop();
				UtilLog.Warn($"Network.GetHttpWebRequest ,url {api}, {sw.ElapsedMilliseconds} ms," + ex);
			}
			_times = 0;
			if (success)
			{
				_errortimes = 0;
				if (App.GetWindowShow())
				{
					MainWindowManager.SetSyncErrorStatus(NetworkError.None);
				}
			}
			else
			{
				_errortimes++;
				if (_errortimes == 5 && App.GetWindowShow())
				{
					await CheckConnection();
				}
			}
			return (responseReturn: responseReturn, success: success);
		});
	}

	public static async Task PostLog(string uri, string content = "", string auth = null)
	{
		await Task.Run(async delegate
		{
			try
			{
				using HttpClient httpClient = HttpClientHelper.GetHttpClient();
				httpClient.DefaultRequestHeaders.Add("Authorization", "OAuth " + auth);
				httpClient.DefaultRequestHeaders.Add("User-Agent", "TickTick/W-" + Utils.GetVersion());
				httpClient.DefaultRequestHeaders.Add("x-device", Utils.GetDeviceInfo());
				httpClient.DefaultRequestHeaders.Add("x-timestamp", Utils.GetXTimeStamp());
				MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
				byte[] bytes = Encoding.UTF8.GetBytes(content);
				MemoryStream stream = new MemoryStream(bytes);
				StreamContent content2 = new StreamContent(stream, (int)stream.Length);
				multipartFormDataContent.Add(content2, "file", "WindowsLog.txt");
				await httpClient.PostAsync(new Uri(uri), multipartFormDataContent);
				stream.Close();
			}
			catch (Exception)
			{
			}
		});
	}

	public static async Task<bool> PostFiles(string uri, List<string> files)
	{
		if (files == null || files.Count == 0)
		{
			return true;
		}
		HttpResponseMessage response = null;
		return await Task.Run(async delegate
		{
			try
			{
				using HttpClient httpClient = HttpClientHelper.GetHttpClient();
				httpClient.DefaultRequestHeaders.Add("User-Agent", "TickTick/W-" + Utils.GetVersion());
				httpClient.DefaultRequestHeaders.Add("x-device", Utils.GetDeviceInfo());
				httpClient.DefaultRequestHeaders.Add("x-timestamp", Utils.GetXTimeStamp());
				List<string> list = files;
				if (list != null && list.Count > 0)
				{
					MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
					List<FileStream> streams = new List<FileStream>();
					foreach (string file in files)
					{
						string fileName = Path.GetFileName(file);
						FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
						if (fileStream.Length <= 0)
						{
							fileStream.Close();
						}
						else
						{
							streams.Add(fileStream);
							StreamContent content = new StreamContent(fileStream, (int)fileStream.Length);
							multipartFormDataContent.Add(content, "file", fileName);
						}
					}
					response = await httpClient.PostAsync(new Uri(uri), multipartFormDataContent);
					streams.ForEach(delegate(FileStream s)
					{
						s.Close();
					});
					return response != null && response.StatusCode < HttpStatusCode.InternalServerError;
				}
			}
			catch (Exception e)
			{
				UserActCollectUtils.SendException(e, ExceptionType.OtherThread);
				return true;
			}
			return false;
		});
	}

	private static void HandleApiError(string result)
	{
		if (string.IsNullOrEmpty(result))
		{
			return;
		}
		ErrorModel errorModel = JsonConvert.DeserializeObject<ErrorModel>(result);
		string text = errorModel?.errorCode;
		if (!(text == "need_pro"))
		{
			if (text == "need_upgrade_client")
			{
				string forceText = null;
				if (errorModel.errorMessage == "mfa")
				{
					forceText = Utils.GetString("TwoFactorAuthenticationErrorMsg");
				}
				App.Window.HandleNeedUpgradeClient(forceText);
			}
		}
		else
		{
			App.Window.HandleOnNeedPro();
		}
	}

	public static async Task<string> GetHttpRealUrlRequest(string uri, string auth = null)
	{
		string responseReturn = "";
		HttpResponseMessage response;
		return await Task.Run(delegate
		{
			try
			{
				using HttpClient httpClient = new HttpClient(new HttpClientHandler
				{
					AllowAutoRedirect = false
				});
				httpClient.DefaultRequestHeaders.Add("User-Agent", "TickTick/W-" + Utils.GetVersion());
				httpClient.DefaultRequestHeaders.Add("x-device", Utils.GetDeviceInfo());
				httpClient.DefaultRequestHeaders.Add("x-timestamp", Utils.GetXTimeStamp());
				HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(uri));
				response = httpClient.GetAsync(new Uri(uri)).Result;
				IEnumerable<string> values = response.Headers.GetValues("Location");
				responseReturn = values.First();
				httpRequestMessage.Dispose();
			}
			catch (Exception)
			{
			}
			return responseReturn;
		});
	}

	public static async Task<bool> PostDataCollect(string uri, string json)
	{
		try
		{
			await ApiClient.PostJsonStrAsync<object>(uri, json);
		}
		catch (ApiException ex)
		{
			if (ex.Error.StatusCode >= HttpStatusCode.InternalServerError)
			{
				return false;
			}
			UtilLog.Warn(uri + " POST: " + json + ", ex: " + ex);
		}
		catch (HttpRequestException)
		{
			return false;
		}
		catch (Exception ex3)
		{
			UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex3));
		}
		return true;
	}

	public static async Task<bool> TestNetWork()
	{
		return await Task.Run(async delegate
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			using (HttpClient httpClient = HttpClientHelper.GetHttpClient())
			{
				_ = 2;
				try
				{
					string requestUri = BaseUrl.GetApiDomain() + "/about/status";
					using HttpResponseMessage response = await httpClient.GetAsync(requestUri);
					bool flag = response.StatusCode == HttpStatusCode.OK;
					if (!flag)
					{
						flag = await response.Content.ReadAsStringAsync() == "ok";
					}
					if (flag)
					{
						return true;
					}
					string text = await response.Content.ReadAsStringAsync();
					UtilLog.Info("TestNextWork (" + watch.ElapsedMilliseconds + " ms) , result: " + text);
					return false;
				}
				catch (Exception ex)
				{
					watch.Stop();
					UtilLog.Warn("TestNextWork (" + watch.ElapsedMilliseconds + " ms), ex: " + ex);
				}
			}
			return false;
		});
	}

	public static async Task CheckConnection()
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
			MainWindowManager.SetSyncErrorStatus(NetworkError.NoNetwork);
		}
		else if (!(await TestNetWork()))
		{
			MainWindowManager.SetSyncErrorStatus((LocalSettings.Settings.ProxyType == 0) ? NetworkError.Error : NetworkError.CheckProxy);
		}
		else
		{
			MainWindowManager.SetSyncErrorStatus(NetworkError.None);
		}
	}
}
