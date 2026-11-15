using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Upgrade;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Util;

public class Utils
{
	public class TupleList<T1, T2> : List<Tuple<T1, T2>>
	{
		public void Add(T1 item, T2 item2)
		{
			Add(new Tuple<T1, T2>(item, item2));
		}
	}

	public class TupleList<T1, T2, T3> : List<Tuple<T1, T2, T3>>
	{
		public void Add(T1 item, T2 item2, T3 item3)
		{
			Add(new Tuple<T1, T2, T3>(item, item2, item3));
		}
	}

	public class TupleList<T1, T2, T3, T4> : List<Tuple<T1, T2, T3, T4>>
	{
		public void Add(T1 item, T2 item2, T3 item3, T4 item4)
		{
			Add(new Tuple<T1, T2, T3, T4>(item, item2, item3, item4));
		}
	}

	public const int AM = 0;

	public const int PM = 1;

	public static IToastShowWindow ToastWindow;

	public static string LocalTimeZone { get; private set; }

	public static bool IsChinese(string content)
	{
		return new Regex("^[\\u4e00-\\u9fa5]{0,}$").IsMatch(content);
	}

	public static bool ContainsChineseInText(string content)
	{
		if (!string.IsNullOrEmpty(content))
		{
			return content.Any((char ch) => IsChinese(ch.ToString()));
		}
		return false;
	}

	public static async Task<string> GetAvatarUrl(string tempUserCode)
	{
		return await Task.Run(async delegate
		{
			if (tempUserCode == null)
			{
				return "../Assets/logo.png";
			}
			UserPublicProfilesModel userPublicProfilesModel = await UserPublicProfilesDao.GetUserInfoByUserCode(tempUserCode);
			return (userPublicProfilesModel != null) ? userPublicProfilesModel.avatarUrl : "../Assets/avatar-new.png";
		});
	}

	public static async Task LogotEmptyDb()
	{
		LocalSettings.Settings.LoginUserId = string.Empty;
		LocalSettings.Settings.LoginUserAuth = string.Empty;
		LocalSettings.Settings.InServerBoxId = string.Empty;
		LocalSettings.Settings.InBoxId = -1;
		LocalSettings.Settings.LoginAvatarUrl = string.Empty;
		await LocalSettings.Settings.Save();
	}

	public static async Task<int> CreatInbox()
	{
		if (LocalSettings.Settings.InBoxId.ToString() == "-1")
		{
			ProjectModel projectModel = (await ProjectDao.GetAllProjects()).FirstOrDefault((ProjectModel project) => project.Isinbox);
			return await ProjectDao.TryUpdateProject(new ProjectModel
			{
				name = GetString("Inbox"),
				Isinbox = true,
				userid = LocalSettings.Settings.LoginUserId,
				id = LocalSettings.Settings.InServerBoxId,
				inAll = true,
				color = projectModel?.color,
				sortOrder = long.MinValue
			});
		}
		return LocalSettings.Settings.InBoxId;
	}

	public static async Task<TaskReminderModel[]> CopyReminderItem(TaskModel repeatTask, TaskReminderModel[] items)
	{
		if (items == null)
		{
			return new List<TaskReminderModel>().ToArray();
		}
		List<TaskReminderModel> newItemsList = new List<TaskReminderModel>();
		foreach (TaskReminderModel taskReminderModel in items)
		{
			TaskReminderModel taskReminderModel2 = new TaskReminderModel
			{
				id = GetGuid(),
				Taskid = repeatTask._Id,
				taskserverid = repeatTask.id,
				trigger = taskReminderModel.trigger
			};
			newItemsList.Add(taskReminderModel2);
			await TaskReminderDao.SaveReminders(taskReminderModel2);
		}
		return newItemsList.ToArray();
	}

	public static async Task<AttachmentModel[]> CopyAttachmentItem(TaskModel repeatTask, string oldTaskid)
	{
		List<AttachmentModel> list = await AttachmentDao.GetTaskAttachments(oldTaskid);
		List<AttachmentModel> newItemsList = new List<AttachmentModel>();
		foreach (AttachmentModel item in list)
		{
			if (item.status == 0)
			{
				AttachmentModel temp = new AttachmentModel
				{
					id = GetGuid(),
					refId = item.refId,
					createdTime = item.createdTime,
					taskId = repeatTask.id,
					path = item.path,
					deleted = item.deleted,
					fileName = item.fileName,
					fileType = item.fileType,
					localPath = item.localPath,
					size = item.size
				};
				newItemsList.Add(temp);
				await AttachmentDao.InsertOrUpdateAttachment(temp);
				if (repeatTask.kind != "CHECKLIST")
				{
					repeatTask.content = repeatTask.content?.Replace(item.id, temp.id);
				}
			}
		}
		return newItemsList.ToArray();
	}

	public static long GetNowTimeStamp()
	{
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
		return (long)(DateTime.Now - dateTime).TotalSeconds;
	}

	public static long GetNowTimeStampInMills()
	{
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
		return (long)(DateTime.Now - dateTime).TotalMilliseconds;
	}

	public static long GetNowTimeStampInMills2()
	{
		return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
	}

	public static DateTime TimeStampToDateTime(string timeStamp)
	{
		DateTime dateTime = new DateTime(1970, 1, 1);
		long ticks = long.Parse(timeStamp + "0000000");
		TimeSpan value = new TimeSpan(ticks);
		return TimeZone.CurrentTimeZone.ToLocalTime(dateTime.Add(value));
	}

	public static DateTime TimeStampToDateTime(long timeStamp)
	{
		DateTime dateTime = new DateTime(1970, 1, 1);
		return TimeZone.CurrentTimeZone.ToLocalTime(dateTime.AddMilliseconds(timeStamp));
	}

	public static bool IsValidEmail0(string email)
	{
		try
		{
			if (email.IndexOf(' ') >= 0)
			{
				return false;
			}
			int num = email.IndexOf('@');
			if (email[0] == '_' || num == -1)
			{
				return false;
			}
			int num2 = email.IndexOf('.', num);
			if (num != email.LastIndexOf('@') || num2 <= 0 || num2 == email.Length - 1 || num2 != email.LastIndexOf('.'))
			{
				return false;
			}
			return new MailAddress(email).Address == email;
		}
		catch
		{
			return false;
		}
	}

	public static string GetGuid0(int maxLength = -1)
	{
		string text = Guid.NewGuid().ToString("N").Substring(8);
		if (maxLength <= 0 || text.Length <= maxLength)
		{
			return text;
		}
		return text.Substring(0, maxLength);
	}

	public static string GetGuid()
	{
		try
		{
			return ObjectIdGenerator.GenerateString();
		}
		catch (Exception ex)
		{
			UserActCollectUtils.SendCustomException(ExceptionUtils.BuildExceptionMessage(ex));
			return GetGuid0();
		}
	}

	public static string GetTriggerValue(Dictionary<string, int> dict)
	{
		string text = "TRIGGER:-PT";
		int num = (dict.ContainsKey("M") ? dict["M"] : 0);
		int num2 = (dict.ContainsKey("H") ? dict["H"] : 0);
		num = (dict.ContainsKey("H") ? dict["D"] : 0) * 24 * 60 + num2 * 60 + num;
		if (num == 0)
		{
			return text + "0S";
		}
		return text + num + "M";
	}

