using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.CountDown.ViewModels;

namespace ticktick_WPF.Dal;

public static class CountdownDao
{
	public static async Task<List<CountdownModel>> GetAllCountdowns()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from it in App.Connection.Table<CountdownModel>()
			where it.UserId == userId && it.Deleted == 0
			orderby it.SortOrder
			select it).ToListAsync();
	}

	public static async Task<List<CountdownModel>> GetActiveCountdowns()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.Status == 0 && v.Deleted == 0
			orderby v.SortOrder
			select v).ToListAsync();
	}

	public static async Task<List<CountdownModel>> GetArchivedCountdowns()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.Status == 1 && v.Deleted == 0
			orderby v.SortOrder
			select v).ToListAsync();
	}

	public static async Task<CountdownModel> GetCountdownByIdWithDeleted(string countdownId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.Id == countdownId
			select v).FirstOrDefaultAsync();
	}

	public static async Task<CountdownModel> GetCountdownById(string countdownId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.Id == countdownId && v.Deleted == 0
			select v).FirstOrDefaultAsync();
	}

	public static async Task<List<CountdownModel>> GetCountdownsByType(CountDownType type)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && (int)v.Type == (int)type && v.Status == 0 && v.Deleted == 0
			orderby v.SortOrder
			select v).ToListAsync();
	}

	public static async Task<List<CountdownModel>> GetUpcomingCountdowns(int daysAhead = 7)
	{
		List<CountdownModel> obj = await GetActiveCountdowns();
		DateTime today = DateTime.Today;
		DateTime dateTime = today.AddDays(daysAhead);
		List<CountdownModel> list = new List<CountdownModel>();
		foreach (CountdownModel item in obj)
		{
			if (!string.IsNullOrEmpty(item.RepeatFlag))
			{
				if (item.TargetDate != default(DateTime) && item.TargetDate >= today && item.TargetDate <= dateTime)
				{
					list.Add(item);
				}
				continue;
			}
			DateTime dateAsDateTime = item.GetDateAsDateTime();
			if (dateAsDateTime >= today && dateAsDateTime <= dateTime)
			{
				list.Add(item);
			}
		}
		return list.OrderBy((CountdownModel c) => c.GetDaysRemaining()).ToList();
	}

	public static async Task<List<CountdownModel>> GetSmartListCountdowns(int queryType = 0)
	{
		List<CountdownModel> result = new List<CountdownModel>();
		if (!LocalSettings.Settings.EnableCountDownDays)
		{
			return result;
		}
		List<CountdownModel> obj = await GetActiveCountdowns();
		DateTime baseDate = DateTime.Today;
		(DateTime, DateTime) smartListDateRange = GetSmartListDateRange(queryType, baseDate);
		foreach (CountdownModel item in obj)
		{
			if (item.TypeOfSmartList == 9999)
			{
				continue;
			}
			if (item.TypeOfSmartList == -9999)
			{
				result.Add(item);
				continue;
			}
			DateTime targetDate = item.GetTargetDate();
			(DateTime, DateTime) displayDateRange = GetDisplayDateRange(item.TypeOfSmartList, targetDate);
			if (HasDateRangeOverlap(smartListDateRange, displayDateRange) && baseDate >= displayDateRange.Item1 && baseDate <= displayDateRange.Item2)
			{
				result.Add(item);
			}
		}
		return (from x in result.Select(delegate(CountdownModel c)
			{
				DateTime? current2 = baseDate;
				return new
				{
					Item = c,
					Days = c.GetDaysRemaining(null, current2)
				};
			})
			orderby (x.Days < 0) ? 1 : 0, Math.Abs(x.Days)
			select x.Item).ToList();
	}

	private static (DateTime start, DateTime end) GetSmartListDateRange(int queryType, DateTime baseDate)
	{
		return queryType switch
		{
			0 => (start: baseDate, end: baseDate), 
			1 => (start: baseDate.AddDays(1.0), end: baseDate.AddDays(1.0)), 
			2 => (start: baseDate, end: baseDate), 
			_ => (start: baseDate, end: baseDate), 
		};
	}

	private static (DateTime start, DateTime end) GetDisplayDateRange(int typeOfSmartList, DateTime targetDate)
	{
		return typeOfSmartList switch
		{
			-3 => (start: targetDate.AddDays(-3.0), end: targetDate), 
			-7 => (start: targetDate.AddDays(-7.0), end: targetDate), 
			_ => (start: targetDate, end: targetDate), 
		};
	}

	private static bool HasDateRangeOverlap((DateTime start, DateTime end) range1, (DateTime start, DateTime end) range2)
	{
		if (range1.end >= range2.start)
		{
			return range1.start <= range2.end;
		}
		return false;
	}

	public static async Task<CountdownModel> CreateCountdown(CountdownModel countdown)
	{
		if (countdown == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(countdown.Id))
		{
			countdown.Id = Utils.GetGuid();
		}
		countdown.UserId = Utils.GetCurrentUserIdInt().ToString();
		countdown.CreatedTime = DateTime.Now;
		countdown.ModifiedTime = DateTime.Now;
		countdown.SyncStatus = 0;
		if (countdown.SortOrder == 0L)
		{
			countdown.SortOrder = await GetNewCountdownSortOrder();
		}
		await App.Connection.InsertAsync(countdown);
		DataChangedNotifier.NotifyCountdownsChanged(new CountdownNotifier
		{
			Model = countdown,
			Type = CountdownNotifyType.Add
		});
		await CountdownSyncService.SyncCountdowns();
		return countdown;
	}

	public static async Task<CountdownModel> UpdateCountdown(CountdownModel countdown, bool notify = true)
	{
		if (countdown == null)
		{
			return null;
		}
		CountdownModel countdownModel = await GetCountdownById(countdown.Id);
		if (countdownModel == null)
		{
			return null;
		}
		countdown.ModifiedTime = DateTime.Now;
		countdown.SyncStatus = GetUpdatedSyncStatus(countdownModel.SyncStatus);
		await UpdateAsync(countdown);
		await CountdownSyncService.SyncCountdowns(notify);
		return countdown;
	}

	public static async Task SaveCountdownEtag(string countdownId, string etag)
	{
		CountdownModel countdownModel = await GetCountdownById(countdownId);
		if (countdownModel != null)
		{
			countdownModel.Etag = etag;
			countdownModel.SyncStatus = 2;
			await UpdateAsync(countdownModel);
		}
	}

	public static async Task<List<CountdownModel>> GetNeedPostCountdowns()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<CountdownModel> countdowns = await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.SyncStatus != 2
			orderby v.SortOrder
			select v).ToListAsync();
		if (countdowns == null || !countdowns.Any())
		{
			return countdowns;
		}
		int num = await (from v in App.Connection.Table<CountdownModel>()
			where v.UserId == userId && v.Deleted == 0 && v.Status == 0 && v.SyncStatus != 0 && v.SyncStatus != 4
			select v).CountAsync();
		int num2 = (int)Utils.GetUserLimit(Constants.LimitKind.CountdownNumber);
		int canAddCount = Math.Max(0, num2 - num);
		List<CountdownModel> result = new List<CountdownModel>();
		foreach (CountdownModel item in countdowns)
		{
			if (!IsAddPendingStatus(item.SyncStatus))
			{
				result.Add(item);
			}
			else if (item.Status == 1)
			{
				result.Add(item);
			}
			else if (canAddCount > 0)
			{
				result.Add(item);
				canAddCount--;
			}
			else if (item.SyncStatus == 0)
			{
				item.SyncStatus = 4;
				await UpdateAsync(item);
			}
		}
		return result;
	}

	public static async Task ChangeCountdownArchiveStatus(string countdownId, bool isArchive)
	{
		CountdownModel countdown = await GetCountdownById(countdownId);
		if (countdown != null)
		{
			countdown.Status = (isArchive ? 1 : 0);
			countdown.ModifiedTime = DateTime.Now;
			countdown.ArchivedTime = (isArchive ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			countdown.SyncStatus = GetUpdatedSyncStatus(countdown.SyncStatus);
			await UpdateAsync(countdown);
			DataChangedNotifier.NotifyCountdownsChanged(new CountdownNotifier
			{
				Model = countdown,
				Type = CountdownNotifyType.Update
			});
			await CountdownSyncService.SyncCountdowns();
		}
	}

	public static async Task DeleteCountdown(string countdownId)
	{
		CountdownModel countdown = await GetCountdownById(countdownId);
		if (countdown != null)
		{
			UtilLog.Info("CountdownDao.DeleteCountdown " + countdownId);
			countdown.Deleted = 1;
			countdown.ModifiedTime = DateTime.Now;
			countdown.SyncStatus = -1;
			await UpdateAsync(countdown);
			DataChangedNotifier.NotifyCountdownsChanged(new CountdownNotifier
			{
				Model = countdown,
				Type = CountdownNotifyType.Delete
			});
			await CountdownSyncService.SyncCountdowns();
		}
	}

	public static async Task<List<long>> GetAllSortOrders()
	{
		return (await GetAllCountdowns())?.Select((CountdownModel c) => c.SortOrder).ToList();
	}

	public static async Task<long> GetNewCountdownSortOrder()
	{
		List<long> list = await GetAllSortOrders();
		return ((list != null && list.Count > 0) ? list.Min() : 0) - 268435456;
	}

	public static async Task<string> GetCountdownNameById(string id)
	{
		string sql = "select Name as Text from CountdownModel where Id = '" + id + "' limit 1";
		StringModel stringModel = (await App.Connection.QueryAsync<StringModel>(sql, Array.Empty<object>()))?.FirstOrDefault();
		return (stringModel != null) ? (stringModel.Text ?? string.Empty) : null;
	}

	public static async Task<bool> UpdateCountdownBackground(string countdownId, StyleSelectionResult style)
	{
		_ = 1;
		try
		{
			if (string.IsNullOrEmpty(countdownId))
			{
				return false;
			}
			CountdownModel countdownModel = await GetCountdownById(countdownId);
			if (countdownModel == null)
			{
				return false;
			}
			SetCoundownStyle(countdownModel, style);
			countdownModel.ModifiedTime = DateTime.Now;
			await UpdateCountdown(countdownModel);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void SetCoundownStyle(CountdownModel model, StyleSelectionResult style)
	{
		model.Style = style.Style;
		model.StyleColors = style.StyleColors;
		model.LocalImagePath = style.LocalImagePath;
		string attachmentId = style.AttachmentId;
		string text = attachmentId;
		if (!string.IsNullOrEmpty(attachmentId) && attachmentId.Length > 1)
		{
			text = attachmentId;
		}
		else if (!string.IsNullOrEmpty(style.PresetImage))
		{
			text = "preset_" + style.PresetImage;
		}
		if (!string.IsNullOrEmpty(text))
		{
			BackgroundModel backgroundModel = model.BackgroundObject ?? new BackgroundModel();
			backgroundModel.Id = text;
			if (style.ClipRect.Width > 0.0)
			{
				backgroundModel.Landscape = new CropModel
				{
					Top = style.ClipRect.Top,
					Right = style.ClipRect.Right,
					Bottom = style.ClipRect.Bottom,
					Left = style.ClipRect.Left
				};
			}
			else
			{
				backgroundModel.Landscape = null;
			}
			model.BackgroundObject = backgroundModel;
		}
	}

	public static async Task UpdateLocalImagePath(string countdownId, string imagePath)
	{
		CountdownModel countdownModel = await GetCountdownById(countdownId);
		if (countdownModel != null)
		{
			countdownModel.LocalImagePath = imagePath;
			countdownModel.ModifiedTime = DateTime.Now;
			await UpdateAsync(countdownModel);
		}
	}

	public static async Task ChangeCountdownPinStatus(string countdownId, bool isPinned)
	{
		if (isPinned)
		{
			IEnumerable<CountdownModel> enumerable = (await GetAllCountdowns()).Where((CountdownModel it) => it.pinnedTimeValue != null && it.pinnedTimeValue != "-1");
			foreach (CountdownModel item in enumerable)
			{
				item.pinnedTimeStamp = -1L;
				item.ModifiedTime = DateTime.Now;
				item.SyncStatus = GetUpdatedSyncStatus(item.SyncStatus);
				await UpdateAsync(item);
			}
		}
		CountdownModel countdown = await GetCountdownById(countdownId);
		if (countdown != null)
		{
			countdown.pinnedTimeStamp = (isPinned ? Utils.GetNowTimeStamp() : (-1));
			countdown.ModifiedTime = DateTime.Now;
			countdown.SyncStatus = GetUpdatedSyncStatus(countdown.SyncStatus);
			await UpdateAsync(countdown);
			DataChangedNotifier.NotifyCountdownsChanged(new CountdownNotifier
			{
				Model = countdown,
				Type = CountdownNotifyType.Update
			});
			await CountdownSyncService.SyncCountdowns();
		}
	}

	public static async Task<CountdownModel> UpdateRemark(string countdownId, string remark)
	{
		CountdownModel countdownModel = await GetCountdownById(countdownId);
		if (countdownModel == null)
		{
			return null;
		}
		countdownModel.Remark = remark;
		return await UpdateCountdown(countdownModel);
	}

	public static async Task<CountdownModel> GetNearestCountdown()
	{
		List<CountdownModel> countdowns = await GetActiveCountdowns();
		return (await Task.Run(() => MainCountDownViewModel.SortAllModels(countdowns, isArchivedOrder: false, ignorePinOrder: true))).FirstOrDefault();
	}

	public static async Task SetSyncStatus(string countdownId, int status)
	{
		CountdownModel countdownModel = await GetCountdownByIdWithDeleted(countdownId);
		if (countdownModel != null)
		{
			countdownModel.SyncStatus = status;
			await UpdateAsync(countdownModel);
		}
	}

	public static async Task ClearCountdownEtag()
	{
		List<CountdownModel> list = await GetAllCountdowns();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (CountdownModel item in list)
		{
			item.Etag = string.Empty;
		}
		await UpdateAllAsync(list);
	}

	public static async Task<int> InsertAllAsync(List<CountdownModel> models)
	{
		return await App.Connection.InsertAllAsync(models);
	}

	public static async Task<int> InsertAsync(CountdownModel model)
	{
		return await App.Connection.InsertAsync(model);
	}

	public static async Task<int> UpdateAsync(CountdownModel model)
	{
		return await App.Connection.UpdateAsync(model);
	}

	public static async Task<int> UpdateAllAsync(List<CountdownModel> models)
	{
		return await App.Connection.UpdateAllAsync(models);
	}

	private static bool IsAddPendingStatus(int syncStatus)
	{
		if (syncStatus != 0)
		{
			return syncStatus == 4;
		}
		return true;
	}

	private static int GetUpdatedSyncStatus(int syncStatus)
	{
		if (!IsAddPendingStatus(syncStatus))
		{
			return 1;
		}
		return syncStatus;
	}

	public static async Task InitializeDefaultCountdowns()
	{
		_ = 2;
		try
		{
			List<CountdownModel> list = await GetAllCountdowns();
			if ((list == null || list.Count == 0) && !LocalSettings.Settings.ExtraSettings.HasCreatedInitCountdowns && (await CountdownService.PullRemoteCountdowns()).Countdowns == null)
			{
				LocalSettings.Settings.ExtraSettings.HasCreatedInitCountdowns = true;
				UtilLog.Info("init default countdowns");
				await CreateDefaultCountdowns();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("InitializeDefaultCountdowns Failed: " + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public static async Task CreateDefaultCountdowns()
	{
		List<CountdownModel> list = await GetAllCountdowns();
		if (list == null || list.Count <= 0)
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			new List<CountdownModel>();
			List<CountdownModel> models;
			if (Utils.IsTickPackage())
			{
				List<CountdownModel> list2 = new List<CountdownModel>
				{
					new CountdownModel
					{
						Id = Utils.GetGuid(),
						UserId = userId,
						Name = Utils.GetString("weekend"),
						IconRes = "countdown_countdown",
						Color = "#FF9500",
						Type = CountDownType.Countdown,
						ShowCalendarType = ShowCalendarType.Gregorian,
						DateDisplayFormat = "day",
						Style = "normal",
						TypeOfSmartList = 0,
						IgnoreYear = true,
						RepeatFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1",
						Status = 0,
						Deleted = 0,
						Date = GetNextSaturday(),
						SyncStatus = 0,
						CreatedTime = DateTime.Now,
						ModifiedTime = DateTime.Now,
						SortOrder = -268435456L
					},
					new CountdownModel
					{
						Id = Utils.GetGuid(),
						UserId = userId,
						Name = Utils.GetString("new_years_day"),
						IconRes = "countdown_new_year_day",
						Color = "#FF9500",
						Type = CountDownType.Holiday,
						ShowCalendarType = ShowCalendarType.Gregorian,
						DateDisplayFormat = "day",
						Style = "normal",
						RepeatFlag = "RRULE:FREQ=YEARLY;INTERVAL=1;BYMONTH=1;BYMONTHDAY=1",
						TypeOfSmartList = 0,
						IgnoreYear = true,
						Status = 0,
						Deleted = 0,
						Date = GetNextNewYearDate(),
						SyncStatus = 0,
						CreatedTime = DateTime.Now,
						ModifiedTime = DateTime.Now,
						SortOrder = -805306368L
					}
				};
				List<CountdownModel> list3 = list2;
				CountdownModel countdownModel = new CountdownModel
				{
					Id = Utils.GetGuid(),
					UserId = userId,
					Name = string.Format(Utils.GetString("use_app_xx"), Utils.GetAppName()),
					IconRes = "countdown_ticktick",
					Color = "#4CD964",
					Type = CountDownType.Anniversary,
					ShowCalendarType = ShowCalendarType.Gregorian,
					DateDisplayFormat = "day",
					Style = "normal",
					TypeOfSmartList = 0,
					IgnoreYear = false,
					Status = 0,
					Deleted = 0
				};
				CountdownModel countdownModel2 = countdownModel;
				countdownModel2.Date = await GetTickTickStartDate();
				countdownModel.SyncStatus = 0;
				countdownModel.CreatedTime = DateTime.Now;
				countdownModel.ModifiedTime = DateTime.Now;
				countdownModel.SortOrder = -1342177280L;
				list3.Add(countdownModel);
				models = list2;
			}
			else
			{
				List<CountdownModel> list3 = new List<CountdownModel>
				{
					new CountdownModel
					{
						Id = Utils.GetGuid(),
						UserId = userId,
						Name = Utils.GetString("weekend"),
						IconRes = "countdown_countdown",
						Color = "#FF9500",
						Type = CountDownType.Countdown,
						ShowCalendarType = ShowCalendarType.Gregorian,
						DateDisplayFormat = "day",
						Style = "normal",
						TypeOfSmartList = 0,
						IgnoreYear = true,
						Status = 0,
						Deleted = 0,
						RepeatFlag = "RRULE:FREQ=WEEKLY;INTERVAL=1",
						Date = GetNextSaturday(),
						SyncStatus = 0,
						CreatedTime = DateTime.Now,
						ModifiedTime = DateTime.Now,
						SortOrder = -268435456L
					},
					new CountdownModel
					{
						Id = Utils.GetGuid(),
						UserId = userId,
						Name = Utils.GetString("spring_festival"),
						IconRes = "countdown_chunjie",
						Color = "#FF9500",
						Type = CountDownType.Holiday,
						ShowCalendarType = ShowCalendarType.Lunar,
						DateDisplayFormat = "day",
						Style = "normal",
						TypeOfSmartList = 0,
						IgnoreYear = true,
						Status = 0,
						Deleted = 0,
						RepeatFlag = "LUNAR:FREQ=YEARLY;INTERVAL=1;BYMONTH=1;BYMONTHDAY=1",
						Date = GetNextSpringFestivalDate(),
						SyncStatus = 0,
						CreatedTime = DateTime.Now,
						ModifiedTime = DateTime.Now,
						SortOrder = -536870912L
					}
				};
				List<CountdownModel> list2 = list3;
				CountdownModel countdownModel2 = new CountdownModel
				{
					Id = Utils.GetGuid(),
					UserId = userId,
					Name = string.Format(Utils.GetString("use_app_xx"), Utils.GetAppName()),
					IconRes = "countdown_ticktick",
					Color = "#4CD964",
					Type = CountDownType.Anniversary,
					ShowCalendarType = ShowCalendarType.Gregorian,
					DateDisplayFormat = "day",
					Style = "normal",
					TypeOfSmartList = 0,
					IgnoreYear = false,
					Status = 0,
					Deleted = 0
				};
				CountdownModel countdownModel = countdownModel2;
				countdownModel.Date = await GetTickTickStartDate();
				countdownModel2.SyncStatus = 0;
				countdownModel2.CreatedTime = DateTime.Now;
				countdownModel2.ModifiedTime = DateTime.Now;
				countdownModel2.SortOrder = -1342177280L;
				list2.Add(countdownModel2);
				models = list3;
			}
			await InsertAllAsync(models);
			await CountdownSyncService.SyncCountdowns();
		}
	}

	private static int GetNextSpringFestivalDate()
	{
		return 20250129;
	}

	private static int GetNextNewYearDate()
	{
		int num = DateTime.Now.Year + 1;
		return int.Parse($"{num}0101");
	}

	private static int GetNextSaturday()
	{
		DateTime today = DateTime.Today;
		DateTime dateTime = today.AddDays((int)(6 - today.DayOfWeek + 7) % 7);
		if (today.DayOfWeek == DayOfWeek.Saturday)
		{
			dateTime = dateTime.AddDays(7.0);
		}
		return int.Parse(dateTime.ToString("yyyyMMdd"));
	}

	private static int GetNextNationalDay()
	{
		int year = DateTime.Now.Year;
		DateTime dateTime = new DateTime(year, 10, 1);
		if (DateTime.Now > dateTime)
		{
			dateTime = new DateTime(year + 1, 10, 1);
		}
		return int.Parse(dateTime.ToString("yyyyMMdd"));
	}

	private static async Task<int> GetTickTickStartDate()
	{
		try
		{
			return int.Parse(await Communicator.GetUserRegTime());
		}
		catch (Exception)
		{
			return int.Parse(DateTime.Now.ToString("yyyyMMdd"));
		}
	}
}
