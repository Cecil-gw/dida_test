using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util;

public static class UserActCollectUtils
{
	private static readonly DelayActionHandler _repostDeviceHandler = new DelayActionHandler(180000);

	private static readonly Timer _postEventTimer = new Timer(10000.0);

	private static string _oldFileUrl = AppPaths.DataDir + "ticktick_events_tmp.txt";

	private static string _fileUrl = AppPaths.DataDir + "dida_events_tmp.txt";

	private static readonly BlockingList<string> StoredJson = new BlockingList<string>();

	private static object _o = new object();

	private static bool _init;

	private static DateTime _nextPostTime;

	private static int CustomExceptionCount = 100;

	public static void StartPost()
	{
		if (!_init)
		{
			_repostDeviceHandler.SetAction(TryPostDevice);
			_postEventTimer.Elapsed += TryPostEvent;
			_postEventTimer.Start();
			PostEvents().ContinueWith(UtilRun.LogTask);
			_init = true;
		}
	}

	private static void TryPostDevice(object sender, EventArgs e)
	{
		PostDevice().ContinueWith(UtilRun.LogTask);
	}

	public static void PostEventNow()
	{
		_nextPostTime = DateTime.Now;
		WriteJsonsToFile();
		PostEvents().ContinueWith(UtilRun.LogTask);
	}

	public static async Task OnDeviceDataChanged()
	{
		try
		{
			await PostDevice();
		}
		catch (HttpRequestException)
		{
		}
	}

	private static async Task PostDevice()
	{
		await UpdateDevice();
		bool flag = true;
		if (!string.IsNullOrEmpty(LocalSettings.Settings.UserDeviceString))
		{
			flag = await Communicator.PostUserDevice(LocalSettings.Settings.UserDeviceString);
		}
		if (flag)
		{
			_repostDeviceHandler.CancelAction();
		}
		else
		{
			_repostDeviceHandler.TryDoAction();
		}
	}

	private static async Task UpdateDevice()
	{
		string windowsVersion = "Windows_" + Utils.GetWindowsVersion();
		string machineName = Environment.MachineName;
		UserDeviceInfo userDeviceInfo = new UserDeviceInfo
		{
			did = App.UId,
			ver = Utils.GetVersion(),
			channel = "website",
			os = windowsVersion,
			name = machineName,
			ctime = DateTime.Now
		};
		UserDeviceInfo userDeviceInfo2 = userDeviceInfo;
		userDeviceInfo2.userCode = await UserManager.GetUserCode();
		userDeviceInfo.hl = LocalSettings.Settings.UserChooseLanguage;
		userDeviceInfo.tz = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
		userDeviceInfo.enable_tz = TimeZoneData.EnableTz;
		userDeviceInfo.locale = CultureInfo.InstalledUICulture.Name.Replace("-", "_");
		UserDeviceInfo deviceModel = userDeviceInfo;
		deviceModel.device = windowsVersion + "x64";
		UserActionModel userActionModel = new UserActionModel
		{
			kind = "app",
			time = DateTime.Now
		};
		UserActionModel userActionModel2 = userActionModel;
		userActionModel2.userCode = await UserManager.GetUserCode();
		userActionModel.type = "device";
		userActionModel.ctype = "update";
		userActionModel.data = deviceModel;
		userActionModel.did = App.UId;
		userActionModel.id = Utils.GetGuid();
		string userDeviceString = JsonConvert.SerializeObject(userActionModel);
		LocalSettings.Settings.UserDeviceString = userDeviceString;
	}

	public static async void AddSortOptionEvent(string type, string ctype, string groupBy, string orderBy)
	{
		try
		{
			JObject jObject = new JObject();
			jObject["label"] = groupBy + "_" + orderBy;
			jObject["pm_group_by"] = groupBy;
			jObject["pm_order_by"] = orderBy;
			await AddEvent("biz", type, ctype, jObject);
		}
		catch (Exception)
		{
		}
	}

	public static async void SendUpgradePurchaseSuccessData(string from, string productId)
	{
		await SendUpgradeData("purchase_succeed", from, productId);
	}