	public static string GetShortWeekName(int weekday)
	{
		return App.Ci.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)weekday);
	}

	public static string GetWeekLabel(DayOfWeek day)
	{
		return App.Ci.DateTimeFormat.GetShortestDayName(day);
	}

	public static void LoadDefaultLanguage()
	{
		string text = "zh-CN";
		System.Windows.Application.Current?.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("Resource\\StringResource\\StringResource." + text + ".xaml", UriKind.Relative)
		});
	}

	public static void SwitchLanguage()
	{
		List<ResourceDictionary> list = System.Windows.Application.Current?.Resources.MergedDictionaries.ToList();
		string languageName = App.Ci.ToString();
		if ((IsDida() && languageName == "zh-CN") || (!IsDida() && languageName == "en-US"))
		{
			AddExtra();
			return;
		}
		switch (App.Ci.ToString())
		{
		case "zh-HK":
			languageName = "zh_rHK";
			break;
		default:
			languageName = languageName.Substring(0, 2);
			break;
		case "zh-CN":
		case "zh-TW":
		case "en-US":
			break;
		}
		string uriString = "Resource\\StringResource\\StringResource." + languageName + ".xaml";
		if (languageName != "en-US")
		{
			string defaultDict = "Resource\\StringResource\\StringResource.en-US.xaml";
			if (languageName == "zh-TW" || languageName == "zh_rHK")
			{
				defaultDict = "Resource\\StringResource\\StringResource.zh-CN.xaml";
			}
			ResourceDictionary resourceDictionary = list?.FirstOrDefault((ResourceDictionary d) => d.Source.OriginalString.Equals(defaultDict));
			if (resourceDictionary != null)
			{
				System.Windows.Application.Current?.Resources.MergedDictionaries.Remove(resourceDictionary);
				System.Windows.Application.Current?.Resources.MergedDictionaries.Add(resourceDictionary);
			}
			else
			{
				System.Windows.Application.Current?.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri(defaultDict, UriKind.Relative)
				});
			}
		}
		try
		{
			System.Windows.Application.Current?.Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(uriString, UriKind.Relative)
			});
		}
		catch (Exception)
		{
		}
		AddExtra();
		void AddExtra()
		{
			if (languageName == "zh-CN" || languageName == "zh-TW" || languageName == "en-US")
			{
				string uriString2 = $"Resource\\StringResource\\String.D.{App.Ci}.xaml";
				System.Windows.Application.Current?.Resources.MergedDictionaries.Add(new ResourceDictionary
				{
					Source = new Uri(uriString2, UriKind.Relative)
				});
			}
		}
	}

	public static long GetUserLimit(Constants.LimitKind kind)
	{
		return long.MaxValue;
	}

	[Obsolete("文不对题，用ThreadUtil.DetachedRunOnUiThread")]
	public static void RunOnUiThread(Dispatcher dispatcher, Action action)
	{
		if (action != null)
		{
			new Thread((ThreadStart)delegate
			{
				dispatcher?.Invoke(DispatcherPriority.Normal, action);
			}).Start();
		}
	}

	public static void RunOnBackgroundThread(Dispatcher dispatcher, Action action)
	{
		if (action != null)
		{
			new Thread((ThreadStart)delegate
			{
				dispatcher?.Invoke(DispatcherPriority.Background, action);
			}).Start();
		}
	}

	public static Geometry GetIconData(string index, ResourceDictionary context = null)
	{
		if (string.IsNullOrEmpty(index))
		{
			return null;
		}
		if (context != null)
		{
			return (context.FindName(index) as System.Windows.Shapes.Path)?.Data;
		}
		return (System.Windows.Application.Current?.TryFindResource(index) as System.Windows.Shapes.Path)?.Data;
	}

	public static DrawingImage GetImageSource(string index, ResourceDictionary context = null)
	{
		if (string.IsNullOrEmpty(index))
		{
			return null;
		}
		if (context != null)
		{
			return context.FindName(index) as DrawingImage;
		}
		return System.Windows.Application.Current?.TryFindResource(index) as DrawingImage;
	}

	public static DrawingImage GetImageSource(string index, FrameworkElement element)
	{
		if (string.IsNullOrEmpty(index))
		{
			return null;
		}
		if (element != null && element.TryFindResource(index) is DrawingImage result)
		{
			return result;
		}
		return System.Windows.Application.Current?.TryFindResource(index) as DrawingImage;
	}

	public static async Task<BitmapImage> GetNetWorkImageAsync(string url, int? width = null)
	{
		if (string.IsNullOrEmpty(url?.Trim()))
		{
			return null;
		}
		BitmapImage image = new BitmapImage();
		await Task.Run(delegate
		{
			try
			{
				Stream responseStream = ((HttpWebRequest)WebRequest.Create(url)).GetResponse().GetResponseStream();
				if (responseStream != null)
				{
					BitmapToBitmapImage(new Bitmap(System.Drawing.Image.FromStream(responseStream)), width, image);
				}
			}
			catch (Exception)
			{
			}
		});
		return image;
	}

	public static BitmapImage GetNetWorkImage(string url, int? width = null)
	{
		if (string.IsNullOrEmpty(url?.Trim()))
		{
			return null;
		}
		try
		{
			Stream responseStream = ((HttpWebRequest)WebRequest.Create(url)).GetResponse().GetResponseStream();
			if (responseStream != null)
			{
				return BitmapToBitmapImage(new Bitmap(System.Drawing.Image.FromStream(responseStream)), width);
			}
		}
		catch (Exception)
		{
			return null;
		}
		return null;
	}

	private static BitmapImage BitmapToBitmapImage(Bitmap bitmap, int? width = null, BitmapImage image = null)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		Bitmap bitmap2 = new Bitmap(bitmap.Width, bitmap.Height);
		for (int i = 0; i < bitmap.Width; i++)
		{
			for (int j = 0; j < bitmap.Height; j++)
			{
				System.Drawing.Color pixel = bitmap.GetPixel(i, j);
				System.Drawing.Color color = System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B);
				bitmap2.SetPixel(i, j, color);
			}
		}
		MemoryStream ms = new MemoryStream();
		bitmap2.Save(ms, ImageFormat.Bmp);
		stopwatch.Stop();
		bool init = false;
		System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
		{
			try
			{
				image = image ?? new BitmapImage();
				image.BeginInit();
				image.StreamSource = new MemoryStream(ms.ToArray());
				if (width.HasValue && bitmap.Width > width.Value)
				{
					image.DecodePixelWidth = width.Value;
				}
				image.EndInit();
				image.Freeze();
			}
			finally
			{
				init = true;
			}
		});
		while (!init)
		{
			Thread.Sleep(100);
		}
		ms.Close();
		ms.Dispose();
		return image;
	}

	private async void TryDownLoadUrlImage(string url, string filePath)
	{
		new Thread((ThreadStart)async delegate
		{
			try
			{
				byte[] buffer = new WebClient().DownloadData(url);
				BitmapImage bitmapImage = new BitmapImage();
				using (MemoryStream streamSource = new MemoryStream(buffer))
				{
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					bitmapImage.Freeze();
				}
				if (!bitmapImage.IsDownloading && bitmapImage.Height > 1.0 && bitmapImage.Width > 1.0)
				{
					BitmapEncoder bitmapEncoder = new PngBitmapEncoder
					{
						Frames = { BitmapFrame.Create(bitmapImage) }
					};
					using FileStream fileStream = new FileStream(filePath, FileMode.Create);
					bitmapEncoder.Save(fileStream);
					fileStream.Close();
					return;
				}
			}
			catch (NotSupportedException)
			{
			}
			catch (Exception)
			{
			}
		}).Start();
	}

	public static void GetPicFromControl(FrameworkElement element, string type, string outputPath)
	{
		RenderTargetBitmap renderTargetBitmap = null;
		try
		{
			int num = ((element.ActualHeight > 15000.0) ? 1 : 2);
			renderTargetBitmap = new RenderTargetBitmap((int)element.ActualWidth * num, (int)(element.ActualHeight + 100.0) * num, 96 * num, 96 * num, PixelFormats.Pbgra32);
			renderTargetBitmap.Render(element);
			BitmapEncoder bitmapEncoder = type.ToUpper() switch
			{
				"BMP" => new BmpBitmapEncoder(), 
				"GIF" => new GifBitmapEncoder(), 
				"JPEG" => new JpegBitmapEncoder(), 
				"PNG" => new PngBitmapEncoder(), 
				"TIFF" => new TiffBitmapEncoder(), 
				_ => new PngBitmapEncoder(), 
			};
			if (!string.IsNullOrEmpty(outputPath))
			{
				bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
				string directoryName = System.IO.Path.GetDirectoryName(outputPath);
				if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				FileStream fileStream = File.Create(outputPath);
				bitmapEncoder.Save(fileStream);
				fileStream.Close();
				renderTargetBitmap.Clear();
			}
		}
		catch (Exception)
		{
			renderTargetBitmap?.Clear();
		}
	}

	public static Geometry GetIcon(string index)
	{
		try
		{
			return (System.Windows.Application.Current?.FindResource(index) as System.Windows.Shapes.Path)?.Data;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static Geometry GetGeometry(string key)
	{
		try
		{
			return System.Windows.Application.Current?.FindResource(key) as Geometry;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string GetString(string key, string defaultText = null)
	{
		object obj = System.Windows.Application.Current?.TryFindResource(key);
		if (obj != null)
		{
			return obj.ToString();
		}
		return defaultText ?? key;
	}

	public static string GetStringFormat(string key, params object[] args)
	{
		string text = GetString(key);
		if (text != key)
		{
			return string.Format(text, args);
		}
		return text;
	}

	public static bool IsNetworkAvailable()
	{
		try
		{
			return CheckNet();
		}
		catch (Exception)
		{
			return true;
		}
	}

	[DllImport("wininet.dll")]
	private static extern bool InternetGetConnectedState(out int description, int reservedValue);

	private static bool CheckNet()
	{
		int description;
		return InternetGetConnectedState(out description, 0);
	}

	public static string BuildReminderString(int num, string unit, string time = "")
	{
		return string.Format(GetString("ReminderTemplate"), num, unit, time);
	}

	public static bool IsEmptyDate(DateTime date)
	{
		return date.Year == 1;
	}

	public static long GetTimeStamp(DateTime? time)
	{
		if (!time.HasValue)
		{
			return 0L;
		}
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
		return (long)(time.Value - dateTime).TotalMilliseconds;
	}

	public static long GetTimeStampInSecond(DateTime? time)
	{
		if (!time.HasValue)
		{
			return 0L;
		}
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
		return (long)(time.Value - dateTime).TotalSeconds;
	}

	public static bool IsEmptyDate(DateTime? date)
	{
		if (date.HasValue)
		{
			return date.Value.Year == 1;
		}
		return true;
	}

	public static string GetSafeDateLogText(DateTime? date)
	{
		if (!date.HasValue)
		{
			return null;
		}
		return GetSafeDateLogText(date.Value);
	}

	public static string GetSafeDateLogText(DateTime date)
	{
		try
		{
			return date.ToString(CultureInfo.InvariantCulture);
		}
		catch (ArgumentOutOfRangeException)
		{
			return date.Ticks.ToString(CultureInfo.InvariantCulture);
		}
	}

	public static async Task<List<ProjectUsersMode>> GetRecentProjectUsersList()
	{
		string value = await Communicator.GetRecentProjectUsersList();
		if (!string.IsNullOrEmpty(value))
		{
			try
			{
				return JsonConvert.DeserializeObject<List<ProjectUsersMode>>(value);
			}
			catch (Exception)
			{
			}
		}
		return new List<ProjectUsersMode>();
	}

	public static string ToVersion(double version)
	{
		string text = version.ToString(CultureInfo.InvariantCulture);
		string text2 = string.Empty;
		string text3 = text;
		for (int i = 0; i < text3.Length; i++)
		{
			text2 = text2 + text3[i] + ".";
		}
		if (text2.EndsWith("."))
		{
			text2 = text2.Substring(0, text2.Length - 1);
		}
		return text2;
	}

	public static double VersionNumToDouble()
	{
		return double.Parse(System.Windows.Application.ResourceAssembly.GetName().Version.Major.ToString() + System.Windows.Application.ResourceAssembly.GetName().Version.Minor + System.Windows.Application.ResourceAssembly.GetName().Version.Build + System.Windows.Application.ResourceAssembly.GetName().Version.Revision);
	}

	public static BitmapImage GetImage(string uri)
	{
		BitmapImage bitmapImage = new BitmapImage();
		try
		{
			string uriString = RemoveControlCharacters(uri);
			bitmapImage.BeginInit();
			bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
			bitmapImage.UriSource = new Uri(uriString);
			bitmapImage.EndInit();
			return bitmapImage;
		}
		catch (NotSupportedException)
		{
		}
		catch (Exception)
		{
		}
		return null;
	}

	private static string RemoveControlCharacters(string inString)
	{
		if (inString == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in inString)
		{
			char c2 = (char)(0xFF & c);
			if (c2 != ' ' && c2 != '\r' && c2 != '\t' && c2 != '\n' && c2 != '\u007f')
			{
				stringBuilder.Append(c2);
			}
		}
		return stringBuilder.ToString();
	}

	public static Tuple<long, long> GetResidueAttachmentCount(int nextUploadCount = 0)
	{
		long userLimit = GetUserLimit(Constants.LimitKind.DailyUploadNumber);
		int num = AttachmentCache.TodayAttachmentCount();
		return new Tuple<long, long>(userLimit - num, userLimit);
	}

	public static string GetDeviceInfo()
	{
		string text = "Windows_" + GetWindowsVersion();
		string machineName = Environment.MachineName;
		machineName = HttpUtility.UrlEncode(machineName);
		return JsonConvert.SerializeObject(new DeviceInfoModel
		{
			platform = "windows",
			os = text,
			name = machineName,
			version = GetVersion(),
			id = (App.UId ?? GetGuid0()),
			channel = "website",
			device = text + "x64"
		});
	}

	public static void OutPutJson(object obj)
	{
		JsonTextWriter jsonWriter = new JsonTextWriter(new StringWriter())
		{
			Formatting = Formatting.Indented,
			Indentation = 4,
			IndentChar = '\u00a0'
		};
		new JsonSerializer().Serialize(jsonWriter, obj);
	}

	public static string GetLocalTimeZone()
	{
		if (!string.IsNullOrEmpty(LocalTimeZone))
		{
			return LocalTimeZone;
		}
		LocalTimeZone = TimeZoneUtils.GetTimeZoneName(TimeZoneInfo.Local);
		return LocalTimeZone;
	}

	public static Visual GetDescendantByType(Visual element, Type type, string name)
	{
		if (element == null)
		{
			return null;
		}
		if (element.GetType() == type && element is FrameworkElement frameworkElement && frameworkElement.Name == name)
		{
			return frameworkElement;
		}
		Visual visual = null;
		(element as FrameworkElement)?.ApplyTemplate();
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			visual = GetDescendantByType(VisualTreeHelper.GetChild(element, i) as Visual, type, name);
			if (visual != null)
			{
				break;
			}
		}
		return visual;
	}

	public static bool IsTickPackage()
	{
		return false;
	}

	public static bool IsDida()
	{
		return true;
	}

	public static bool IsCn(string ci = null)
	{
		ci = (string.IsNullOrEmpty(ci) ? App.Ci.ToString() : ci);
		if (!(ci == "zh-CN"))
		{
			return ci == "zh-TW";
		}
		return true;
	}

	public static bool IsEn()
	{
		return App.Ci.ToString() == "en-US";
	}

	public static bool IsKo()
	{
		return App.Ci.ToString() == "ko-KR";
	}

	public static bool IsPt()
	{
		return App.Ci.ToString() == "pt-BR";
	}

	public static bool IsZhCn()
	{
		return App.Ci.ToString() == "zh-CN";
	}

	public static bool IsJp()
	{
		return "ja-JP".Equals(App.Ci.ToString());
	}

	public static bool IsRussia()
	{
		return "ru-RU".Equals(App.Ci.ToString());
	}

	public static bool IsShowHoliday()
	{
		if (!IsJp())
		{
			return IsDida();
		}
		return true;
	}

	public static IToastShowWindow GetToastWindow()
	{
		return ToastWindow ?? App.Window;
	}

	public static void Toast(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			GetToastWindow()?.TryToastString(null, message);
		}
	}

	public static void ToastInMainWindow(string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			App.Window?.TryToastString(null, message);
		}
	}

	public static Constants.SortType LoadSmartProjectSortType(string index)
	{
		Enum.TryParse<Constants.SortType>(LocalSettings.Settings[index].ToString(), out var result);
		return result;
	}

	public static Constants.SortType LoadSortType(string index)
	{
		Enum.TryParse<Constants.SortType>(index, out var result);
		return result;
	}

	public static async Task StartUpgradeOnWeb(string type = null)
	{
		List<string> allGroups = ABTestManager.GetAllGroups();
		TryProcessStartUrlWithToken("/about/upgrade?" + (string.IsNullOrEmpty(type) ? string.Empty : ("from=" + type + "&")) + "platform=windows&did=" + App.UId + "&group=" + allGroups.Join(","));
	}

	public static async Task StartUpgradeOnWebV2(string method = "paypal_subscribe", string type = "settings", string freq = "", string count = "")
	{
		List<string> allGroups = ABTestManager.GetAllGroups();
		TryProcessStartUrlWithToken("/payment/app/" + method + "?" + (string.IsNullOrEmpty(type) ? string.Empty : ("from=" + type + "&")) + "platform=windows&did=" + App.UId + "&freq=" + freq + "&count=" + count + "&group=" + allGroups.Join(","));
	}

	public static async Task StartUpgrade(string type, Window parent = null, bool toSettings = false)
	{
		UserModel user = await UserDao.QueryUserModelListDbByIdAsync(LocalSettings.Settings.LoginUserId);
		if (user.pro)
		{
			try
			{
				parent?.Close();
			}
			catch (Exception)
			{
			}
			if (toSettings)
			{
				SettingDialog.ShowSettingDialog(SettingsType.Premium, App.Window);
				return;
			}
		}
		else if (user.errorType == 1)
		{
			string text = GetString("AvoidDuplicateSubscriptionContent");
			if (App.Ci.TwoLetterISOLanguageName == "zh")
			{
				text = text.Replace(" {0} ", "{0}");
			}
			IconTextButtonWindow obj = new IconTextButtonWindow("", "DuplicateSubsDrawingImage", string.Format(text, GetString("ManageSubscriptions")), GetString("ManageSubscriptions"), async delegate
			{
				if (user != null && user.subscribeType != "order")
				{
					ManageProSubscribeWindow manageProSubscribeWindow = new ManageProSubscribeWindow(await Communicator.GetSubscribeList());
					manageProSubscribeWindow.Owner = parent ?? App.Window;
					manageProSubscribeWindow.ShowDialog();
				}
				else
				{
					TryProcessStartUrlWithToken("webapp/#settings/premium");
				}
			})
			{
				Owner = (parent ?? App.Window)
			};
			obj.Owner?.Activate();
			obj.Show();
			return;
		}
		PerformUpgrade(type, parent);
	}

	private static async Task PerformUpgrade(string type, Window parent = null)
	{
		_ = 1;
		try
		{
			PayProWindow payProWindow = new PayProWindow(type, BuildPayModel(await Communicator.GetOrderSpecList(), await Communicator.GetSubscribeSpecList()))
			{
				Owner = (parent ?? App.Window)
			};
			if (!(parent is UpgradeProDialog))
			{
				parent = null;
			}
			if (parent != null)
			{
				parent.Opacity = 0.0;
			}
			payProWindow.ShowDialog();
			if (parent != null)
			{
				if (payProWindow.PayClick)
				{
					parent.Close();
					parent.Owner?.Activate();
				}
				else
				{
					parent.Opacity = 1.0;
				}
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
			StartUpgradeOnWeb(type);
			try
			{
				parent?.Close();
			}
			catch
			{
			}
		}
	}

	private static PayViewModel BuildPayModel(IReadOnlyCollection<OrderSpecModel> orderSpecs, IReadOnlyCollection<SubscribeSpecModel> subscriptionSpecs)
	{
		return new PayViewModel
		{
			OrderByMonth = orderSpecs.FirstOrDefault((OrderSpecModel spec) => spec.unit == "month")?.amount,
			OrderByYear = orderSpecs.FirstOrDefault((OrderSpecModel spec) => spec.unit == "year")?.amount,
			AlipaySubsMonthly = subscriptionSpecs.FirstOrDefault((SubscribeSpecModel spec) => spec.freq == "monthly" && spec.type == "alipay")?.price,
			StripeSubsMonthly = subscriptionSpecs.FirstOrDefault((SubscribeSpecModel spec) => spec.freq == "monthly" && spec.type == "stripe")?.price,
			StripeSubsYearly = subscriptionSpecs.FirstOrDefault((SubscribeSpecModel spec) => spec.freq == "yearly" && spec.type == "stripe")?.price,
			PaypalSubsMonthly = subscriptionSpecs.FirstOrDefault((SubscribeSpecModel spec) => spec.freq == "monthly" && spec.type == "paypal")?.price,
			PaypalSubsYearly = subscriptionSpecs.FirstOrDefault((SubscribeSpecModel spec) => spec.freq == "yearly" && spec.type == "paypal")?.price
		};
	}

	public static async Task<string> GetAutoSignUrl(string destApi)
	{
		string text = await Communicator.GetSignOnToken();
		return BaseUrl.GetDomainUrl() + "/sign/autoSignOn?token=" + text + "&dest=" + UrlEncode0(destApi);
	}

	public static async Task TryProcessStartUrlWithToken(string destApi)
	{
		TryProcessStartUrl(await GetAutoSignUrl(destApi));
	}

	public static async void TryProcessToNotionConnect(string calendarId)
	{
		if (!string.IsNullOrEmpty(calendarId))
		{
			TryProcessStartUrlWithToken(BaseUrl.GetDomainUrl() + "/connect/notion/config/" + calendarId + "?channel=windows");
		}
		else
		{
			TryProcessStartUrlWithToken((await Communicator.NeedShowNotionPage()) ? ("/connect/notion/guide?channel=windows&lang=" + GetLanguage().ToLower() + "&hideBind=false") : GetConnectNotionUrl());
		}
	}

	public static async void TryProcessStartUrl(string url)
	{
		try
		{
			Process.Start(url);
		}
		catch (Exception)
		{
			try
			{
				Process.Start("IExplore.exe", url);
			}
			catch (Exception)
			{
			}
		}
	}

	public static void GotoPaySubscriptionHelp(bool isApple)
	{
		TryProcessStartUrl("https://support.apple.com/zh-cn/108095");
	}

	public static string GetInboxId()
	{
		return "inbox" + LocalSettings.Settings.LoginUserId;
	}

	public static bool IsEmptyRepeatFlag(string flag)
	{
		if (!string.IsNullOrEmpty(flag) && !(flag == "FREQ=NONE"))
		{
			return flag == "RRULE:FREQ=NONE";
		}
		return true;
	}

	public static SolidColorBrush GetPriorityColor(int value, ResourceDictionary dict = null)
	{
		return value switch
		{
			0 => ThemeUtil.GetColor("BaseColorOpacity40", dict), 
			1 => ThemeUtil.GetColor("PriorityLowColor"), 
			3 => ThemeUtil.GetColor("PriorityMiddleColor"), 
			5 => ThemeUtil.GetColor("PriorityHighColor"), 
			_ => ThemeUtil.GetColor("BaseColorOpacity40", dict), 
		};
	}

	public static string GetPriorityName(int value)
	{
		return value switch
		{
			0 => GetString("PriorityNull"), 
			1 => GetString("PriorityLow"), 
			3 => GetString("PriorityMedium"), 
			5 => GetString("PriorityHigh"), 
			_ => GetString("PriorityNull"), 
		};
	}

	public static bool IfShiftPressed()
	{
		if (!Keyboard.IsKeyDown(Key.LeftShift))
		{
			return Keyboard.IsKeyDown(Key.RightShift);
		}
		return true;
	}

	public static bool IfCtrlPressed()
	{
		if (!Keyboard.IsKeyDown(Key.LeftCtrl))
		{
			return Keyboard.IsKeyDown(Key.RightCtrl);
		}
		return true;
	}

	public static bool IfAltPressed()
	{
		if (!Keyboard.IsKeyDown(Key.LeftAlt))
		{
			return Keyboard.IsKeyDown(Key.RightAlt);
		}
		return true;
	}

	public static bool IfWinPressed()
	{
		if (!Keyboard.IsKeyDown(Key.LWin))
		{
			return Keyboard.IsKeyDown(Key.RWin);
		}
		return true;
	}

	public static bool IfModifierKeyDown()
	{
		return Keyboard.Modifiers != ModifierKeys.None;
	}

	public static System.Windows.Controls.ListViewItem GetListViewItem(System.Windows.Controls.ListView listview, int index)
	{
		try
		{
			if (index < 0 || index >= listview.Items.Count)
			{
				return null;
			}
			if (listview.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
			{
				return null;
			}
			DependencyObject dependencyObject = listview.ItemContainerGenerator.ContainerFromIndex(index);
			if (dependencyObject == null)
			{
				listview.UpdateLayout();
				dependencyObject = listview.ItemContainerGenerator.ContainerFromIndex(index);
			}
			return dependencyObject as System.Windows.Controls.ListViewItem;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string RemoveLastFlag(string data, string flag)
	{
		if (!string.IsNullOrEmpty(data) && data.EndsWith(flag))
		{
			return data.Substring(0, data.Length - flag.Length);
		}
		return data;
	}

	public static object GetBoolText(object value, string positiveKey, string negtiveKey, string defaultKey)
	{
		if (value is bool)
		{
			return GetString(((bool)value) ? positiveKey : negtiveKey);
		}
		return GetString(defaultKey);
	}

	public static bool IsPointInTriangle(System.Windows.Point p, System.Windows.Point p1, System.Windows.Point p2, System.Windows.Point p3)
	{
		Vector vector = p1 - p;
		Vector vector2 = p2 - p;
		Vector vector3 = p3 - p;
		double num = vector.X * vector2.Y - vector.Y * vector2.X;
		double num2 = vector2.X * vector3.Y - vector2.Y * vector3.X;
		double num3 = vector3.X * vector.Y - vector3.Y * vector.X;
		if (num * num2 >= 0.0)
		{
			return num2 * num3 >= 0.0;
		}
		return false;
	}

	private static bool IsPointInRect(double x, double y, double width, double height)
	{
		if (x >= 0.0 && x <= width && y >= 0.0)
		{
			return y <= height;
		}
		return false;
	}

	public static bool IsMouseOver(System.Windows.Input.MouseEventArgs arg, FrameworkElement element)
	{
		double x = arg.GetPosition(element).X;
		double y = arg.GetPosition(element).Y;
		double actualWidth = element.ActualWidth;
		double actualHeight = element.ActualHeight;
		return IsPointInRect(x, y, actualWidth, actualHeight);
	}

	public static bool IsRectInRect(double pivotX, double pivotY, double left, double top, double right, double bottom, double width, double height)
	{
		double x = pivotX - left;
		double y = pivotY - top;
		double x2 = pivotX + right;
		double y2 = pivotY + bottom;
		if (IsPointInRect(x, y, width, height))
		{
			return IsPointInRect(x2, y2, width, height);
		}
		return false;
	}

	private static bool IsEmptyPoint(System.Windows.Point point)
	{
		if (Math.Abs(point.X) <= 0.0)
		{
			return Math.Abs(point.Y) <= 0.0;
		}
		return false;
	}

	public static bool CheckMoveDelta(FrameworkElement element, System.Windows.Input.MouseEventArgs e, System.Windows.Point start, int delta)
	{
		if (IsEmptyPoint(start))
		{
			return false;
		}
		double value = e.GetPosition(element).X - start.X;
		double value2 = e.GetPosition(element).Y - start.Y;
		if (Math.Abs(value) >= (double)delta || Math.Abs(value2) >= (double)delta)
		{
			return true;
		}
		return false;
	}

	public static Thickness GetPopupMargin(System.Windows.Input.MouseEventArgs arg, FrameworkElement element, bool modelDayMode = false, double verticalOffset = 0.0)
	{
		if (modelDayMode)
		{
			return new Thickness(20.0, 0.0, 20.0, 0.0);
		}
		System.Windows.Point position = arg.GetPosition(element);
		return new Thickness(position.X, position.Y, element.ActualWidth - position.X + 4.0, 0.0);
	}

	public static string GetVersion(string split = "")
	{
		return System.Windows.Application.ResourceAssembly.GetName().Version.Major + split + System.Windows.Application.ResourceAssembly.GetName().Version.Minor + split + System.Windows.Application.ResourceAssembly.GetName().Version.Build + split + System.Windows.Application.ResourceAssembly.GetName().Version.Revision;
	}

	public static DateTime GetCalendarMonthViewStartDate(DateTime time)
	{
		if (IsEmptyDate(time))
		{
			time = DateTime.Today;
		}
		DateTime weekStart = GetWeekStart(time.AddDays(1 - time.Day));
		if (weekStart.Month == time.Month && weekStart.Day != 1)
		{
			return weekStart.AddDays(-7.0);
		}
		return weekStart;
	}

	public static DateTime GetWeekStart(DateTime date)
	{
		int num = (7 + date.DayOfWeek - LocalSettings.Settings.WeekStartFromType) % 7;
		return date.AddDays(-num);
	}

	public static async Task<PomodoroSummaryModel[]> CopyPomoSummaries(TaskModel repeatTask, string oldTaskId, bool clear = false)
	{
		List<PomodoroSummaryModel> copies = (from item in (await PomoSummaryDao.GetPomosByTaskId(oldTaskId)).ToArray()
			select item.Copy(repeatTask.id, clear)).ToList();
		await PomoSummaryDao.SavePomoSummaries(copies);
		return copies.ToArray();
	}

	public static int SecondToMinute(long second)
	{
		return (int)(second / 60 + ((second % 60 >= 30) ? 1 : 0));
	}

	public static string GetDurationString(long duration, bool isShort = false, bool showZero = false, bool showSpan = true, bool isLong = false)
	{
		string text = (isShort ? "" : (showSpan ? " " : (IsCn() ? "" : " ")));
		duration = SecondToMinute(duration);
		long num = duration / 60;
		long num2 = duration % 60;
		return ((num > 0) ? (num + text + (isShort ? "h" : GetString((!isLong) ? ((num > 1) ? "PublicHours" : "PublicHour") : ((num > 1) ? "UpHours" : "UpHour")).ToLower())) : "") + ((num2 > 0 || (num == 0 && showZero)) ? (((num > 0 && !isShort) ? " " : "") + num2 + text + (isShort ? "m" : GetString((num2 > 1) ? "PublicMinutes" : "PublicMinute").ToLower())) : "");
	}

	public static string GetShortDurationString(long duration, bool showZeroMinute = true)
	{
		long num = duration / 3600;
		long num2 = duration / 60 % 60;
		string text = ((num > 0) ? (num + "h") : "");
		if (num2 != 0L || num == 0 || showZeroMinute)
		{
			text = text + num2 + "m";
		}
		return text;
	}

	public static Window GetParentWindow(DependencyObject child)
	{
		if (child == null)
		{
			return null;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(child);
		if (parent == null)
		{
			return null;
		}
		if (parent is Window result)
		{
			return result;
		}
		return GetParentWindow(parent);
	}

	public static bool CheckIfAddText(TextChangedEventArgs textChange)
	{
		if (textChange.Changes != null && textChange.Changes.Any((TextChange change) => change.AddedLength > 0 && change.RemovedLength == 0))
		{
			return true;
		}
		return false;
	}

	public static int GetCurrentUserIdInt()
	{
		int.TryParse(LocalSettings.Settings.LoginUserId, out var result);
		return result;
	}

	public static string GetCurrentUserStr()
	{
		return LocalSettings.Settings.LoginUserId;
	}

	public static int CompareChunks(List<TitleChunk> leftChunks, List<TitleChunk> rightChunks)
	{
		int num = Math.Min(leftChunks.Count, rightChunks.Count);
		for (int i = 0; i < num; i++)
		{
			TitleChunk titleChunk = leftChunks[i];
			TitleChunk titleChunk2 = rightChunks[i];
			int num2 = 0;
			if (!titleChunk.IsNum || !titleChunk2.IsNum)
			{
				if (string.IsNullOrEmpty(titleChunk.Text) && string.IsNullOrEmpty(titleChunk2.Text))
				{
					return 0;
				}
				if (string.IsNullOrEmpty(titleChunk.Text))
				{
					return 1;
				}
				if (string.IsNullOrEmpty(titleChunk2.Text))
				{
					return -1;
				}
				num2 = string.Compare(titleChunk.Text, titleChunk2.Text, StringComparison.CurrentCulture);
				if (num2 != 0)
				{
					return num2;
				}
			}
			else
			{
				num2 = titleChunk.Num.CompareTo(titleChunk2.Num);
				if (num2 != 0)
				{
					return num2;
				}
				int result = titleChunk2.RealLength.CompareTo(titleChunk.RealLength);
				if (titleChunk.RealLength != titleChunk2.RealLength)
				{
					return result;
				}
			}
		}
		return leftChunks.Count.CompareTo(rightChunks.Count);
	}

	public static int CompareChunksDesc(List<TitleChunk> leftChunks, List<TitleChunk> rightChunks)
	{
		int num = Math.Min(leftChunks.Count, rightChunks.Count);
		for (int i = 0; i < num; i++)
		{
			TitleChunk titleChunk = leftChunks[i];
			TitleChunk titleChunk2 = rightChunks[i];
			int num2 = 0;
			if (!titleChunk.IsNum || !titleChunk2.IsNum)
			{
				if (string.IsNullOrEmpty(titleChunk.Text) && string.IsNullOrEmpty(titleChunk2.Text))
				{
					return 0;
				}
				if (string.IsNullOrEmpty(titleChunk.Text))
				{
					return 1;
				}
				if (string.IsNullOrEmpty(titleChunk2.Text))
				{
					return -1;
				}
				num2 = string.Compare(titleChunk.Text, titleChunk2.Text, StringComparison.CurrentCulture);
				if (num2 != 0)
				{
					return -num2;
				}
			}
			else
			{
				num2 = titleChunk.Num.CompareTo(titleChunk2.Num);
				if (num2 != 0)
				{
					return -num2;
				}
				int num3 = titleChunk2.RealLength.CompareTo(titleChunk.RealLength);
				if (titleChunk.RealLength != titleChunk2.RealLength)
				{
					return -num3;
				}
			}
		}
		return rightChunks.Count.CompareTo(leftChunks.Count);
	}

	public static T FindParent<T>(DependencyObject child) where T : class
	{
		if (child == null)
		{
			return null;
		}
		DependencyObject dependencyObject;
		if (child is ContentElement contentElement)
		{
			dependencyObject = ContentOperations.GetParent(contentElement) ?? (contentElement as FrameworkContentElement)?.Parent;
		}
		else
		{
			if (!(child is Visual) && !(child is Visual3D))
			{
				return null;
			}
			dependencyObject = VisualTreeHelper.GetParent(child);
		}
		if (dependencyObject == null)
		{
			return null;
		}
		if (dependencyObject is T)
		{
			return dependencyObject as T;
		}
		return FindParent<T>(dependencyObject);
	}

	public static T FindSingleVisualChildren<T>(DependencyObject parentObj) where T : class
	{
		T val = null;
		if (parentObj != null)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parentObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parentObj, i);
				if (child is T val2)
				{
					val = val2;
					break;
				}
				val = FindSingleVisualChildren<T>(child);
				if (val != null)
				{
					break;
				}
			}
		}
		return val;
	}

	public static void CleanAfterLogout()
	{
		SearchHelper.ClearTaskSearchModels();
		WindowManager.AppLockOrExit = true;
		App.ClearReminders();
		App.ClearCountdown();
		ClosedListSyncService.ClearLoadDict();
		ProjectWidgetsHelper.CloseAll();
		TaskStickyWindow.CloseAllStickies();
		IndependentWindow.CloseAll();
		AIWindow.HideDesktopWidget();
		CalendarWidgetHelper.CloseWidget();
		CountdownWidgetsHelper.CloseAll();
		HabitWidgetHelper.CloseAll();
		CacheManager.Clear();
		TaskViewModelHelper.ClearModels();
		ProjectAndTaskIdsCache.Clear();
		SearchProjectHelper.Clear();
		UserManager.Clear();
		TaskDetailWindows.CloseAll();
		AvatarHelper.Clear();
		SyncManager.Clear();
		ScheduleService.Clear();
		SystemToastUtils.ClearToastHistory();
		TickFocusManager.Clear();
		WebSocketService.DisposeWs();
		InviteHelper.Clear();
		TrashSyncService.Reset();
		HabitCheckinCache.Clear();
		MainWindow.NewVersionReminderWindow?.Close();
		AICompleteWindow.Window?.Close();
		AIRecommendWindow.Window?.Close();
		AIRecommendService.lastCTime = (AIRecommendService.lastMTime = default(DateTime));
		App.Window?.TryStopCalendarProTimer();
		MessageInfoWindow.Window?.Close();
	}

	public static System.Windows.Point GetLocationSafely(System.Windows.Point location, double width, double height, System.Windows.Point defaultLocation, double rate)
	{
		Screen[] allScreens = Screen.AllScreens;
		int num = (int)(location.X / rate);
		int y = (int)(location.Y / rate);
		int x = (int)((double)num + width / rate);
		Screen[] array = allScreens;
		foreach (Screen screen in array)
		{
			if (screen.WorkingArea.Contains(new System.Drawing.Point(num, y)) || screen.WorkingArea.Contains(new System.Drawing.Point(x, y)))
			{
				return location;
			}
		}
		return defaultLocation;
	}

	public static System.Windows.Point GetPopupOffset(Window context, FrameworkElement target, double hoffset, double voffset)
	{
		if (context == null)
		{
			context = App.Window;
		}
		System.Windows.Point point = new System.Windows.Point(0.0, 0.0);
		try
		{
			point = target.TransformToAncestor(context).Transform(new System.Windows.Point(0.0, 0.0));
		}
		catch (Exception ex)
		{
			try
			{
				context = GetParentWindow(target);
				point = target.TransformToAncestor(context).Transform(new System.Windows.Point(0.0, 0.0));
			}
			catch (Exception)
			{
				System.Windows.Forms.MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
		double num = 0.0;
		double num2 = 0.0;
		if (context != null)
		{
			num = context.Left;
			num2 = context.Top;
			if (context.WindowState == WindowState.Maximized)
			{
				num = -6.0;
				num2 = -6.0;
			}
		}
		double x = point.X + num + hoffset - 22.0;
		double y = point.Y + num2 + voffset;
		return new System.Windows.Point(x, y);
	}

	public static bool IsWindows7()
	{
		if (Environment.OSVersion.Version.Major == 6)
		{
			return true;
		}
		return false;
	}

	public static bool IsWindows11()
	{
		int major = Environment.OSVersion.Version.Major;
		int build = Environment.OSVersion.Version.Build;
		if (major == 10)
		{
			return build >= 22000;
		}
		return false;
	}

	public static string GetWindowsVersion()
	{
		int major = Environment.OSVersion.Version.Major;
		int minor = Environment.OSVersion.Version.Minor;
		int build = Environment.OSVersion.Version.Build;
		if (major == 6)
		{
			switch (minor)
			{
			case 1:
				return "7";
			case 2:
				return "8";
			case 3:
				return "8.1";
			}
		}
		if (major == 10 && build >= 22000)
		{
			return "11";
		}
		if (major != 10)
		{
			return "unknown";
		}
		return "10";
	}

	public static string Base64Encode(string plainText)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
	}

	public static string Base64Decode(string base64EncodedData)
	{
		try
		{
			byte[] bytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	public static void InitBaseEvents(Window window, Func<string, DependencyObject> getTemplateChild)
	{
		if (getTemplateChild("CloseButton") is UIElement uIElement)
		{
			uIElement.MouseLeftButtonUp += delegate
			{
				window.Close();
			};
		}
		InitDragEvents(window, getTemplateChild);
	}

	public static void InitDragEvents(Window window, Func<string, DependencyObject> getTemplateChild)
	{
		if (!(getTemplateChild("DragGrid") is Grid grid))
		{
			return;
		}
		grid.PreviewMouseDown += delegate
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				window.DragMove();
			}
		};
	}

	public static void ResetPassword(string username)
	{
		TryProcessStartUrl(BaseUrl.GetDomainUrl() + "/sign/requestRestPassword?username=" + username);
	}

	public static List<TaskReminderModel> BuildReminders(IReadOnlyCollection<string> reminders)
	{
		List<TaskReminderModel> list = new List<TaskReminderModel>();
		if (reminders != null && reminders.Count > 0)
		{
			list.AddRange(from reminder in reminders
				where !string.IsNullOrEmpty(reminder)
				select new TaskReminderModel
				{
					trigger = reminder,
					id = GetGuid()
				});
		}
		return list;
	}

	public static async Task<ReleaseNoteModel> GetReleaseNote()
	{
		Stopwatch watch = Stopwatch.StartNew();
		string url = BaseUrl.GetReleaseNoteUrl();
		try
		{
			return await ApiClient.GetJsonAsync<ReleaseNoteModel>(url);
		}
		catch (Exception arg)
		{
			UtilLog.Warn($"GetReleaseNote failed, url={url}, elapsedMs={watch.ElapsedMilliseconds}, ex={arg}");
			return null;
		}
	}

	public static string EscapeReg(string original)
	{
		return original.Replace("?", ".").Replace("//", "..").Replace(":", ".")
			.Replace("(", ".")
			.Replace(")", ".")
			.Replace("+", ".")
			.Replace("$", ".")
			.Replace("\\", ".")
			.Replace("[", ".")
			.Replace("]", ".")
			.Replace("^", ".")
			.Replace("*", ".")
			.Replace("|", ".");
	}

	public static string HandleReg(string original)
	{
		return original.Replace("\\", "\\\\").Replace("?", "\\?").Replace("//", "\\/\\/")
			.Replace(":", "\\:")
			.Replace("(", "\\(")
			.Replace(")", "\\)")
			.Replace("+", "\\+")
			.Replace("\\$", "\\$")
			.Replace("[", "\\[")
			.Replace("]", "\\]")
			.Replace("^", "\\^")
			.Replace("*", "\\*")
			.Replace(".", "\\.");
	}

	public static bool ShowBatchAdd(string text)
	{
		return text.Split('\n').Count((string line) => !string.IsNullOrEmpty(line.Replace("\r", string.Empty).Replace("\n", string.Empty))) > 1;
	}

	public static bool IsMobilePhone(string input)
	{
		return new Regex("^1\\d{10}$").IsMatch(input);
	}

	public static string NormalizeMainlandPhone(string phone)
	{
		if (string.IsNullOrWhiteSpace(phone) || phone.Contains("*"))
		{
			return string.Empty;
		}
		string text = Regex.Replace(phone.Trim(), "[\\s\\-()]", string.Empty);
		if (text.StartsWith("+86", StringComparison.Ordinal))
		{
			text = text.Substring(3);
		}
		else if (text.StartsWith("86", StringComparison.Ordinal) && text.Length == 13)
		{
			text = text.Substring(2);
		}
		if (!IsMobilePhone(text))
		{
			return string.Empty;
		}
		return text;
	}

	public static string GetMaskedMainlandPhone(string phone)
	{
		if (string.IsNullOrWhiteSpace(phone))
		{
			return string.Empty;
		}
		string text = phone.Trim();
		Match match = Regex.Match(text, "^(?:\\+?86[\\s-]?)?(1\\d{2})\\*{4}(\\d{4})$");
		if (match.Success)
		{
			return match.Groups[1].Value + "****" + match.Groups[2].Value;
		}
		string text2 = NormalizeMainlandPhone(text);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2.Substring(0, 3) + "****" + text2.Substring(7);
		}
		return string.Empty;
	}

	public static bool IsValidEmail(string email)
	{
		return new Regex("\\w+([-+.]\\w+)*@([a-z0-9A-Z.-]+(-[a-z0-9A-Z]+)?\\.)+[a-zA-Z]{2,}$").IsMatch(email);
	}

	public static bool IsFakeEmail(string email)
	{
		string hostName = BaseUrl.GetHostName();
		return email?.Contains(hostName) ?? false;
	}

	public static string GetMaskPhone(string phone)
	{
		if (!string.IsNullOrEmpty(phone) && phone.StartsWith("86 "))
		{
			phone = phone.Substring(3);
			if (phone.Length == 11)
			{
				return phone.Substring(0, 3) + "****" + phone.Substring(7);
			}
		}
		return string.Empty;
	}

	public static void ReplaceFirst(ref string name, string old, string newStr)
	{
		int startIndex = name.IndexOf(old, StringComparison.Ordinal);
		name = name.Remove(startIndex, old.Length).Insert(startIndex, newStr);
	}

	public static void DebugToast(string message)
	{
	}

	public static void PlayCompletionSound()
	{
		if (!PomoSoundHelper.IsPomoSoundPlaying())
		{
			string completionSound = LocalSettings.Settings.CompletionSound;
			if (!(completionSound == "None"))
			{
				PlaySound(AppDomain.CurrentDomain.BaseDirectory + "completion_sound_" + completionSound.ToLower() + ".wav");
			}
		}
	}

	public static async void PlaySound(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				if (IsWindows7())
				{
					SoundPlayer player = new SoundPlayer
					{
						SoundLocation = path
					};
					player.Play();
					await Task.Delay(3000);
					player.Stop();
					player.Dispose();
				}
				else
				{
					TTSoundPlayer.PlayOnce(path, (LocalSettings.Settings.ExtraSettings.ReminderVolume > 0.0) ? (0.7 + LocalSettings.Settings.ExtraSettings.ReminderVolume / 100.0 * 0.3) : 0.0);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static long GetTotalSecond(DateTime start, DateTime end)
	{
		double totalSeconds = (end - start).TotalSeconds;
		double num = Math.Ceiling(totalSeconds);
		double num2 = Math.Floor(totalSeconds);
		if (!(totalSeconds >= (num + num2) / 2.0))
		{
			return (long)num2;
		}
		return (long)num;
	}

	public static string GetAppName()
	{
		return GetString("PublicDida");
	}

	public static async Task<bool> TryDownloadFile(string url, string localPath, bool ignoreExist = false)
	{
		if (!ignoreExist && File.Exists(localPath))
		{
			try
			{
				if (new FileInfo(localPath).Length > 0)
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				UtilLog.Info("TryDownloadFile FileExist:" + url + "," + ex.Message);
				return true;
			}
		}
		if (!IsNetworkAvailable())
		{
			return false;
		}
		WebClient downloader = new WebClient();
		try
		{
			UtilLog.Info("TryDownloadFile:" + url + "," + localPath);
			await Task.Run(async delegate
			{
				await downloader.DownloadFileTaskAsync(url, localPath);
			});
			return true;
		}
		catch (Exception ex2)
		{
			UtilLog.Error("TryDownloadFile::" + url + "," + localPath + ExceptionUtils.BuildExceptionMessage(ex2));
			if (File.Exists(localPath))
			{
				File.Delete(localPath);
			}
			return false;
		}
		finally
		{
			downloader.Dispose();
		}
	}

	public static bool IsEqualString(string textA, string textB)
	{
		if (!(textA == textB))
		{
			if (string.IsNullOrEmpty(textA))
			{
				return string.IsNullOrEmpty(textB);
			}
			return false;
		}
		return true;
	}

	public static string IntToStringWithDivide(int length)
	{
		string text = length.ToString();
		List<string> list = new List<string>();
		while (text.Length > 3)
		{
			list.Insert(0, text.Substring(text.Length - 3, 3));
			text = text.Substring(0, text.Length - 3);
		}
		if (text.Length > 0)
		{
			list.Insert(0, text);
		}
		return string.Join(",", list);
	}

	public static T GetMousePointVisibleItem<T>(System.Windows.Input.MouseEventArgs e, FrameworkElement element) where T : class
	{
		System.Windows.Point point = e?.GetPosition(element) ?? Mouse.GetPosition(element);
		IInputElement inputElement = element.InputHitTest(new System.Windows.Point(point.X, point.Y));
		DependencyObject dependencyObject = inputElement as DependencyObject;
		if (dependencyObject != null)
		{
			if (inputElement is Run run)
			{
				dependencyObject = run.Parent;
			}
			if (dependencyObject is T)
			{
				return dependencyObject as T;
			}
			return FindParent<T>(dependencyObject);
		}
		return null;
	}

	public static T GetMousePointElement<T>(System.Windows.Input.MouseEventArgs e, FrameworkElement element) where T : class
	{
		System.Windows.Point point = e?.GetPosition(element) ?? Mouse.GetPosition(element);
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(element, new System.Windows.Point(point.X, point.Y));
		if (hitTestResult?.VisualHit is T)
		{
			return hitTestResult.VisualHit as T;
		}
		if (hitTestResult != null)
		{
			return FindParent<T>(hitTestResult.VisualHit);
		}
		return null;
	}

	public static T GetMousePointItem<T>(System.Windows.Point point, FrameworkElement element) where T : class
	{
		HitTestResult hitTestResult = VisualTreeHelper.HitTest(element, point);
		if (hitTestResult?.VisualHit is T)
		{
			return hitTestResult.VisualHit as T;
		}
		if (hitTestResult != null)
		{
			return FindParent<T>(hitTestResult.VisualHit);
		}
		return null;
	}

	public static string UrlEncode(string text)
	{
		Dictionary<string, string> encodeDict = new Dictionary<string, string>
		{
			{ "(", "%28" },
			{ ")", "%29" },
			{ "!", "%21" },
			{ "*", "%2a" }
		};
		text = HttpUtility.UrlEncode(text);
		text = new Regex("[!()\\*]").Replace(text, (Match a) => (!encodeDict.ContainsKey(a.Value)) ? a.Value : encodeDict[a.Value]);
		return text;
	}

	public static string UrlEncode0(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return HttpUtility.UrlEncode(text);
		}
		return text;
	}

	public static string RgbaToArgb(string colorStr)
	{
		if (colorStr != null && colorStr.Length == 9)
		{
			string text = colorStr.Substring(7);
			string text2 = colorStr.Substring(1, 6);
			return "#" + text + text2;
		}
		return colorStr;
	}

	public static double MeasureStringWidth(string text, double fontSize, FontWeight weight)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0.0;
		}
		return MeasureString(text, new Typeface(GetDefaultFontFamily(), FontStyles.Normal, weight, FontStretches.Normal), fontSize).Width;
	}

	public static double MeasureStringWidth(string text, double fontSize)
	{
		return MeasureStringWidth(text, fontSize, FontWeights.Normal);
	}

	public static System.Windows.Size MeasureString(string text, Typeface typeface, double fontSize)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new System.Windows.Size(0.0, 0.0);
		}
		if (fontSize <= 0.0)
		{
			UtilLog.Error($"无效的字号：{fontSize}");
			return new System.Windows.Size(0.0, 0.0);
		}
		if (!typeface.TryGetGlyphTypeface(out var _))
		{
			UtilLog.Error($"无效的typeface，字体：{typeface.FontFamily}");
			return new System.Windows.Size(0.0, 0.0);
		}
		FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeface, fontSize, System.Windows.Media.Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display);
		return new System.Windows.Size(formattedText.Width, formattedText.Height);
	}

	public static BitmapImage LoadBitmap(string imagePath, int pixelWidth = -1, int pixelHeight = -1, BitmapCacheOption cache = BitmapCacheOption.OnLoad)
	{
		try
		{
			BitmapImage bitmapImage = null;
			if (File.Exists(imagePath))
			{
				bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
				bitmapImage.UriSource = new Uri(imagePath);
				if (pixelWidth > 0 && pixelHeight > 0)
				{
					bitmapImage.DecodePixelWidth = pixelWidth;
					bitmapImage.DecodePixelHeight = pixelHeight;
				}
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
		}
		catch (ArgumentException)
		{
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static Rect GetRectByString(string location)
	{
		try
		{
			return Rect.Parse(location);
		}
		catch (Exception)
		{
		}
		return new Rect(0.0, 0.0, 0.0, 0.0);
	}

	public static string GetNthString(int num)
	{
		num = ((num < 0) ? (-num) : num);
		string format = GetString("nth");
		switch (num % 10)
		{
		case 1:
			format = GetString("1st");
			break;
		case 2:
			format = GetString("2nd");
			break;
		case 3:
			format = GetString("3rd");
			break;
		}
		return string.Format(format, num);
	}

	public static int GetDotNetReleaseKey()
	{
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\");
			if (registryKey != null)
			{
				object value = registryKey.GetValue("Release");
				if (value is int)
				{
					return (int)value;
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	public static string GetDotNetVersion()
	{
		int dotNetReleaseKey = GetDotNetReleaseKey();
		if (dotNetReleaseKey >= 528040)
		{
			return "4.8 or later";
		}
		if (dotNetReleaseKey >= 461808)
		{
			return "4.7.2";
		}
		if (dotNetReleaseKey >= 461308)
		{
			return "4.7.1";
		}
		if (dotNetReleaseKey >= 460798)
		{
			return "4.7";
		}
		if (dotNetReleaseKey >= 394802)
		{
			return "4.6.2";
		}
		if (dotNetReleaseKey >= 394254)
		{
			return "4.6.1";
		}
		if (dotNetReleaseKey >= 393295)
		{
			return "4.6";
		}
		if (dotNetReleaseKey >= 379893)
		{
			return "4.5.2";
		}
		if (dotNetReleaseKey >= 378675)
		{
			return "4.5.1";
		}
		if (dotNetReleaseKey >= 378389)
		{
			return "4.5";
		}
		return "No 4.5 or later version detected";
	}

	public static bool IsBetween(double value, double a, double b)
	{
		if (!(a < b))
		{
			if (b <= value)
			{
				return value <= a;
			}
			return false;
		}
		if (a <= value)
		{
			return value <= b;
		}
		return false;
	}

	public static bool IsContain(double a1, double b1, double a2, double b2)
	{
		if ((!(a2 <= a1) || !(a1 <= b2)) && (!(a2 <= b1) || !(b1 <= b2)))
		{
			if (a2 >= a1)
			{
				return b1 >= b2;
			}
			return false;
		}
		return true;
	}

	public static System.Windows.Media.FontFamily GetDefaultFontFamily()
	{
		return LocalSettings.Settings.FontFamily ?? new System.Windows.Media.FontFamily("Microsoft YaHei UI");
	}

	public static string GetLanguage(bool useUnderLine = true)
	{
		string text = App.Ci.ToString();
		if (string.IsNullOrEmpty(text))
		{
			text = (IsDida() ? "zh-CN" : "en-US");
		}
		if (useUnderLine)
		{
			text = text.Replace("-", "_");
		}
		return text;
	}

	public static void LogActionTimes(Action action, int maxMilliseconds, string startMessage, string warnMessage)
	{
		if (!string.IsNullOrEmpty(startMessage))
		{
			UtilLog.Info(startMessage);
		}
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		action();
		stopwatch.Stop();
		if (maxMilliseconds < 0 || stopwatch.ElapsedMilliseconds > maxMilliseconds)
		{
			UtilLog.Info(warnMessage + " cost : " + stopwatch.ElapsedMilliseconds);
		}
	}

	public static async Task LogTaskTimes(Task task, int maxMilliseconds = 1000, string startMessage = null, string warnMessage = null)
	{
		if (!string.IsNullOrEmpty(startMessage))
		{
			UtilLog.Info(startMessage);
		}
		Stopwatch sw = new Stopwatch();
		sw.Start();
		await task;
		sw.Stop();
		if (maxMilliseconds < 0 || sw.ElapsedMilliseconds > maxMilliseconds)
		{
			UtilLog.Info((warnMessage ?? task.ToString()) + " cost : " + sw.ElapsedMilliseconds);
		}
	}

	public static void Focus(UIElement element)
	{
		element.Dispatcher.BeginInvoke(DispatcherPriority.Input, (ThreadStart)delegate
		{
			element.Focus();
		});
	}

	public static string GetTimeTextFromDuration(int duration, bool showHour)
	{
		int num = duration % 60;
		int num2 = (showHour ? (duration / 60 % 60) : (duration / 60));
		int num3 = (showHour ? (duration / 3600) : 0);
		return ((num3 > 0) ? (((num3 >= 10) ? "" : "0") + num3 + ":") : "") + ((num2 >= 10) ? "" : "0") + num2 + ":" + ((num >= 10) ? "" : "0") + num;
	}

	public static string MinutesToHourMinuteText(int minutes, bool showZero = false)
	{
		if (showZero && minutes == 0)
		{
			return " 0 " + GetString("PublicMinute");
		}
		int num = minutes % 60;
		int num2 = minutes / 60;
		return ((num2 > 0) ? ($"{num2} " + GetString((num2 > 1) ? "PublicHours" : "PublicHour")) : "") + ((minutes > 0) ? ($" {num} " + GetString((num > 1) ? "PublicMinutes" : "PublicMinute")) : "");
	}

	public static void SetWindowChrome(Window window, Thickness thickness)
	{
		if (ABTestManager.IsSystemWC())
		{
			WindowUtils.SetWindowChrome(window, thickness);
		}
		else
		{
			WindowUtils.SetCustomWindowChrome(window, thickness);
		}
	}

	public static string GetSystemLanguage()
	{
		List<string> obj = new List<string> { "zh-CN", "en-US", "ja-JP", "ko-KR", "fr-FR", "ru-RU", "pt-BR" };
		string text = CultureInfo.CurrentCulture.ToString();
		if (obj.Contains(text))
		{
			return text;
		}
		if (!IsDida())
		{
			return "en-US";
		}
		return "zh-CN";
	}

	public static string GetXTimeStamp()
	{
		string text = GetNowTimeStampInMills2().ToString();
		string uId = App.UId;
		try
		{
			return EncryptTimestamp(text, uId);
		}
		catch (Exception ex)
		{
			UtilLog.Error("generate xtimestamp error: " + text + ", " + uId);
			UtilLog.Error(ex);
			return text;
		}
	}

	private static string EncryptTimestamp(string timeStamp, string key)
	{
		string text = (long.Parse(timeStamp) / 2000 % 1000).ToString();
		if (text.Length < 2)
		{
			text = "00" + text;
		}
		else if (text.Length < 3)
		{
			text = "0" + text;
		}
		int index = int.Parse(text[0].ToString());
		int index2 = int.Parse(text[1].ToString());
		int index3 = int.Parse(text[2].ToString());
		string text2 = ((key[index3] - 48) % 10).ToString();
		string text3 = ((key[index2] - 48) % 10).ToString();
		string text4 = ((key[index] - 48) % 10).ToString();
		string text5 = text2 + text3 + text4;
		return timeStamp.Substring(0, timeStamp.Length - 3) + text5;
	}

	public static string ConvertNumberToLetters(int number)
	{
		if (number <= 0)
		{
			return "";
		}
		int num = number / 26;
		int num2 = number % 26;
		if (num2 == 0)
		{
			num2 = 26;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append((char)(97 + i));
		}
		if (num2 > 0)
		{
			stringBuilder.Append((char)(97 + num2 - 1));
		}
		return stringBuilder.ToString();
	}

	public static bool TryConvertLetterStringToNumber(string str, out int num)
	{
		if (string.IsNullOrEmpty(str))
		{
			num = -1;
			return false;
		}
		num = 0;
		for (int num2 = str.Length - 1; num2 >= 0; num2--)
		{
			if (str[num2] < 'a' || str[num2] > 'z')
			{
				num = -1;
				return false;
			}
			int num3 = str[num2] - 97 + 1;
			num += num3 * (int)Math.Pow(26.0, str.Length - 1 - num2);
		}
		return true;
	}

	public static bool TryConvertRomanToNumber(string roman, out int arabic)
	{
		arabic = 0;
		Dictionary<char, int> dictionary = new Dictionary<char, int>
		{
			{ 'i', 1 },
			{ 'v', 5 },
			{ 'x', 10 },
			{ 'l', 50 },
			{ 'c', 100 },
			{ 'd', 500 },
			{ 'm', 1000 }
		};
		int num = 0;
		int num2 = roman.Length - 1;
		while (num2 >= 0)
		{
			char key = roman[num2];
			if (dictionary.TryGetValue(key, out var value))
			{
				if (value < num)
				{
					arabic -= value;
				}
				else
				{
					arabic += value;
				}
				num = value;
				num2--;
				continue;
			}
			arabic = -1;
			return false;
		}
		return true;
	}

	public static string ConvertNumberToRoman(int num)
	{
		if (num <= 0)
		{
			return "";
		}
		Dictionary<int, string> source = new Dictionary<int, string>
		{
			{ 1000, "m" },
			{ 900, "cm" },
			{ 500, "d" },
			{ 400, "cd" },
			{ 100, "c" },
			{ 90, "xc" },
			{ 50, "l" },
			{ 40, "xl" },
			{ 10, "x" },
			{ 9, "ix" },
			{ 5, "v" },
			{ 4, "iv" },
			{ 1, "i" }
		};
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, string> item in source.OrderByDescending((KeyValuePair<int, string> x) => x.Key))
		{
			while (num >= item.Key)
			{
				num -= item.Key;
				stringBuilder.Append(item.Value);
			}
		}
		return stringBuilder.ToString();
	}

	public static string GetPriorityImageKey(int priority)
	{
		return priority switch
		{
			1 => "LowPriorityDrawingImage", 
			3 => "MidPriorityDrawingImage", 
			5 => "HighPriorityDrawingImage", 
			_ => "NonePriorityDrawingImage", 
		};
	}

	public static string GetDisplayText(int level, int number)
	{
		return (level % 3) switch
		{
			0 => number.ToString(), 
			1 => ConvertNumberToLetters(number), 
			2 => ConvertNumberToRoman(number), 
			_ => number.ToString(), 
		};
	}

	public static bool TryParseOrderListIndex(string countText, int indent, out int num)
	{
		num = -1;
		bool flag = (indent % 3) switch
		{
			0 => int.TryParse(countText, out num), 
			1 => TryConvertLetterStringToNumber(countText, out num), 
			2 => TryConvertRomanToNumber(countText, out num), 
			_ => int.TryParse(countText, out num), 
		};
		if (!flag)
		{
			flag = int.TryParse(countText, out num);
		}
		if (!flag)
		{
			flag = TryConvertLetterStringToNumber(countText, out num);
		}
		if (!flag)
		{
			flag = TryConvertRomanToNumber(countText, out num);
		}
		return flag;
	}

	public static int GetNewPreOrder(OrderExtra extra)
	{
		if (!string.IsNullOrEmpty(extra.Last) && TryParseOrderListIndex(extra.Last, extra.Indent, out var num))
		{
			return num + 1;
		}
		return -1;
	}

	public static int GetMarkIndexLength(int startIndex, string title, params string[] marks)
	{
		for (int i = 1; startIndex - i >= 0; i++)
		{
			int num = startIndex - i;
			if (num >= 0 && num < title.Length)
			{
				string value = title[num].ToString();
				if (marks.Contains(value))
				{
					return num;
				}
			}
		}
		return -1;
	}

	public static int GetSplashIndexLength(int startIndex, string title)
	{
		return GetMarkIndexLength(startIndex, title, "/", "、");
	}

	public static string GetConnectNotionUrl()
	{
		return BaseUrl.GetApiDomain() + "/pub/api/v2/calendar/connect/notion?redirectUri=" + UrlEncode(BaseUrl.GetDomainUrl() + "/connect/notion/windows");
	}

	public static string GetFirstLine(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}
		string[] array = input.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		if (array.Length == 0)
		{
			return string.Empty;
		}
		return array[0];
	}
}
