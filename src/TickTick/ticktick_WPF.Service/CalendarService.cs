using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Calendar;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Service;

public static class CalendarService
{
	private enum MoveType
	{
		Arrange,
		ArrangeToDay,
		Day2Point,
		Point2Day,
		Day2Day,
		Point2Point,
		OverPoint2OverPoint,
		OverPoint2Point
	}

	public static bool IsPulling;

	private static bool _isPullingEvents;

	private static BlockingSet<string> _loged = new BlockingSet<string>();

	public static async Task TryPullAccountCacheEvents(string accountId)
	{
		try
		{
			await PullAccountCacheEvents(accountId);
		}
		catch (Exception)
		{
		}
	}

	public static async Task PullAccountCacheEvents(string accountId)
	{
		if (!UserDao.IsUserValid())
		{
			return;
		}
		CalendarEventData calendarEventData = await Communicator.GetCacheEventsByAccountId(accountId);
		if (calendarEventData == null)
		{
			return;
		}
		List<BindCalendarModel> events = calendarEventData.events;
		DateTime pullStart = calendarEventData.begin;
		DateTime pullEnd = calendarEventData.end;
		if (events != null && events.Any())
		{
			foreach (BindCalendarModel calendar in events)
			{
				calendar.AccountId = accountId;
				await BindCalendarAccountDao.SaveCalendarInfo(calendar, accountId);
				await CalendarEventDao.MergeCalendarEvents(calendar.Id, calendar.Events, pullStart, pullEnd);
			}
		}
		BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(accountId);
		if (accountCalById != null && accountCalById.Expired)
		{
			accountCalById.Expired = false;
			await BindCalendarAccountDao.SaveBindCalendarAccount(accountCalById, handleCalendar: false);
		}
	}

	public static async Task PullAccountEvents(string accountId, string kind, string site)
	{
		if (_isPullingEvents || !UserDao.IsUserValid())
		{
			return;
		}
		_isPullingEvents = true;
		try
		{
			switch (kind?.ToLower())
			{
			case "caldav":
			case "icloud":
				await PullBindCalEvents(accountId, null, null, Constants.BindAccountType.CalDAV);
				_isPullingEvents = false;
				return;
			case "exchange":
				await PullBindCalEvents(accountId, null, null, Constants.BindAccountType.Exchange);
				_isPullingEvents = false;
				return;
			default:
			{
				DateTime pullStart = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(-1);
				DateTime pullEnd = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(5);
				List<BindCalendarModel> list;
				if (site != null && site.ToLower() == "outlook")
				{
					CalendarEventData calendarEventData = await PullOutlookCalendars(null, null);
					if (calendarEventData == null)
					{
						_isPullingEvents = false;
						return;
					}
					if (calendarEventData.errorIds.Contains(accountId))
					{
						return;
					}
					list = calendarEventData.events;
					pullStart = calendarEventData.begin;
					pullEnd = calendarEventData.end;
				}
				else
				{
					list = await Communicator.GetBindCalendarEvents(accountId);
				}
				if (list != null && list.Any())
				{
					foreach (BindCalendarModel calendar in list)
					{
						await BindCalendarAccountDao.SaveCalendarInfo(calendar, (site.ToLower() == "outlook") ? null : accountId);
						await CalendarEventDao.MergeCalendarEvents(calendar.Id, calendar.Events, pullStart, pullEnd);
					}
				}
				await HandleCalendarUnExpired(accountId);
				break;
			}
			}
		}
		catch (CustomException.CalendarExpiredException ex)
		{
			_ = ex;
			await HandleCalendarExpired(accountId);
		}
		if (ABTestManager.IsNewRemindCalculate())
		{
			EventReminderCalculator.InitAllEventsReminderTimes();
		}
		else
		{
			ReminderCalculator.AssembleReminders();
		}
		_isPullingEvents = false;
	}

	private static async Task HandleCalendarExpired(string accountId)
	{
		BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(accountId);
		if (accountCalById != null && !accountCalById.Expired)
		{
			await BindCalendarAccountDao.HandleAccountExpired(accountCalById);
			DataChangedNotifier.NotifyCalendarChanged(50);
		}
	}

