using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Ical.Net;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Util;

public static class SubscribeCalendarHelper
{
	public const int SubscribeCal = 6;

	public const int GoogleCal = 1;

	public const int OutlookCal = 2;

	public const int Exchange = 3;

	public const int ICloud = 4;

	public const int CalDavCal = 5;

	private static readonly Dictionary<string, DateTime> LoadUrlTimeDict = new Dictionary<string, DateTime>();

	private static bool _isParsingCalendar;

	public static async Task UnsubscribeCalendar(string calendarId)
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
		}
		else if (!string.IsNullOrEmpty(calendarId))
		{
			await Communicator.UnsubscribeCalendar(calendarId);
			await CalendarSubscribeProfileDao.DeleteCalendar(calendarId);
		}
	}

	public static async Task UnbindCalendar(string accountId)
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NoNetwork"));
		}
		else if (!string.IsNullOrEmpty(accountId))
		{
			await Communicator.UnbindCalendar(accountId);
			await BindCalendarAccountDao.DeleteBindAccountById(accountId);
			GlobalEventManager.NotifyReloadCalendar();
		}
	}

	public static async Task<CalendarSubscribeProfileModel> SubscribeCalendar(string remotePath, string color = null, string show = "show")
	{
		CalendarSubscribeProfileModel profile = await Communicator.SubscribeCalendar(new CalendarSubscribeProfileModel
		{
			Name = "Calendar",
			Url = remotePath,
			Show = show,
			CreatedTime = DateTime.Now,
			Color = (color ?? ThemeUtil.GetRandomColor())
		});
		if (profile != null && profile.errorId == null)
		{
			profile.UserId = Utils.GetCurrentUserIdInt().ToString();
			await CalendarSubscribeProfileDao.AddOrUpdateCalendar(profile);
			return profile;
		}
		return null;
	}

	public static async Task<CalendarSubscribeProfileModel> UpdateUrlOrHolidaySubscribe(CalendarSubscribeProfileModel model)
	{
		CalendarSubscribeProfileModel profile = await Communicator.SubscribeCalendar(model);
		if (profile != null && profile.errorId == null)
		{
			profile.UserId = Utils.GetCurrentUserIdInt().ToString();
			await CalendarSubscribeProfileDao.AddOrUpdateCalendar(profile);
			return profile;
		}
		return null;
	}

	public static async Task ParseUrlCalendar(string calendarId, string url)
	{
		if (!_isParsingCalendar)
		{
			_isParsingCalendar = true;
			try
			{
				await CalendarService.PullSubscribeEventsByCalIdAsync(calendarId);
				HandleCalendarUnExpired(calendarId);
			}
			catch (CustomException.CalendarExpiredException)
			{
				HandleCalendarExpired(calendarId);
			}
			_isParsingCalendar = false;
		}
	}

	private static async Task HandleCalendarUnExpired(string calendarId)
	{
		CalendarSubscribeProfileModel calendar = CacheManager.GetSubscribeCalById(calendarId);
		if (calendar != null && calendar.Expired)
		{
			calendar.Expired = false;
			await App.Connection.UpdateAsync(calendar);
			CacheManager.UpdateCalendarProfile(calendar);
		}
	}

	private static async Task HandleCalendarExpired(string calendarId)
	{
		CalendarSubscribeProfileModel calendar = CacheManager.GetSubscribeCalById(calendarId);
		if (calendar == null || calendar.Expired)
		{
			return;
		}
		calendar.Expired = true;
		List<CalendarEventModel> list = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == LocalSettings.Settings.LoginUserId && model.CalendarId == calendarId
			select model).ToListAsync();
		if (list.Any())
		{
			foreach (CalendarEventModel item in list)
			{
				await App.Connection.DeleteAsync(item);
			}
		}
		await App.Connection.UpdateAsync(calendar);
		CacheManager.UpdateCalendarProfile(calendar);
		Application.Current.Dispatcher.Invoke(delegate
		{
			SubscribeExpiredWindow.TryShowWindow(null, new List<CalendarSubscribeProfileModel> { calendar });
		});
	}

	private static string UrlToPath(string url)
	{
		string text = Utils.Base64Encode(url);
		if (text.Length > 10)
		{
			return text.Substring(0, 10);
		}
		return text;
	}

	public static async Task<CalendarCollection> TryParseCalendar(string url)
	{
		if (!Directory.Exists(AppPaths.CalendarDir))
		{
			Directory.CreateDirectory(AppPaths.CalendarDir);
		}
		string text = UrlToPath(url);
		string localPath = AppPaths.CalendarDir + text + ".ics";
		string text2 = AppPaths.CalendarDir + text + "_bak.ics";
		if (File.Exists(localPath))
		{
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			try
			{
				File.Move(localPath, text2);
			}
			catch (Exception)
			{
			}
		}
		string remotePath = url;
		if (remotePath.StartsWith("webcal"))
		{
			remotePath = remotePath.Substring(6);
			remotePath = "https" + remotePath;
		}
		if (remotePath.StartsWith("http") && !remotePath.StartsWith("https"))
		{
			remotePath = remotePath.Replace("http", "https");
		}
		if (!remotePath.StartsWith("https") && !remotePath.StartsWith("http"))
		{
			remotePath = "https://" + remotePath;
		}
		if (remotePath.Contains("mymba.sjtu.edu.cn"))
		{
			remotePath = remotePath.Replace("https", "http");
		}
		if (Utils.IsNetworkAvailable())
		{
			WebClient downloader = new WebClient();
			try
			{
				await downloader.DownloadFileTaskAsync(remotePath, localPath);
			}
			catch (Exception)
			{
				if (remotePath.StartsWith("https"))
				{
					remotePath = remotePath.Replace("https", "http");
					try
					{
						await downloader.DownloadFileTaskAsync(remotePath, localPath);
					}
					catch (Exception)
					{
					}
				}
			}
			finally
			{
				downloader?.Dispose();
			}
			if (File.Exists(localPath))
			{
				try
				{
					CalendarCollection calendarCollection = CalendarCollection.Load(localPath);
					if (calendarCollection != null)
					{
						return calendarCollection;
					}
				}
				catch (Exception)
				{
					return null;
				}
			}
		}
		return null;
	}

	public static async Task SaveBindAccount(BindCalendarAccountModel account)
	{
		await BindCalendarAccountDao.SaveBindCalendarAccount(account);
	}

	public static async Task<BindCalendarAccountModel> BindCalendarAccount(string code)
	{
		string url = (Utils.IsDida() ? "dida365.com" : "ticktick.com");
		BindCalendarAccountModel bindCalendarAccountModel = await Communicator.BindCalendarAccount(code, "google", url);
		if (bindCalendarAccountModel != null && !string.IsNullOrEmpty(bindCalendarAccountModel.Account))
		{
			bindCalendarAccountModel.UserId = Utils.GetCurrentUserIdInt().ToString();
			return bindCalendarAccountModel;
		}
		return null;
	}

	public static string GetCalendarName(string calendarId)
	{
		if (string.IsNullOrEmpty(calendarId))
		{
			return string.Empty;
		}
		BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId);
		if (bindCalendarModel == null)
		{
			return string.Empty;
		}
		return bindCalendarModel.Name;
	}

	public static BindCalendarModel GetDefaultCalendar(string accountId)
	{
		List<BindCalendarModel> source = (from cal in CacheManager.GetBindCalendars()
			where cal.AccountId == accountId && cal.Accessible
			select cal).ToList();
		if (source.Any())
		{
			return source.FirstOrDefault((BindCalendarModel cal) => cal.Show != "hidden") ?? source.First();
		}
		return null;
	}

	public static string GetCalendarSelectorAccountIdForCreate(string preferredAccountId = null)
	{
		HashSet<string> accountIds = new HashSet<string>(from account in CacheManager.GetBindCalendarAccounts()
			where account != null && !account.Expired
			select account.Id);
		List<string> list = (from calendar in CacheManager.GetBindCalendars()
			where calendar != null && accountIds.Contains(calendar.AccountId) && calendar.Accessible
			select calendar.AccountId into accountId
			where !string.IsNullOrEmpty(accountId)
			select accountId).Distinct().ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		string text;
		if (list.Count <= 1)
		{
			text = preferredAccountId;
			if (text == null)
			{
				return string.Empty;
			}
		}
		else
		{
			text = string.Empty;
		}
		return text;
	}

	public static async void ParseUrlCalendars(List<CalendarSubscribeProfileModel> models)
	{
		foreach (CalendarSubscribeProfileModel model in models)
		{
			await ParseUrlCalendar(model.Id, model.Url);
		}
		CalendarEventChangeNotifier.NotifyRemoteChanged();
	}

	public static int GetCalendarType(string calendarId)
	{
		if (string.IsNullOrEmpty(calendarId))
		{
			return 6;
		}
		return GetAccountTypeById(CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId)?.AccountId);
	}

	private static int GetAccountTypeById(string accountId)
	{
		if (string.IsNullOrEmpty(accountId))
		{
			return 6;
		}
		return GetAccountType(CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel ac) => ac.Id == accountId));
	}

	public static int GetAccountType(BindCalendarAccountModel account)
	{
		if (account == null)
		{
			return 6;
		}
		switch (account.Kind)
		{
		case "icloud":
			return 4;
		case "caldav":
			return 5;
		case "exchange":
			return 3;
		default:
		{
			string site = account.Site;
			if (!(site == "outlook"))
			{
				if (site == "google")
				{
					return 1;
				}
				return 6;
			}
			return 2;
		}
		}
	}

	public static Geometry GetCalendarProjectIconById(string accountId)
	{
		return GetCalendarProjectIcon(string.IsNullOrEmpty(accountId) ? null : CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel ac) => ac.Id == accountId));
	}

	public static Geometry GetCalendarProjectIcon(BindCalendarAccountModel account)
	{
		switch (account?.Kind)
		{
		case "caldav":
			return Utils.GetGeometry("Ic.ProjectList.Caldav");
		case "exchange":
			return Utils.GetGeometry("Ic.ProjectList.Exchange");
		case "icloud":
			return Utils.GetGeometry("Ic.ProjectList.ICloud");
		default:
		{
			string text = account?.Site;
			if (!(text == "outlook"))
			{
				if (text == "google")
				{
					return Utils.GetGeometry("Ic.ProjectList.Google");
				}
				return Utils.GetGeometry("Ic.ProjectList.Url");
			}
			return Utils.GetGeometry("Ic.ProjectList.Outlook");
		}
		}
	}

	public static async Task<bool> OnHolidaySubscribeSelect(string data, Action<string> toastAction)
	{
		string[] newUrls = data.Split(',');
		if (string.IsNullOrEmpty(data))
		{
			newUrls = new string[0];
		}
		List<CalendarSubscribeProfileModel> source = await CalendarSubscribeProfileDao.GetProfiles();
		IEnumerable<string> enumerable = source.Select((CalendarSubscribeProfileModel x) => x.Url);
		IEnumerable<string> added = newUrls.Except(enumerable);
		IEnumerable<string> removed = enumerable.Except(newUrls);
		IEnumerable<string> removedIds = from x in source
			where removed.Contains(x.Url)
			select x.Id;
		if (removed.Any() && await Communicator.BatchUnsubscribeCalendar(removedIds))
		{
			foreach (string item in removedIds)
			{
				await CalendarSubscribeProfileDao.DeleteCalendar(item);
			}
		}
		if (added.Any())
		{
			List<CalendarSubscribeProfileModel> profiles = await Communicator.BatchSubscribeCalendar(added);
			if (profiles != null)
			{
				foreach (CalendarSubscribeProfileModel item2 in profiles)
				{
					if (item2 != null && item2.errorId == null)
					{
						item2.UserId = Utils.GetCurrentUserIdInt().ToString();
						await CalendarSubscribeProfileDao.AddOrUpdateCalendar(item2);
					}
				}
				Task.Run(delegate
				{
					ParseUrlCalendars(profiles);
				});
			}
		}
		ListViewContainer.ReloadProjectData();
		if (added.Count() > 0)
		{
			toastAction?.Invoke(Utils.GetString("Added"));
		}
		else if (removed.Count() > 0)
		{
			toastAction?.Invoke(Utils.GetString("Unsubscribed"));
		}
		return added.Count() + removed.Count() > 0;
	}
}