	public static async void SendUpgradePurchaseData(string from, string productId)
	{
		await SendUpgradeData("purchase", from, productId);
	}

	public static async void SendUpgradeShowData(string from)
	{
		await SendUpgradeData("show", from);
	}

	public static async void SendUpgradePromptData(string from)
	{
		await SendUpgradeData("prompt", from);
	}

	public static async Task SendUserDataWithGroup(string ctype, string label, string group)
	{
		try
		{
			JObject data = new JObject
			{
				["group"] = group,
				["label"] = label,
				["platform"] = "windows"
			};
			await AddEvent("biz", "user_data", ctype, data);
			UtilLog.Info("UserActCollect.SendUserData: " + ctype + " " + label);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private static async Task SendUpgradeData(string ctype, string from, string productId = null)
	{
		try
		{
			JObject data = new JObject
			{
				["group"] = ABTestManager.GetPremiumV1PlanCode(),
				["label"] = from,
				["platform"] = "windows"
			};
			if (productId != null)
			{
				data["productId"] = productId;
			}
			await AddEvent("biz", "upgrade_data", ctype, data);
			UtilLog.Info("UserActCollect.SendUpgradeData: " + ctype + " " + data);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	public static async void AddTaskLabelEvent(string type, string ctype, string label, long time)
	{
		try
		{
			JObject jObject = new JObject();
			jObject["label"] = label;
			jObject["loading_time"] = time;
			await AddEvent("biz", type, ctype, jObject);
		}
		catch (Exception)
		{
		}
	}

	public static async void AddClickEventWithData(string type, string ctype, JObject data)
	{
		try
		{
			await AddEvent("biz", type, ctype, data);
			UtilLog.Info("UserActCollect.AddClickEvent: " + type + " " + ctype + " " + data);
		}
		catch (Exception)
		{
		}
	}

	public static async void AddClickEvent(string type, string ctype, string data)
	{
		try
		{
			JObject jObject = new JObject();
			jObject["label"] = data;
			await AddEvent("biz", type, ctype, jObject);
			UtilLog.Info("UserActCollect.AddClickEvent: " + type + " " + ctype + " " + data);
		}
		catch (Exception)
		{
		}
	}

	private static async Task AddEvent(string kind, string type, string ctype, JObject data)
	{
		JArray jArray = new JArray();
		ABTestManager.GetAllGroups().ForEach(delegate(string g)
		{
			jArray.Add(g);
		});
		data["group"] = jArray;
		UserActionModel userActionModel = new UserActionModel
		{
			kind = kind,
			time = DateTime.Now,
			did = App.UId
		};
		UserActionModel userActionModel2 = userActionModel;
		userActionModel2.userCode = await UserManager.GetUserCode();
		userActionModel.type = type;
		userActionModel.ctype = ctype;
		userActionModel.data = data;
		userActionModel.id = Utils.GetGuid();
		AddJson(JsonConvert.SerializeObject(userActionModel));
	}

	private static void AddJson(string json)
	{
		if (!string.IsNullOrEmpty(json))
		{
			if (StoredJson.Count < 100)
			{
				StoredJson.Add(json);
			}
			else if (StoredJson.Count == 100)
			{
				StoredJson.Add(json);
				UtilLog.Warn("UserActCollect.AddJson Warn: over limit");
			}
		}
	}

	private static void TryPostEvent(object sender, ElapsedEventArgs e)
	{
		WriteJsonsToFile();
		PostEvents().ContinueWith(UtilRun.LogTask);
	}

	private static void WriteJsonsToFile()
	{
		lock (_o)
		{
			List<string> list = StoredJson.ToList();
			if (list.Count == 0)
			{
				return;
			}
			StoredJson.Clear();
			try
			{
				using FileStream fileStream = new FileStream(_fileUrl, FileMode.Append);
				foreach (string item in list)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(item + Environment.NewLine);
					fileStream.Write(bytes, 0, bytes.Length);
				}
			}
			catch (Exception ex)
			{
				UtilLog.Warn(ex.Message);
			}
		}
	}

	private static async Task PostEvents()
	{
		if (DateTime.Now < _nextPostTime)
		{
			return;
		}
		_nextPostTime = DateTime.Now.AddHours(1.0);
		UtilLog.Info("UserActCollect.StartPostEvents");
		DoCompress();
		if (Utils.IsNetworkAvailable())
		{
			FileInfo[] files = new DirectoryInfo(AppPaths.DataDir).GetFiles("ticktick_events_*.zip");
			List<string> fileList = (from f in files
				select f.FullName into f
				orderby f
				select f).ToList();
			UtilLog.Info("UserActCollect.StartPostEvents FileCount: " + fileList.Count);
			if (!fileList.Any())
			{
				return;
			}
			List<string> postFiles = new List<string>();
			long num = 0L;
			bool postFailed = false;
			foreach (string item in fileList)
			{
				num += new FileInfo(item).Length;
				postFiles.Add(item);
				if (num > 200000)
				{
					if (!(await DoPost(postFiles)))
					{
						SendException(new Exception("PostEvent Failed " + fileList.Count), ExceptionType.OtherThread);
						postFailed = true;
						break;
					}
					postFiles.Clear();
					num = 0L;
				}
			}
			if (!postFailed && postFiles.Any())
			{
				postFailed = !(await DoPost(postFiles));
			}
			if (postFailed)
			{
				_nextPostTime = DateTime.Now.AddMinutes(5.0);
				UtilLog.Info("UserActCollect.StartPostEvents Failed");
			}
		}
		else
		{
			_nextPostTime = DateTime.Now.AddMinutes(5.0);
		}
	}

	private static async Task<bool> DoPost(List<string> files)
	{
		if (files == null || files.Count == 0)
		{
			return true;
		}
		if (await Communicator.PostUserEvents(files))
		{
			foreach (string file in files)
			{
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}
			return true;
		}
		return false;
	}

	private static void DoCompress()
	{
		if (File.Exists(_oldFileUrl) && new FileInfo(_oldFileUrl).Length > 10)
		{
			UtilLog.Info("UserActCollect.DoCompress fileLength: " + new FileInfo(_oldFileUrl).Length);
			string text = DateTime.Now.AddMinutes(-1.0).ToString("yyyyMMddHHmm", App.Ci);
			string zipUrl = AppPaths.DataDir + "ticktick_events_" + text + ".zip";
			if (CompressFile(_oldFileUrl, zipUrl))
			{
				new FileStream(_oldFileUrl, FileMode.Truncate, FileAccess.ReadWrite).Close();
			}
		}
		if (File.Exists(_fileUrl) && new FileInfo(_fileUrl).Length > 10)
		{
			UtilLog.Info("UserActCollect.DoCompress fileLength: " + new FileInfo(_fileUrl).Length);
			string text2 = DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
			string zipUrl2 = AppPaths.DataDir + "ticktick_events_" + text2 + ".zip";
			if (CompressFile(_fileUrl, zipUrl2))
			{
				new FileStream(_fileUrl, FileMode.Truncate, FileAccess.ReadWrite).Close();
			}
		}
	}

	private static bool CompressFile(string fileUrl, string zipUrl)
	{
		try
		{
			using ZipArchive destination = ZipFile.Open(zipUrl, ZipArchiveMode.Create);
			string fileName = Path.GetFileName(fileUrl);
			destination.CreateEntryFromFile(fileUrl, fileName);
			return true;
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ex.Message);
			return false;
		}
	}

	public static async Task SendCustomException(string custom)
	{
		if (CustomExceptionCount-- > 0)
		{
			await Communicator.PostException(new Exception(custom), ExceptionType.Custom);
		}
		UtilLog.Info(custom);
	}

	public static async Task SendException(Exception e, ExceptionType type)
	{
		if (e != null)
		{
			await Communicator.PostException(e, type);
		}
	}

	public static void OnAppExit()
	{
		_postEventTimer.Stop();
		if (StoredJson.Count > 0)
		{
			WriteJsonsToFile();
		}
	}

	public static void AddShortCutEvent(string ctype, string label)
	{
		AddClickEvent("shortcut", ctype, label);
	}
}