	private static async Task HandleCalendarUnExpired(string accountId)
	{
		BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(accountId);
		if (accountCalById != null && accountCalById.Expired)
		{
			accountCalById.Expired = false;
			await BindCalendarAccountDao.SaveBindCalendarAccount(accountCalById, handleCalendar: false);
			DataChangedNotifier.NotifyCalendarChanged(200);
		}
	}

	public static async Task PullCalendarConnects()
	{
		if (Utils.IsNetworkAvailable())
		{
			ThirdAccountsModel thirdAccountsModel = await Communicator.GetCalendarThirdAccounts();
			if (thirdAccountsModel != null)
			{
				await CalendarConnectService.SaveConnects(thirdAccountsModel.connects);
			}
		}
	}

	public static async Task PullAccountCalendarsAndEvents(DateTime? start = null, DateTime? end = null, string accountId = "")
	{
		if (!UserDao.IsUserValid())
		{
			IsPulling = false;
			return;
		}
		if (IsPulling && string.IsNullOrEmpty(accountId))
		{
			IsPulling = false;
			return;
		}
		IsPulling = true;
		ThirdAccountsModel model = await Communicator.GetCalendarThirdAccounts();
		if (model != null)
		{
			await CalendarConnectService.SaveConnects(model.connects);
			List<BindCalendarAccountModel> accounts = model.accounts;
			if (accounts == null || !accounts.Any())
			{
				await BindCalendarAccountDao.DeleteBindAccounts();
			}
			else
			{
				await BindCalendarAccountDao.SaveBindCalendarAccounts(accounts);
				foreach (BindCalendarAccountModel account in accounts)
				{
					if (!string.IsNullOrEmpty(accountId) && account.Id != accountId)
					{
						continue;
					}
					if (!start.HasValue || !end.HasValue)
					{
						switch (account.Kind)
						{
						case "caldav":
						case "icloud":
							await PullBindCalEvents(account.Id, start, end, Constants.BindAccountType.CalDAV);
							break;
						case "exchange":
							await PullBindCalEvents(account.Id, start, end, Constants.BindAccountType.Exchange);
							break;
						}
						string site = account.Site;
						if (!(site == "outlook"))
						{
							if (site == "google")
							{
								await PullGoogleAllEvents(start, end);
							}
						}
						else
						{
							await PullOutlookEvents(start, end);
						}
						continue;
					}
					List<(DateTime, DateTime)> list = SplitIntoTwoMonthIntervals(start.Value, end.Value);
					foreach (var interval in list)
					{
						switch (account.Kind)
						{
						case "caldav":
						case "icloud":
							await PullBindCalEvents(account.Id, interval.Item1, interval.Item2, Constants.BindAccountType.CalDAV);
							break;
						case "exchange":
							await PullBindCalEvents(account.Id, interval.Item1, interval.Item2, Constants.BindAccountType.Exchange);
							break;
						}
						string site = account.Site;
						if (!(site == "outlook"))
						{
							if (site == "google")
							{
								await PullGoogleAllEvents(interval.Item1, interval.Item2);
							}
						}
						else
						{
							await PullOutlookEvents(interval.Item1, interval.Item2);
						}
					}
				}
				if (!string.IsNullOrEmpty(accountId))
				{
					return;
				}
			}
		}
		ThreadUtil.DetachedRunOnUiBackThread(delegate
		{
			if (ABTestManager.IsNewRemindCalculate())
			{
				EventReminderCalculator.InitAllEventsReminderTimes();
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
		});
		IsPulling = false;
	}

	private static List<(DateTime Start, DateTime End)> SplitIntoTwoMonthIntervals(DateTime start, DateTime end)
	{
		List<(DateTime, DateTime)> list = new List<(DateTime, DateTime)>();
		if (start > end)
		{
			return list;
		}
		DateTime dateTime = start;
		while (dateTime < end)
		{
			DateTime dateTime2 = dateTime.AddMonths(2);
			if (dateTime2 > end)
			{
				dateTime2 = end;
			}
			list.Add((dateTime, dateTime2));
			dateTime = dateTime2;
			if (dateTime2 >= end)
			{
				break;
			}
		}
		return list;
	}

	public static async Task PullBindCalEvents(string id, DateTime? pullStart, DateTime? pullEnd, Constants.BindAccountType type)
	{
		_ = 4;
		try
		{
			List<BindCalendarModel> list = null;
			switch (type)
			{
			case Constants.BindAccountType.CalDAV:
				list = await Communicator.GetBindCalDavEvents(id, pullStart, pullEnd);
				break;
			case Constants.BindAccountType.Exchange:
				list = await Communicator.GetExchangeEvents(id, pullStart, pullEnd);
				break;
			}
			pullStart = pullStart ?? DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(-1);
			pullEnd = pullEnd ?? DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(5);
			if (list != null && list.Any())
			{
				foreach (BindCalendarModel calendar in list)
				{
					calendar.AccountId = id;
					await BindCalendarAccountDao.SaveCalendarInfo(calendar, id);
					await CalendarEventDao.MergeCalendarEvents(calendar.Id, calendar.Events, pullStart.Value, pullEnd.Value);
				}
			}
			BindCalendarAccountModel accountCalById = CacheManager.GetAccountCalById(id);
			if (accountCalById != null && accountCalById.Expired)
			{
				accountCalById.Expired = false;
				await BindCalendarAccountDao.SaveBindCalendarAccount(accountCalById, handleCalendar: false);
			}
		}
		catch
		{
			HandleCalendarExpired(id);
		}
	}

	private static async Task<CalendarEventData> PullOutlookCalendars(DateTime? pullStart, DateTime? pullEnd)
	{
		CalendarEventData calendarEventData = await Communicator.GetBindOutlookEvents(pullStart, pullEnd);
		if (calendarEventData != null && calendarEventData.errorIds?.Count > 0)
		{
			foreach (string errorId in calendarEventData.errorIds)
			{
				HandleCalendarExpired(errorId);
			}
		}
		if (calendarEventData?.events?.Any() == true)
		{
			(from e in calendarEventData.events.DistinctEx((BindCalendarModel e) => e.AccountId)
				select e.AccountId).ToList().ForEach(delegate(string id)
			{
				HandleCalendarUnExpired(id);
			});
		}
		return calendarEventData;
	}

	private static async Task PullOutlookEvents(DateTime? pullStart, DateTime? pullEnd)
	{
		CalendarEventData outlook = await PullOutlookCalendars(pullStart, pullEnd);
		List<BindCalendarModel> list = outlook?.events;
		if (list == null || !list.Any())
		{
			return;
		}
		int order = 0;
		foreach (BindCalendarModel calendar in list)
		{
			calendar.sortOrder = order++;
			await BindCalendarAccountDao.SaveCalendarInfo(calendar, calendar.AccountId);
			await CalendarEventDao.MergeCalendarEvents(calendar.Id, calendar.Events, outlook.begin, outlook.end);
		}
	}

	private static async Task PullGoogleAllEvents(DateTime? start, DateTime? end)
	{
		BindCalendarsCollection bindCalendarsCollection = ((!start.HasValue || !end.HasValue) ? (await Communicator.GetBindCalendarEvents()) : (await Communicator.GetBindCalendarEvents(start.Value, end.Value)));
		BindCalendarsCollection calendarEvents = bindCalendarsCollection;
		if (calendarEvents?.ErrorIds != null && calendarEvents.ErrorIds.Any())
		{
			foreach (string errorId in calendarEvents.ErrorIds)
			{
				HandleCalendarExpired(errorId);
			}
		}
		if (calendarEvents?.Events != null && calendarEvents.Events.Any())
		{
			DateTime start2 = start ?? DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(-1);
			DateTime end2 = end ?? DateTime.Now.Date.AddDays(1 - DateTime.Now.Day).AddMonths(5);
			await CalendarEventDao.MergeCalendarEvents(calendarEvents.Events, start2, end2);
			(from e in calendarEvents.Events.DistinctEx((BindCalendarModel e) => e.AccountId)
				select e.AccountId).ToList().ForEach(delegate(string id)
			{
				HandleCalendarUnExpired(id);
			});
		}
	}

	public static async Task PullSubscribeCalendars()
	{
		List<CalendarSubscribeProfileModel> list = await Communicator.GetCalendarSubscriptions();
		if (list != null)
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			list.ForEach(delegate(CalendarSubscribeProfileModel sub)
			{
				sub.UserId = userId;
			});
			await CalendarSubscribeProfileDao.SaveProfiles(list);
		}
	}

