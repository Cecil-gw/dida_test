using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Dal;

public static class CalendarEventDao
{
	private static List<CalendarEventModel> _skipEvents;

	private static readonly TimeSpan RemoteSnapshotDeleteProtectionDuration = TimeSpan.FromMinutes(3.0);

	private static readonly object RemoteSnapshotDeleteProtectionLocker = new object();

	private static readonly Dictionary<string, DateTime> RemoteSnapshotDeleteProtectedEventIds = new Dictionary<string, DateTime>();

	public static void ProtectFromRemoteSnapshotDelete(CalendarEventModel calendarEvent)
	{
		if (calendarEvent == null || string.IsNullOrEmpty(calendarEvent.Id))
		{
			return;
		}
		lock (RemoteSnapshotDeleteProtectionLocker)
		{
			RemoteSnapshotDeleteProtectedEventIds[calendarEvent.Id] = DateTime.Now.Add(RemoteSnapshotDeleteProtectionDuration);
		}
	}

	private static void ClearRemoteSnapshotDeleteProtection(CalendarEventModel calendarEvent)
	{
		if (calendarEvent == null || string.IsNullOrEmpty(calendarEvent.Id))
		{
			return;
		}
		lock (RemoteSnapshotDeleteProtectionLocker)
		{
			RemoteSnapshotDeleteProtectedEventIds.Remove(calendarEvent.Id);
		}
	}

	public static bool ShouldDeleteMissingLocalEvent(CalendarEventModel calendarEvent, DateTime start, DateTime end, HashSet<string> protectedEventIds)
	{
		if (calendarEvent == null)
		{
			return false;
		}
		if (IsRemoteSnapshotDeleteProtected(calendarEvent, protectedEventIds))
		{
			return false;
		}
		if (calendarEvent.DueStart.HasValue && calendarEvent.DueEnd.HasValue && (calendarEvent.DueStart.Value <= start || calendarEvent.DueStart.Value >= end))
		{
			return false;
		}
		return true;
	}

	private static async Task<HashSet<string>> GetRemoteSnapshotDeleteProtectedEventIds()
	{
		HashSet<string> protectedEventIds = GetRecentlyProtectedEventIds();
		List<SyncStatusModel> list = await SyncStatusDao.GetAllSyncStatus();
		if (list == null || !list.Any())
		{
			return protectedEventIds;
		}
		foreach (SyncStatusModel item in list)
		{
			if (item.Type == 10)
			{
				List<CalendarEventModel> list2 = await GetEventsByEventId(item.EntityId);
				if (list2.Count == 1 && !string.IsNullOrEmpty(list2[0].Id))
				{
					protectedEventIds.Add(list2[0].Id);
				}
			}
		}
		return protectedEventIds;
	}

	private static HashSet<string> GetRecentlyProtectedEventIds()
	{
		DateTime now = DateTime.Now;
		HashSet<string> hashSet = new HashSet<string>();
		lock (RemoteSnapshotDeleteProtectionLocker)
		{
			foreach (string item in (from pair in RemoteSnapshotDeleteProtectedEventIds
				where pair.Value <= now
				select pair.Key).ToList())
			{
				RemoteSnapshotDeleteProtectedEventIds.Remove(item);
			}
			foreach (string key in RemoteSnapshotDeleteProtectedEventIds.Keys)
			{
				hashSet.Add(key);
			}
			return hashSet;
		}
	}