	public static async Task NotifyTaskDrop(TimeDataModel original, TaskCellViewModel model, TimeDataModel merged)
	{
		if (merged != null)
		{
			await TaskService.SetDateTime(merged);
		}
		if (!original.StartDate.HasValue || original.IsAllDay != merged?.IsAllDay)
		{
			TaskModel obj = new TaskModel
			{
				id = model.TaskId
			};
			obj.reminders = ((merged == null || !merged.IsAllDay.HasValue || merged.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray());
			await TaskService.SaveTaskReminders(obj);
		}
	}

	public static async Task NotifyCheckItemDrop(TimeDataModel date, TaskCellViewModel model)
	{
		if (date != null && date.StartDate.HasValue)
		{
			await TaskService.SetCheckItemDate(model.GetTaskId(), model.ItemId, date);
		}
	}

	private static MoveType GetMoveType(TimeDataModel original, TimeDataModel delta)
	{
		if (!original.StartDate.HasValue)
		{
			return MoveType.Arrange;
		}
		bool flag = original.IsAllDay ?? true;
		bool flag2 = delta.IsAllDay ?? true;
		if (original.StartDate.HasValue && original.DueDate.HasValue && (original.DueDate.Value - original.StartDate.Value).TotalHours > 24.0)
		{
			if (!flag)
			{
				if (!flag2)
				{
					TimeSpan? timeSpan = delta.DueDate - delta.StartDate;
					if (!timeSpan.HasValue || !(timeSpan.GetValueOrDefault().TotalHours > 24.0))
					{
						goto IL_00fe;
					}
				}
				return MoveType.OverPoint2OverPoint;
			}
			goto IL_00fe;
		}
		goto IL_0103;
		IL_0103:
		if (flag && !flag2)
		{
			return MoveType.Day2Point;
		}
		if (!flag && flag2)
		{
			return MoveType.Point2Day;
		}
		if (flag)
		{
			return MoveType.Day2Day;
		}
		return MoveType.Point2Point;
		IL_00fe:
		if (!flag)
		{
			return MoveType.OverPoint2Point;
		}
		goto IL_0103;
	}

	public static TimeDataModel GetMergedData(TimeDataModel original, TimeDataModel delta)
	{
		TimeDataModel timeDataModel = new TimeDataModel
		{
			TaskId = original.TaskId,
			ItemId = original.ItemId,
			RepeatFlag = original.RepeatFlag,
			RepeatFrom = original.RepeatFrom
		};
		switch (GetMoveType(original, delta))
		{
		case MoveType.Arrange:
			timeDataModel.IsAllDay = delta.IsAllDay;
			timeDataModel.StartDate = delta.StartDate;
			timeDataModel.DueDate = null;
			timeDataModel.HandleReminderMode = ReminderMode.SetDefault;
			break;
		case MoveType.ArrangeToDay:
			timeDataModel.IsAllDay = false;
			if (original.StartDate.HasValue && delta.StartDate.HasValue)
			{
				timeDataModel.StartDate = DateUtils.SetDateOnly(original.StartDate.Value, delta.StartDate.Value);
				if (original.DueDate.HasValue && original.StartDate.HasValue && delta.StartDate.HasValue)
				{
					double totalMinutes2 = (original.DueDate.Value - original.StartDate.Value).TotalMinutes;
					timeDataModel.DueDate = timeDataModel.StartDate.Value.AddMinutes(totalMinutes2);
				}
			}
			timeDataModel.HandleReminderMode = ReminderMode.SetDefault;
			break;
		case MoveType.Day2Point:
			timeDataModel.IsAllDay = false;
			timeDataModel.StartDate = delta.StartDate;
			timeDataModel.DueDate = null;
			timeDataModel.HandleReminderMode = ReminderMode.KeepDay;
			break;
		case MoveType.Point2Day:
			timeDataModel.IsAllDay = true;
			timeDataModel.StartDate = delta.StartDate;
			timeDataModel.DueDate = null;
			timeDataModel.HandleReminderMode = ReminderMode.SetDefault;
			break;
		case MoveType.Day2Day:
			timeDataModel.IsAllDay = true;
			timeDataModel.StartDate = delta.StartDate;
			if (original.DueDate.HasValue && original.StartDate.HasValue && delta.StartDate.HasValue)
			{
				double totalDays = (original.DueDate.Value - original.StartDate.Value).TotalDays;
				timeDataModel.DueDate = delta.StartDate.Value.AddDays(totalDays);
			}
			timeDataModel.HandleReminderMode = ReminderMode.None;
			break;
		case MoveType.Point2Point:
			timeDataModel.IsAllDay = false;
			if (original.StartDate.HasValue && delta.StartDate.HasValue)
			{
				timeDataModel.StartDate = delta.StartDate.Value;
				if (original.DueDate.HasValue && original.StartDate.HasValue && delta.StartDate.HasValue)
				{
					double totalMinutes3 = (original.DueDate.Value - original.StartDate.Value).TotalMinutes;
					timeDataModel.DueDate = timeDataModel.StartDate.Value.AddMinutes(totalMinutes3);
				}
			}
			timeDataModel.HandleReminderMode = ReminderMode.None;
			break;
		case MoveType.OverPoint2OverPoint:
			timeDataModel.IsAllDay = false;
			if (original.StartDate.HasValue && delta.StartDate.HasValue)
			{
				timeDataModel.StartDate = DateUtils.SetDateOnly(original.StartDate.Value, delta.StartDate.Value);
				if (original.DueDate.HasValue && original.StartDate.HasValue && delta.StartDate.HasValue)
				{
					double totalMinutes = (original.DueDate.Value - original.StartDate.Value).TotalMinutes;
					timeDataModel.DueDate = timeDataModel.StartDate.Value.AddMinutes(totalMinutes);
				}
			}
			timeDataModel.HandleReminderMode = ReminderMode.None;
			break;
		case MoveType.OverPoint2Point:
			timeDataModel.IsAllDay = false;
			timeDataModel.StartDate = delta.StartDate;
			timeDataModel.DueDate = null;
			timeDataModel.HandleReminderMode = ReminderMode.None;
			break;
		}
		return timeDataModel;
	}

	public static async Task SetEventDate(string eventId, DateTime startDate, DateTime endDate, bool onlyEnd)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventById(eventId);
		if (calEvent != null)
		{
			calEvent.DueEnd = endDate;
			await CalendarEventDao.SaveEvent(calEvent);
			await SyncStatusDao.AddEventModifySyncStatus(calEvent.EventId);
		}
	}

	public static async Task SaveEventTime(string eventId, TimeData time)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventById(eventId);
		if (calEvent == null)
		{
			return;
		}
		HashSet<string> delayKeys = GetCalendarDelayKeys(calEvent);
		bool isRepeatEvent = !string.IsNullOrEmpty(calEvent.RepeatFlag);
		bool flag = time.IsAllDay ?? true;
		bool flag2 = time.StartDate != calEvent.DueStart || time.DueDate != calEvent.DueEnd || time.RepeatFlag != calEvent.RepeatFlag || flag != calEvent.IsAllDay;
		bool flag3 = false;
		if (time.Reminders.Any())
		{
			List<int> list = new List<int>();
			foreach (TaskReminderModel reminder in time.Reminders)
			{
				int item = TriggerUtils.TriggerToReminder(reminder.trigger);
				list.Add(item);
			}
			string text = JsonConvert.SerializeObject(list);
			flag3 = text != calEvent.Reminders;
			if (flag3)
			{
				calEvent.Reminders = text;
			}
		}
		else if (calEvent.ReminderList != null && calEvent.ReminderList.Any())
		{
			flag3 = true;
			calEvent.ReminderList = null;
		}
		if (!flag2 && !flag3)
		{
			return;
		}
		calEvent.IsAllDay = time.IsAllDay ?? true;
		if (calEvent.IsAllDay && time.StartDate.HasValue)
		{
			double num = DateTimeOffset.Now.Offset.TotalMinutes;
			if (num < 0.0)
			{
				num = 0.0;
			}
			calEvent.DueStart = time.StartDate.Value.AddMinutes(num);
			calEvent.DueEnd = time.DueDate?.AddMinutes(num) ?? calEvent.DueStart.Value.AddDays(1.0);
		}
		else
		{
			calEvent.DueStart = time.StartDate;
			calEvent.DueEnd = time.DueDate ?? time.StartDate;
		}
		calEvent.RepeatFlag = time.RepeatFlag;
		calEvent.ExDates = string.Empty;
		calEvent.ExDateList = null;
		await CalendarEventDao.SaveEvent(calEvent);
		await SyncStatusDao.AddEventModifySyncStatus(calEvent.EventId);
		delayKeys.UnionWith(GetCalendarDelayKeys(calEvent));
		await DeleteCalendarDelayAsync(delayKeys);
		if (ABTestManager.IsNewRemindCalculate())
		{
			await EventReminderCalculator.RecalEventReminders(calEvent);
		}
		else
		{
			ReminderCalculator.AssembleReminders();
		}
		if (isRepeatEvent)
		{
			await HandleDerivativeOnTimeChanged(eventId, calEvent.Uid);
		}
		await Task.Delay(100);
		SyncManager.TryDelaySync();
		CalendarEventChangeNotifier.NotifyEventChanged(calEvent);
	}

	private static HashSet<string> GetCalendarDelayKeys(CalendarEventModel calEvent)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (calEvent == null)
		{
			return hashSet;
		}
		if (!string.IsNullOrEmpty(calEvent.Id))
		{
			hashSet.Add(calEvent.Id);
		}
		if (!string.IsNullOrEmpty(calEvent.EventId))
		{
			hashSet.Add(calEvent.EventId);
		}
		hashSet.Add(ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calEvent)));
		return hashSet;
	}

	private static async Task DeleteCalendarDelayAsync(IEnumerable<string> keys)
	{
		foreach (string item in keys.Where((string k) => !string.IsNullOrEmpty(k)))
		{
			await ReminderDelayDao.DeleteByIdAsync(item, "calendar");
		}
	}

	private static async Task HandleDerivativeOnTimeChanged(string id, string uid)
	{
		List<CalendarEventModel> list = await CalendarEventDao.GetEventDerivatives(id, uid);
		if (!list.Any())
		{
			return;
		}
		foreach (CalendarEventModel calEvent in list)
		{
			await DeleteEvent(calEvent.Id);
			await SyncStatusDao.AddEventDeletedSyncStatus(calEvent.EventId);
		}
	}

	public static async Task MoveCalendar(string eventId, string calendarId)
	{
		CalendarEventModel model = await CalendarEventDao.GetEventById(eventId);
		if (model == null)
		{
			return;
		}
		BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel item) => item.Id == calendarId);
		if (calendar == null)
		{
			return;
		}
		if (CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel item) => item.Id == calendar.AccountId).IsCalDav())
		{
			string text = Guid.NewGuid().ToString();
			string id = text + "@" + model.CalendarId;
			CalendarEventModel calendarEventModel = CalendarEventModel.Copy(model);
			calendarEventModel.Id = id;
			calendarEventModel.EventId = text;
			calendarEventModel.Uid = text;
			calendarEventModel.CalendarId = calendarId;
			await AddEvent(calendarEventModel);
			await DeleteEvent(model.Id);
			await SyncStatusDao.AddEventDeletedSyncStatus(model.EventId);
		}
		else
		{
			string fromCalendarId = model.CalendarId;
			if (fromCalendarId != calendarId)
			{
				string originalId = model.Id;
				model.CalendarId = calendarId;
				model.Id = model.EventId + "@" + calendarId;
				await CalendarEventDao.SaveEvent(model);
				await SyncStatusDao.AddMoveEventStatus(model.EventId, fromCalendarId);
				await TaskSortOrderInDateDao.SyncOnEventCalendarChanged(originalId, model.Id);
				await TaskSortOrderInPriorityDao.SyncOnEventCalendarChanged(originalId, model.Id);
			}
		}
	}

	public static async Task SaveEventContent(string eventId, string content)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventById(eventId);
		if (calEvent != null && calEvent.Content != content)
		{
			calEvent.Content = content;
			await CalendarEventDao.SaveEvent(calEvent);
			await SyncStatusDao.AddEventModifySyncStatus(calEvent.EventId);
		}
	}

	public static async Task SaveEventTitle(string eventId, string title, bool notifyChanged = true)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventById(eventId);
		if (calEvent != null && calEvent.Title != title)
		{
			calEvent.Title = title;
			await CalendarEventDao.SaveEvent(calEvent);
			await SyncStatusDao.AddEventModifySyncStatus(calEvent.EventId);
			if (notifyChanged)
			{
				CalendarEventChangeNotifier.NotifyTitleChanged(eventId, title);
			}
			SyncManager.TryDelaySync(3000);
		}
	}

	public static async void SaveEventLocation(string eventId, string location)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventById(eventId);
		if (calEvent != null && calEvent.Location != location)
		{
			calEvent.Location = location;
			await CalendarEventDao.SaveEvent(calEvent);
			await SyncStatusDao.AddEventModifySyncStatus(calEvent.EventId);
		}
	}

	public static async Task AddEvent(CalendarEventModel calEvent)
	{
		await CalendarEventDao.AddEvent(calEvent);
		CalendarEventDao.ProtectFromRemoteSnapshotDelete(calEvent);
		await EventReminderCalculator.RecalEventReminders(calEvent);
		await SyncStatusDao.AddEventCreateSyncStatus(calEvent.EventId);
	}

	public static async Task UndoDeleteEvent(string eventId)
	{
		CalendarEventModel calEvent = await CalendarEventDao.GetEventByEventId(eventId);
		if (calEvent != null)
		{
			calEvent.Deleted = 0;
			await CalendarEventDao.SaveEvent(calEvent);
			await EventReminderCalculator.RecalEventReminders(calEvent);
			CalendarEventChangeNotifier.NotifyEventDeleted(eventId);
		}
	}

	public static async Task DeleteEvent(string eventId)
	{
		CalendarEventModel model = await CalendarEventDao.GetEventById(eventId);
		if (model != null)
		{
			model.Deleted = 1;
			await CalendarEventDao.SaveEvent(model);
			await DeleteCalendarDelayAsync(GetCalendarDelayKeys(model));
			await EventReminderCalculator.RecalEventReminders(model);
			CalendarEventChangeNotifier.NotifyEventDeleted(eventId);
		}
	}

	public static async Task PullSubscribeEventsByCalIdAsync(string calendarId)
	{
		SubscribeCalendarModel subscribeCalendarModel = await Communicator.GetSubscribeEventsByCalId(calendarId);
		if (subscribeCalendarModel?.events == null)
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (CalendarEventModel eve in subscribeCalendarModel.events)
		{
			eve.UserId = userId;
			eve.CalendarId = calendarId;
			eve.EventId = eve.Id;
			eve.Type = 1;
			if (string.IsNullOrEmpty(eve.TimeZone))
			{
				eve.TimeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
			}
			if (eve.ExDateList != null && eve.ExDateList.Any())
			{
				eve.ExDates = JsonConvert.SerializeObject(eve.ExDateList.Select((DateTime date) => TimeZoneUtils.LocalToTargetTzTime(date, eve.TimeZone).Date));
			}
			if (eve.ReminderList != null && eve.ReminderList.Any())
			{
				eve.Reminders = JsonConvert.SerializeObject(eve.ReminderList);
			}
			if (eve.IsAllDay)
			{
				eve.DueStart = eve.DueStart?.ToUniversalTime();
				eve.DueEnd = eve.DueEnd?.ToUniversalTime();
			}
		}
		await CalendarEventDao.SaveEvents(subscribeCalendarModel.events, calendarId);
	}

	public static void TryLogResult(string calendarId, string result)
	{
		if (!_loged.Contains(calendarId))
		{
			UtilLog.Admin(result);
			_loged.Add(calendarId);
		}
	}

	public static async Task CheckExpiredAccounts()
	{
		int dateNum = DateUtils.GetDateNum(DateTime.Today.AddDays(-6.0));
		if (LocalSettings.Settings.ExtraSettings.BindCalendarExpireCheckTime >= dateNum)
		{
			return;
		}
		UtilLog.Info("BeforeNotifyExpired  " + LocalSettings.Settings.ExtraSettings.BindCalendarExpireCheckTime);
		LocalSettings.Settings.ExtraSettings.BindCalendarExpireCheckTime = DateUtils.GetDateNum(DateTime.Today);
		LocalSettings.Settings.Save(force: true);
		List<BindCalendarAccountModel> expired = (await BindCalendarAccountDao.GetBindCalendarAccounts()).Where((BindCalendarAccountModel a) => a.Expired).ToList();
		List<CalendarSubscribeProfileModel> expiredCalendars = (await CalendarSubscribeProfileDao.GetProfiles()).Where((CalendarSubscribeProfileModel a) => a.Expired).ToList();
		List<CalendarConnectInfo> expiredConnectInfos = (await CalendarConnectInfoDao.GetAllConnectsAsync()).Where((CalendarConnectInfo a) => a.status == 1).ToList();
		if (expired.Count > 0 || expiredCalendars.Count > 0 || expiredConnectInfos.Count > 0)
		{
			Application.Current.Dispatcher.Invoke(delegate
			{
				SubscribeExpiredWindow.TryShowWindow(expired, expiredCalendars, expiredConnectInfos, setCheckTime: false);
			});
		}
		UtilLog.Info("AfterNotifyExpired  " + LocalSettings.Settings.ExtraSettings.BindCalendarExpireCheckTime);
	}

	public static async Task ResubscribeCalendar(string accountId, Window owner, Action<string> toast)
	{
		BindCalendarAccountModel bindCalendarAccountModel = await BindCalendarAccountDao.GetBindCalendarAccount(accountId);
		if (bindCalendarAccountModel == null)
		{
			return;
		}
		if (bindCalendarAccountModel.IsBindAccountPassword())
		{
			BindAccountWindow bindAccountWindow = new BindAccountWindow(bindCalendarAccountModel);
			bindAccountWindow.Owner = owner;
			bindAccountWindow.ShowDialog();
		}
		else if (bindCalendarAccountModel.Site == "outlook")
		{
			BindOutlookWindow bindOutlookWindow = new BindOutlookWindow();
			bindOutlookWindow.SetOriginAccount(bindCalendarAccountModel.Id);
			bindOutlookWindow.ShowDialog();
		}
		else if (bindCalendarAccountModel.Site == "google")
		{
			BindGoogleAccount instance = BindGoogleAccount.GetInstance();
			instance.Start(bindCalendarAccountModel.Id);
			instance.CalendarConnected += delegate
			{
				toast?.Invoke(Utils.GetString("Integrated"));
			};
			instance.ToastText += delegate(object sender2, string s)
			{
				toast?.Invoke(Utils.GetString(s));
			};
		}
		else if (bindCalendarAccountModel.Site == "icloud")
		{
			BindICloudWindow bindICloudWindow = new BindICloudWindow(null, bindCalendarAccountModel);
			bindICloudWindow.Owner = owner;
			bindICloudWindow.Topmost = false;
			bindICloudWindow.ShowDialog();
		}
	}
}