	private static bool IsRemoteSnapshotDeleteProtected(CalendarEventModel calendarEvent, HashSet<string> protectedEventIds)
	{
		if (protectedEventIds == null || protectedEventIds.Count == 0)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(calendarEvent.Id))
		{
			return protectedEventIds.Contains(calendarEvent.Id);
		}
		return false;
	}

	public static async Task SaveEvents(List<CalendarEventModel> models, string calendarId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarEventModel> list = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.CalendarId == calendarId
			select model).ToListAsync();
		if (list.Any())
		{
			foreach (CalendarEventModel item in list)
			{
				await App.Connection.DeleteAsync(item);
			}
		}
		_skipEvents = null;
		await App.Connection.InsertAllAsync(models);
	}

	public static async Task AddEvent(CalendarEventModel model)
	{
		await App.Connection.InsertAsync(model);
	}

	public static async Task SaveEvent(CalendarEventModel model)
	{
		await App.Connection.UpdateAsync(model);
	}

	public static async Task<CalendarEventModel> GetEventByEventIdOrId(string eventId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		CalendarEventModel calendarEventModel = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && (model.EventId == eventId || model.Id == eventId)
			select model).FirstOrDefaultAsync();
		if (calendarEventModel != null)
		{
			AssemblyCalendarEvent(calendarEventModel);
		}
		return calendarEventModel;
	}

	public static async Task<CalendarEventModel> GetEventByEventId(string eventId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		CalendarEventModel calendarEventModel = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.EventId == eventId
			select model).FirstOrDefaultAsync();
		if (calendarEventModel != null)
		{
			AssemblyCalendarEvent(calendarEventModel);
		}
		return calendarEventModel;
	}

	private static async Task<List<CalendarEventModel>> GetEventsByEventId(string eventId)
	{
		if (string.IsNullOrEmpty(eventId))
		{
			return new List<CalendarEventModel>();
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.EventId == eventId
			select model).ToListAsync();
	}

	public static async Task<CalendarEventModel> GetEventById(string eventId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		CalendarEventModel calendarEventModel = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.Id == eventId
			select model).FirstOrDefaultAsync();
		if (calendarEventModel != null)
		{
			AssemblyCalendarEvent(calendarEventModel);
		}
		return calendarEventModel;
	}

	private static void AssemblyCalendarEvent(CalendarEventModel calEvent)
	{
		bool editable = false;
		if (UserDao.IsUserValid())
		{
			BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calEvent.CalendarId);
			if (bindCalendarModel != null)
			{
				if (bindCalendarModel.AvailableType == CalendarAvailableType.Editable)
				{
					editable = true;
				}
				calEvent.AvailableType = bindCalendarModel.AvailableType;
			}
		}
		calEvent.Editable = editable;
		if (!string.IsNullOrEmpty(calEvent.AttendeeInfo))
		{
			calEvent.Attendees = JsonConvert.DeserializeObject<List<CalendarAttendeeModel>>(calEvent.AttendeeInfo);
		}
		if (!string.IsNullOrEmpty(calEvent.Reminders))
		{
			try
			{
				calEvent.ReminderList = JsonConvert.DeserializeObject<List<int>>(calEvent.Reminders);
			}
			catch (Exception)
			{
				calEvent.ReminderList = null;
			}
		}
		if (!string.IsNullOrEmpty(calEvent.ExDates))
		{
			try
			{
				calEvent.ExDateList = JsonConvert.DeserializeObject<List<DateTime>>(calEvent.ExDates);
			}
			catch (Exception)
			{
				calEvent.ExDateList = null;
			}
		}
		if (string.IsNullOrEmpty(calEvent.TimeZone))
		{
			calEvent.TimeZone = Utils.GetLocalTimeZone();
		}
	}

	private static async Task<List<CalendarEventModel>> GetEventsByCalendarId(string calendarId, int type = 1)
	{
		string arg = Utils.GetCurrentUserIdInt().ToString();
		string text = DateTime.Today.AddSeconds(-1.0).ToString("yyyy-MM-dd HH:mm:ss", App.Ci);
		string text2 = DateTime.Today.AddMonths(3).ToString("yyyy-MM-dd 23:59:59", App.Ci);
		string sql = $"select * from CalendarEventModel where Deleted = 0 and userId = '{arg}' and Type = '{type}' and CalendarId = '{calendarId}' and " + "(((DueStart is not null and DueStart >= '" + text + "' and DueStart < '" + text2 + "')) or ((DueEnd is not null and DueEnd > '" + text + "' and DueEnd < '" + text2 + "')) or (DueStart is not null and DueEnd is not null and DueStart < '" + text + "' and DueEnd > '" + text2 + "') or  (repeatFlag is not null and repeatFlag != '' )) ";
		return await App.Connection.QueryAsync<CalendarEventModel>(sql, Array.Empty<object>());
	}

	public static async Task<List<CalendarEventModel>> GetEventDerivatives(string id, string uid)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from calEvent in App.Connection.Table<CalendarEventModel>()
			where calEvent.Uid == uid && calEvent.Id != id && calEvent.UserId == userId
			select calEvent).ToListAsync();
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayEventsInCalendar(string calendarId, int type, DateTime start, DateTime end)
	{
		List<CalendarEventModel> events = await GetEventsByCalendarId(calendarId, type);
		bool editable = true;
		BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => cal.Id == calendarId);
		if (calendar != null && calendar.AvailableType != CalendarAvailableType.Editable)
		{
			editable = false;
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (events.Any())
		{
			List<string> hiddenKeys = await ArchivedDao.GetArchivedKeys();
			List<CalendarEventModel> skipEvents = await GetSkipEvents();
			foreach (CalendarEventModel calendarEvent in events)
			{
				if (calendarEvent.ResponseStatusType == ResponseStatusType.Declined)
				{
					continue;
				}
				if (!string.IsNullOrEmpty(calendarEvent.ExDates))
				{
					try
					{
						calendarEvent.ExDateList = JsonConvert.DeserializeObject<List<DateTime>>(calendarEvent.ExDates);
					}
					catch (Exception)
					{
						calendarEvent.ExDateList = null;
					}
				}
				calendarEvent.Editable = editable;
				if (calendar != null)
				{
					calendarEvent.AvailableType = calendar.AvailableType;
				}
				if (!calendarEvent.DueStart.HasValue)
				{
					continue;
				}
				if (!calendarEvent.DueEnd.HasValue)
				{
					calendarEvent.DueEnd = calendarEvent.DueStart.Value.AddDays(1.0);
				}
				DateTime? dateTime = calendarEvent.DueStart;
				DateTime? dateTime2 = calendarEvent.DueEnd;
				if (calendarEvent.IsAllDay)
				{
					dateTime = calendarEvent.DueStart.Value.Date;
					dateTime2 = calendarEvent.DueEnd.Value.Date;
				}
				if ((dateTime >= start.Date || dateTime2 > start.Date) && dateTime2 <= end.Date && (string.IsNullOrEmpty(calendarEvent.RepeatFlag) || !calendarEvent.IsSkipped(skipEvents, dateTime)))
				{
					string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calendarEvent));
					if (!hiddenKeys.Contains(item))
					{
						list.Add(new TaskBaseViewModel(calendarEvent));
					}
				}
				if (string.IsNullOrEmpty(calendarEvent.RepeatFlag))
				{
					continue;
				}
				DateTime calStart = (calendarEvent.IsAllDay ? calendarEvent.DueStart.Value : TimeZoneUtils.LocalToTargetTzTime(calendarEvent.DueStart.Value, calendarEvent.TimeZone));
				double diff = 0.0;
				if (calendarEvent.DueEnd.HasValue)
				{
					diff = (calendarEvent.DueEnd.Value - calendarEvent.DueStart.Value).TotalMinutes;
				}
				List<DateTime> eventRepeatDates = RepeatDao.GetEventRepeatDates(calendarEvent.RepeatFlag, calStart, start.AddDays(-2 - (int)diff / 1440), end.AddDays(1.0), calendarEvent.TimeZone, calendarEvent.IsAllDay, preloadRange: false);
				string deriveId = calendarEvent.Id;
				int num = 0;
				eventRepeatDates.ForEach(delegate(DateTime time)
				{
					calendarEvent.Id = deriveId + "_time_" + num++;
					calendarEvent.DueStart = time;
					if (calendarEvent.DueEnd.HasValue)
					{
						DateTime dateTime3 = (calendarEvent.IsAllDay ? time : TimeZoneUtils.LocalToTargetTzTime(time, calendarEvent.TimeZone));
						calendarEvent.DueEnd = (calendarEvent.IsAllDay ? dateTime3.AddMinutes(diff) : TimeZoneUtils.ToLocalTime(dateTime3.AddMinutes(diff), calendarEvent.TimeZone));
					}
					bool flag = calendarEvent.IsSkipped(skipEvents);
					bool flag2 = calendarEvent.ExDateList != null && calendarEvent.ExDateList.Any() && calendarEvent.ExDateList.Contains(time.Date);
					if ((calendarEvent.DueStart >= DateTime.Today || calendarEvent.DueEnd > DateTime.Today) && !flag && !flag2)
					{
						string item2 = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calendarEvent));
						if (!hiddenKeys.Contains(item2))
						{
							list.Add(new TaskBaseViewModel(calendarEvent));
						}
					}
				});
			}
		}
		return list;
	}

	public static async Task<List<CalendarEventModel>> GetSkipEvents()
	{
		if (_skipEvents == null)
		{
			string text = Utils.GetCurrentUserIdInt().ToString();
			string sql = "select Id,Uid,EventId,OriginalStartTime,IsAllDay,DueStart from CalendarEventModel where Deleted = 0 and userId = '" + text + "' and OriginalStartTime is not null";
			_skipEvents = (await App.Connection.QueryAsync<CalendarEventModel>(sql, Array.Empty<object>())) ?? new List<CalendarEventModel>();
		}
		return _skipEvents?.ToList();
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayEventsInBindAccount(string accountId, DateTime start, DateTime end)
	{
		List<TaskBaseViewModel> models = new List<TaskBaseViewModel>();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<BindCalendarModel> list = (from calendar in CacheManager.GetBindCalendars()
			where calendar.UserId == userId && calendar.AccountId == accountId && calendar.Show != "hidden"
			select calendar).ToList();
		if (list.Any())
		{
			foreach (BindCalendarModel item in list)
			{
				List<TaskBaseViewModel> list2 = await GetDisplayEventsInCalendar(item.Id, 0, start, end);
				if (list2 != null && list2.Any())
				{
					models.AddRange(list2);
				}
			}
		}
		return models;
	}

	public static async Task<CalendarEventModel> GetEventByStartTimeArchiveId(DateTime? start, string archivedId)
	{
		AsyncTableQuery<CalendarEventModel> asyncTableQuery = App.Connection.Table<CalendarEventModel>();
		List<CalendarEventModel> list = ((!start.HasValue) ? (await asyncTableQuery.ToListAsync()) : (await asyncTableQuery.Where((CalendarEventModel x) => x.DueStart == start).ToListAsync()));
		foreach (CalendarEventModel item in list)
		{
			if (!(ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(item)) != archivedId))
			{
				return item;
			}
		}
		return null;
	}

	public static async Task<List<CalendarEventModel>> GetEventsBetweenSpan(DateTime start, DateTime end, bool inCalendar = true)
	{
		DateTime dateTime = start;
		if (!inCalendar && dateTime < DateTime.Today)
		{
			dateTime = DateTime.Today.AddMonths(-1);
		}
		string text = Utils.GetCurrentUserIdInt().ToString();
		List<string> visibleCalendarIds = (from calendar in CacheManager.GetSubscribeCalendars()
			where (!inCalendar) ? (calendar.Show == "show") : (calendar.Show != "hidden")
			select calendar.Id).ToList();
		IEnumerable<string> collection = from calendar in CacheManager.GetBindCalendars()
			where (!inCalendar) ? (calendar.Show == "show" || string.IsNullOrEmpty(calendar.Show)) : (calendar.Show != "hidden")
			select calendar.Id;
		visibleCalendarIds.AddRange(collection);
		if (dateTime > end)
		{
			return new List<CalendarEventModel>();
		}
		string text2 = dateTime.ToString("yyyy-MM-dd HH:mm:ss", App.Ci);
		string text3 = end.AddDays(-1.0).ToString("yyyy-MM-dd 23:59:59", App.Ci);
		string sql = "select * from CalendarEventModel where userId = '" + text + "' and deleted = '0' and ((DueStart is not null and DueStart >= '" + text2 + "' and DueStart < '" + text3 + "') or (DueEnd is not null and DueEnd > '" + text2 + "' and DueEnd < '" + text3 + "')) or (DueStart is not null and DueEnd is not null and DueStart < '" + text2 + "' and DueEnd > '" + text3 + "')";
		List<CalendarEventModel> models = await App.Connection.QueryAsync<CalendarEventModel>(sql, Array.Empty<object>());
		if (models != null && models.Any())
		{
			List<CalendarEventModel> list = models.Where(delegate(CalendarEventModel model)
			{
				bool flag;
				if (model.DueStart.HasValue)
				{
					DateTime dateTime2 = model.DueStart.Value;
					DateTime? dateTime3 = model.DueEnd;
					if (model.IsAllDay)
					{
						dateTime2 = model.DueStart.Value.Date;
						dateTime3 = model.DueEnd?.Date;
					}
					flag = models.Any(delegate(CalendarEventModel cal)
					{
						if (cal.DueStart.HasValue && string.IsNullOrEmpty(cal.EventId) && string.IsNullOrEmpty(model.EventId) && cal.EventId != model.EventId && cal.EventId.StartsWith(model.EventId))
						{
							DateTime value = cal.DueStart.Value;
							DateTime? dueStart = model.DueStart;
							return value == dueStart;
						}
						return false;
					});
					if (visibleCalendarIds.Contains(model.CalendarId))
					{
						if (dateTime2 >= start && dateTime2 <= end)
						{
							goto IL_018b;
						}
						if (dateTime3.HasValue)
						{
							DateTime? dateTime4 = dateTime3;
							DateTime dateTime5 = start;
							if (dateTime4.HasValue && dateTime4.GetValueOrDefault() > dateTime5 && dateTime3 <= end)
							{
								goto IL_018b;
							}
						}
						if (dateTime3.HasValue)
						{
							DateTime? dateTime4 = dateTime3;
							DateTime dateTime5 = end;
							if (dateTime4.HasValue && dateTime4.GetValueOrDefault() > dateTime5 && dateTime2 < start)
							{
								goto IL_018b;
							}
						}
					}
					return false;
				}
				return visibleCalendarIds.Contains(model.CalendarId);
				IL_018b:
				return !flag;
			}).ToList();
			foreach (CalendarEventModel item in list.Where((CalendarEventModel calEvent) => !calEvent.DueEnd.HasValue && calEvent.IsAllDay))
			{
				if (item.DueStart.HasValue)
				{
					item.DueEnd = item.DueStart.Value.AddDays(1.0);
				}
			}
			return list;
		}
		return new List<CalendarEventModel>();
	}

	private static async Task<(int, List<TaskBaseViewModel>)> GetRepeatEventModels(List<CalendarEventModel> events, DateTime start, DateTime end, List<string> archived, List<string> dates = null, LogicType logicType = LogicType.Or, bool onlyCount = false)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		int count = 0;
		if (events.Any())
		{
			List<CalendarEventModel> skipEvents = await GetSkipEvents();
			foreach (CalendarEventModel @event in events)
			{
				if (string.IsNullOrEmpty(@event.RepeatFlag) || !@event.DueStart.HasValue)
				{
					continue;
				}
				double num = 0.0;
				if (@event.DueStart.HasValue && @event.DueEnd.HasValue)
				{
					num = (@event.DueEnd.Value - @event.DueStart.Value).TotalMinutes;
				}
				List<DateTime> repeatFlagDate = RepeatDao.GetRepeatFlagDate(new TaskBaseViewModel
				{
					IsAllDay = @event.IsAllDay,
					StartDate = @event.DueStart,
					DueDate = @event.DueEnd,
					RepeatFlag = @event.RepeatFlag,
					RepeatFrom = "0",
					TimeZoneName = @event.TimeZone
				}, start.AddDays(-2 - (int)num / 1440), end.AddDays(1.0), preloadRange: false);
				string id = @event.Id;
				int num2 = 0;
				if (!string.IsNullOrEmpty(@event.ExDates))
				{
					try
					{
						@event.ExDateList = JsonConvert.DeserializeObject<List<DateTime>>(@event.ExDates);
					}
					catch (Exception)
					{
						@event.ExDateList = null;
					}
				}
				List<DateTime> list2 = @event.ExDateList ?? new List<DateTime>();
				foreach (DateTime item in repeatFlagDate)
				{
					if (!(item < end))
					{
						continue;
					}
					@event.DueStart = item;
					DateTime dateTime = (@event.IsAllDay ? item : TimeZoneUtils.LocalToTargetTzTime(item, @event.TimeZone));
					@event.DueEnd = (@event.IsAllDay ? dateTime.AddMinutes(num) : TimeZoneUtils.ToLocalTime(dateTime.AddMinutes(num), @event.TimeZone));
					if (!CheckEventMatchDateRules(@event, dates, logicType) || (list2.Any() && list2.Contains(item.Date)) || @event.IsSkipped(skipEvents, item) || @event.ResponseStatusType == ResponseStatusType.Declined || ((!(@event.DueStart >= start) || !(@event.DueStart < end)) && (!(@event.DueEnd > start) || !(@event.DueEnd <= end)) && (!(@event.DueEnd > end) || !(@event.DueStart < start))))
					{
						continue;
					}
					string id2 = id + "_time_" + num2++;
					if (!archived.Any() || !archived.Contains(ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(@event))))
					{
						if (!onlyCount)
						{
							list.Add(new TaskBaseViewModel(@event, id2));
						}
						count++;
					}
				}
			}
		}
		return (count, list);
	}

	public static async Task<List<CalendarEventModel>> GetRepeatEvents()
	{
		string text = Utils.GetCurrentUserIdInt().ToString();
		string sql = "select * from calendarEventModel where repeatFlag is not null and repeatFlag !='' and deleted = '0' and userId ='" + text + "'";
		return await App.Connection.QueryAsync<CalendarEventModel>(sql, Array.Empty<object>());
	}

	public static async Task<(int, List<TaskBaseViewModel>)> GetTaskDisplayModelsBetweenSpan(DateTime start, DateTime end, bool inCal = false, List<string> dates = null, string keyWord = null, LogicType logicType = LogicType.Or, bool onlyCount = false, bool checkConnect = true)
	{
		List<CalendarEventModel> events = await GetEventsBetweenSpan(start, end, inCal);
		foreach (CalendarEventModel item2 in events.Where((CalendarEventModel calEvent) => calEvent.IsAllDay && !calEvent.DueEnd.HasValue))
		{
			if (item2.DueStart.HasValue)
			{
				item2.DueEnd = item2.DueStart.Value.AddDays(1.0);
			}
		}
		List<string> archived = await ArchivedDao.GetArchivedKeys();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<string> subCalIds = (from cal in (from cal in CacheManager.GetSubscribeCalendars()
				where (cal.Show == "show" || (inCal && cal.Show != "hidden")) && cal.UserId == userId
				select cal).ToList()
			select cal.Id).ToList();
		List<string> accounts = (from cal in CacheManager.GetBindCalendarAccounts()
			select cal.Id).ToList();
		List<BindCalendarModel> bindCalendars = (from cal in CacheManager.GetBindCalendars()
			where accounts.Contains(cal.AccountId) && (cal.Show == "show" || string.IsNullOrEmpty(cal.Show) || (inCal && cal.Show != "hidden")) && cal.UserId == userId
			select cal).ToList();
		if (checkConnect)
		{
			List<string> connectedCalendars = await CalendarConnectService.GetConnectedCalendarIds();
			bindCalendars.RemoveAll((BindCalendarModel c) => !string.IsNullOrEmpty(c.OutId) && connectedCalendars.Contains(c.OutId));
		}
		subCalIds.AddRange(bindCalendars.Select((BindCalendarModel cal) => cal.Id));
		keyWord = keyWord?.Trim();
		List<string> keys = null;
		if (!string.IsNullOrEmpty(keyWord))
		{
			keys = keyWord.Split(' ').ToList();
			keys.RemoveAll((string k) => k == "");
		}
		(int, List<TaskBaseViewModel>) tuple = await GetRepeatEventModels((await GetRepeatEvents()).Where(delegate(CalendarEventModel calEvent)
		{
			if (!subCalIds.Contains(calEvent.CalendarId))
			{
				return false;
			}
			return keys == null || SearchHelper.KeyWordMatched(calEvent.Title + "\r\n" + calEvent.Content, keyWord, keys);
		}).ToList(), start, end, archived, dates, logicType, onlyCount);
		int repeatCount = tuple.Item1;
		List<TaskBaseViewModel> repeatModels = tuple.Item2;
		List<TaskBaseViewModel> models = new List<TaskBaseViewModel>();
		int num = 0;
		if (events.Any())
		{
			List<CalendarEventModel> skipEvents = await GetSkipEvents();
			foreach (CalendarEventModel item3 in events)
			{
				if (!subCalIds.Contains(item3.CalendarId) || item3.ResponseStatusType == ResponseStatusType.Declined || !item3.DueStart.HasValue || (keys != null && !SearchHelper.KeyWordMatched(item3.Title + "\r\n" + item3.Content, keyWord, keys)) || !CheckEventMatchDateRules(item3, dates, logicType) || item3.IsSkipped(skipEvents))
				{
					continue;
				}
				string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(item3));
				if (!archived.Contains(item))
				{
					if (!onlyCount)
					{
						models.Add(new TaskBaseViewModel(item3));
					}
					num++;
				}
			}
		}
		models.AddRange(repeatModels);
		num += repeatCount;
		return (num, models);
	}

	public static bool CheckEventMatchDateRules(CalendarEventModel model, List<string> dates, LogicType logicType)
	{
		if (dates == null || dates.Count == 0)
		{
			return true;
		}
		bool flag = false;
		foreach (string date in dates)
		{
			if (date == "all")
			{
				flag = true;
				break;
			}
			if (date == "recurring" && !string.IsNullOrEmpty(model.RepeatFlag))
			{
				flag = true;
				break;
			}
			DateTime dateTime = DateTime.Today;
			DateTime dateTime2 = DateTime.Today.AddDays(181.0);
			switch (date)
			{
			case "today":
				dateTime2 = DateTime.Today.AddDays(1.0);
				break;
			case "tomorrow":
				dateTime = DateTime.Today.AddDays(1.0);
				dateTime2 = DateTime.Today.AddDays(2.0);
				break;
			case "thisweek":
			{
				int nextWeekDayDiff = DateUtils.GetNextWeekDayDiff();
				dateTime2 = DateTime.Today.AddDays(nextWeekDayDiff);
				break;
			}
			case "nextweek":
			{
				int nextWeekDayDiff2 = DateUtils.GetNextWeekDayDiff();
				dateTime = DateTime.Today.AddDays(nextWeekDayDiff2);
				dateTime2 = DateTime.Today.AddDays(nextWeekDayDiff2 + 7);
				break;
			}
			case "thismonth":
				dateTime2 = DateTime.Today.AddDays((DateTime.Now.Day - 1) * -1).AddMonths(1);
				break;
			default:
			{
				if (date.EndsWith("days"))
				{
					int.TryParse(date.Replace("days", string.Empty), out var result);
					dateTime2 = DateTime.Today.AddDays(result);
					break;
				}
				if (date.EndsWith("dayslater"))
				{
					int.TryParse(date.Replace("dayslater", string.Empty), out var result2);
					dateTime = DateTime.Today.AddDays(result2);
					break;
				}
				if (date.EndsWith("daysfromtoday"))
				{
					int.TryParse(date.Replace("daysfromtoday", string.Empty), out var result3);
					dateTime = DateTime.Today.AddDays(result3);
					dateTime2 = dateTime.AddDays(1.0);
					break;
				}
				if (date.StartsWith("span"))
				{
					(int?, int?) spanPairInRule = FilterUtils.GetSpanPairInRule(date);
					int? item = spanPairInRule.Item1;
					int? item2 = spanPairInRule.Item2;
					dateTime = ((!item.HasValue) ? dateTime : DateTime.Today.AddDays(item.Value));
					dateTime2 = ((!item2.HasValue) ? dateTime2 : DateTime.Today.AddDays(item2.Value + 1));
					break;
				}
				if (!date.StartsWith("offset"))
				{
					continue;
				}
				(DateTime, DateTime) dateOffsetPair = FilterUtils.GetDateOffsetPair(date);
				DateTime item3 = dateOffsetPair.Item1;
				DateTime item4 = dateOffsetPair.Item2;
				dateTime = item3;
				dateTime2 = item4;
				break;
			}
			case "nodue":
			case "overdue":
				continue;
			}
			if (model.DueEnd.HasValue)
			{
				DateTime? dueStart = model.DueStart;
				DateTime dateTime3 = dateTime;
				if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() >= dateTime3) || !(model.DueStart < dateTime2))
				{
					dueStart = model.DueEnd;
					dateTime3 = dateTime;
					if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() > dateTime3) || !(model.DueEnd <= dateTime2))
					{
						dueStart = model.DueStart;
						dateTime3 = dateTime;
						if (!dueStart.HasValue || !(dueStart.GetValueOrDefault() < dateTime3) || !(model.DueEnd > dateTime2))
						{
							goto IL_04b1;
						}
					}
				}
				flag = true;
				break;
			}
			goto IL_04b1;
			IL_04b1:
			if (!model.DueEnd.HasValue)
			{
				DateTime? dueStart = model.DueStart;
				DateTime dateTime3 = dateTime;
				if (dueStart.HasValue && dueStart.GetValueOrDefault() >= dateTime3 && model.DueStart < dateTime2)
				{
					flag = true;
					break;
				}
			}
		}
		if (logicType != LogicType.Or)
		{
			return !flag;
		}
		return flag;
	}

	public static async Task DeleteBindEvents()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarEventModel> list = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.Type == 0
			select model).ToListAsync();
		if (!list.Any())
		{
			return;
		}
		foreach (CalendarEventModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task MergeCalendarEvents(string calendarId, IEnumerable<CalendarEventModel> events, DateTime start, DateTime end)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarEventModel> list = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.CalendarId == calendarId
			select model).ToListAsync();
		BindCalendarAccountModel account = CacheManager.GetAccountCalById(CacheManager.GetBindCalendarById(calendarId)?.AccountId);
		Dictionary<string, CalendarEventModel> localEventsDict = new Dictionary<string, CalendarEventModel>();
		List<CalendarEventModel> localRepeatEventList = new List<CalendarEventModel>();
		foreach (CalendarEventModel item in list)
		{
			if (string.IsNullOrEmpty(item.Id))
			{
				continue;
			}
			if (!localEventsDict.ContainsKey(item.Id))
			{
				if (!string.IsNullOrEmpty(item.RepeatFlag) && account?.Site == "google" && RepeatUtils.GetValidRepeatDates(item.RepeatFlag, "2", start, start.Date, end.Date, null, item.TimeZone, !item.IsAllDay).Any())
				{
					localRepeatEventList.Add(item);
				}
				localEventsDict.Add(item.Id, item);
			}
			else
			{
				await App.Connection.DeleteAsync(item);
			}
		}
		List<CalendarEventModel> added = new List<CalendarEventModel>();
		List<CalendarEventModel> updated = new List<CalendarEventModel>();
		foreach (CalendarEventModel calEvent in events)
		{
			string id = calEvent.Id;
			calEvent.EventId = id;
			calEvent.CalendarId = calendarId;
			calEvent.Id = id + "@" + calendarId;
			calEvent.UserId = userId;
			calEvent.Type = 0;
			ClearRemoteSnapshotDeleteProtection(calEvent);
			if (calEvent.Attendees != null && calEvent.Attendees.Any())
			{
				calEvent.AttendeeInfo = JsonConvert.SerializeObject(calEvent.Attendees);
			}
			if (calEvent.ReminderList != null && calEvent.ReminderList.Any())
			{
				calEvent.Reminders = JsonConvert.SerializeObject(calEvent.ReminderList);
			}
			if (calEvent.IsAllDay)
			{
				if (calEvent.DueStart.HasValue)
				{
					calEvent.DueStart = calEvent.DueStart.Value.ToUniversalTime();
				}
				if (calEvent.DueEnd.HasValue)
				{
					calEvent.DueEnd = calEvent.DueEnd.Value.ToUniversalTime();
				}
			}
			if (calEvent.ExDateList != null && calEvent.ExDateList.Any())
			{
				calEvent.ExDates = JsonConvert.SerializeObject(calEvent.ExDateList.Select((DateTime date) => TimeZoneUtils.LocalToTargetTzTime(date, calEvent.TimeZone).Date));
			}
			localRepeatEventList.RemoveAll((CalendarEventModel cal) => cal.Id == calEvent.Id);
			if (localEventsDict.ContainsKey(calEvent.Id))
			{
				CalendarEventModel calendarEventModel = localEventsDict[calEvent.Id];
				if (calendarEventModel != null && (calendarEventModel.Etag != calEvent.Etag || calendarEventModel.Recurrence != calEvent.Recurrence))
				{
					calEvent._Id = calendarEventModel._Id;
					updated.Add(calEvent);
				}
				localEventsDict.Remove(calEvent.Id);
			}
			else
			{
				added.Add(calEvent);
			}
		}
		if (added.Any())
		{
			await App.Connection.InsertAllAsync(added);
		}
		if (updated.Any())
		{
			await App.Connection.UpdateAllAsync(updated);
		}
		bool needDelete = false;
		HashSet<string> protectedEventIds = await GetRemoteSnapshotDeleteProtectedEventIds();
		if (localEventsDict.Any())
		{
			Dictionary<string, CalendarEventModel>.ValueCollection values = localEventsDict.Values;
			foreach (CalendarEventModel item2 in values)
			{
				if (ShouldDeleteMissingLocalEvent(item2, start, end, protectedEventIds))
				{
					needDelete = true;
					await App.Connection.DeleteAsync(item2);
				}
			}
		}
		foreach (CalendarEventModel item3 in localRepeatEventList)
		{
			if (!IsRemoteSnapshotDeleteProtected(item3, protectedEventIds))
			{
				needDelete = true;
				await App.Connection.DeleteAsync(item3);
			}
		}
		if (added.Any() || updated.Any() || needDelete)
		{
			_skipEvents = null;
			CalendarEventChangeNotifier.NotifyRemoteChanged();
		}
	}

	public static async Task MergeCalendarEvents(List<BindCalendarModel> calendars, DateTime start, DateTime end)
	{
		if (calendars == null || !calendars.Any())
		{
			return;
		}
		foreach (BindCalendarModel calendar in calendars)
		{
			await MergeCalendarEvents(calendar.Id, calendar.Events, start, end);
		}
	}

	public static async Task DeleteByCalendarId(string calendarId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<CalendarEventModel> list = await (from model in App.Connection.Table<CalendarEventModel>()
			where model.UserId == userId && model.CalendarId == calendarId
			select model).ToListAsync();
		if (!list.Any())
		{
			return;
		}
		foreach (CalendarEventModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task HandleTimeZoneChanged(TimeZoneInfo newTz, TimeZoneInfo oldTz)
	{
		List<CalendarEventModel> obj = await App.Connection.Table<CalendarEventModel>().ToListAsync();
		List<CalendarEventModel> list = new List<CalendarEventModel>();
		foreach (CalendarEventModel item in obj)
		{
			if (item != null && !item.IsAllDay && item.DueStart.HasValue)
			{
				item.DueStart = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(item.DueStart.Value, DateTimeKind.Unspecified), oldTz, newTz);
				if (item.DueEnd.HasValue)
				{
					item.DueEnd = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(item.DueEnd.Value, DateTimeKind.Unspecified), oldTz, newTz);
				}
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			await App.Connection.UpdateAllAsync(list);
		}
	}

	public static BindCalendarAccountModel GetCalendarAccount(string calendarId)
	{
		BindCalendarModel bindCalendarById = CacheManager.GetBindCalendarById(calendarId);
		if (bindCalendarById != null)
		{
			return CacheManager.GetAccountCalById(bindCalendarById.AccountId);
		}
		return null;
	}

	public static async Task<List<CalendarEventModel>> GetAllShowEvents()
	{
		return await App.Connection.QueryAsync<CalendarEventModel>("select * from CalendarEventModel where (CalendarId in (select Id as CalendarId from BindCalendarModel where Show = 'show') or  CalendarId in (select Id as CalendarId from CalendarSubscribeProfileModel where Show = 'show')) and UserId = '" + LocalSettings.Settings.LoginUserId + "' and Deleted = 0 ", Array.Empty<object>());
	}

	public static DateTime? GetEventNextRepeat(CalendarEventModel model, List<CalendarEventModel> skipEvents, List<string> hiddenKeys)
	{
		if (!model.DueStart.HasValue)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(model.ExDates))
		{
			try
			{
				model.ExDateList = JsonConvert.DeserializeObject<List<DateTime>>(model.ExDates);
			}
			catch (Exception)
			{
				model.ExDateList = null;
			}
		}
		double value = 0.0;
		DateTime start = (model.IsAllDay ? model.DueStart.Value : TimeZoneUtils.LocalToTargetTzTime(model.DueStart.Value, model.TimeZone));
		if (model.DueEnd.HasValue)
		{
			value = (model.DueEnd.Value - model.DueStart.Value).TotalMinutes;
		}
		DateTime eventNextRepeatDate = RepeatDao.GetEventNextRepeatDate(model.RepeatFlag, start, model.TimeZone);
		int num = 20;
		while (num > 0)
		{
			num--;
			model.DueStart = eventNextRepeatDate;
			DateTime start2 = (model.IsAllDay ? eventNextRepeatDate : TimeZoneUtils.LocalToTargetTzTime(eventNextRepeatDate, model.TimeZone));
			if (model.DueEnd.HasValue)
			{
				model.DueEnd = (model.IsAllDay ? start2.AddMinutes(value) : eventNextRepeatDate.AddMinutes(value));
			}
			bool flag = false;
			bool flag2 = false;
			if (model.ExDateList != null && model.ExDateList.Any() && model.ExDateList.Contains(start2.Date))
			{
				flag = true;
			}
			if (skipEvents.Any())
			{
				flag2 = model.IsSkipped(skipEvents, eventNextRepeatDate);
			}
			if (!flag && !flag2)
			{
				string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(model));
				if (!hiddenKeys.Contains(item))
				{
					return eventNextRepeatDate;
				}
			}
			eventNextRepeatDate = RepeatDao.GetEventNextRepeatDate(model.RepeatFlag, start2, model.TimeZone);
		}
		return eventNextRepeatDate;
	}
}
