using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.ReminderTime;

public class EventReminderCalculator
{
	private static bool _needReInit;

	private static bool _loading;

	public static async Task InitAllEventsReminderTimes()
	{
		if (ABTestManager.IsNewRemindCalculate())
		{
			await InitAll();
		}
	}

	private static async Task InitAll()
	{
		if (_loading)
		{
			_needReInit = true;
			return;
		}
		_loading = true;
		try
		{
			List<ReminderDelayModel> source = (await ReminderDelayDao.GetDelayModelByType("calendar")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList();
			Dictionary<string, ReminderDelayModel> delayDict = source.ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
			string text = Utils.GetCurrentUserIdInt().ToString();
			List<CalendarEventModel> events = await App.Connection.QueryAsync<CalendarEventModel>("select * from CalendarEventModel where (CalendarId in (select Id as CalendarId from BindCalendarModel where Show = 'show') or  CalendarId in (select Id as CalendarId from CalendarSubscribeProfileModel where Show = 'show')) and UserId = '" + text + "' and Deleted = '0'", Array.Empty<object>());
			List<CalendarEventModel> skipEvents = await CalendarEventDao.GetSkipEvents();
			List<string> hiddenKeys = await ArchivedDao.GetArchivedKeys();
			await ReminderTimeDao.DeleteReminderTimeByType(2);
			if (events == null || !events.Any())
			{
				return;
			}
			foreach (CalendarEventModel item in events)
			{
				if (CacheManager.GetBindCalendarById(item.CalendarId) != null || CacheManager.GetSubscribeCalById(item.CalendarId) != null)
				{
					List<ReminderTimeModel> list = await GetEventReminders(item, delayDict, hiddenKeys, skipEvents);
					if (list != null)
					{
						await ReminderTimeDao.AddReminderTimes(list);
					}
				}
			}
		}
		finally
		{
			_loading = false;
			if (_needReInit)
			{
				_needReInit = false;
				await InitAll();
			}
		}
	}

	public static async Task<List<ReminderTimeModel>> GetEventReminders(CalendarEventModel eve, Dictionary<string, ReminderDelayModel> delayDict, List<string> hiddenKeys, List<CalendarEventModel> skipEvents)
	{
		if (CacheManager.GetBindCalendarById(eve.CalendarId) == null && CacheManager.GetSubscribeCalById(eve.CalendarId) == null)
		{
			return null;
		}
		List<ReminderTimeModel> result = new List<ReminderTimeModel>();
		string text = ((!string.IsNullOrEmpty(eve.EventId)) ? eve.EventId : eve.Id);
		if (eve.DueStart.HasValue)
		{
			ReminderDelayModel delay = null;
			if (!string.IsNullOrEmpty(text) && delayDict.ContainsKey(text))
			{
				delay = delayDict[text];
			}
			string text2 = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(eve));
			if (delay == null && !string.IsNullOrEmpty(text2) && delayDict.ContainsKey(text2))
			{
				delay = delayDict[text2];
			}
			List<int> mins = ((eve.Reminders != null) ? JsonConvert.DeserializeObject<List<int>>(eve.Reminders) : new List<int>());
			if (!hiddenKeys.Contains(text2))
			{
				result.AddRange(await AddReminder(eve, eve.DueStart.Value, delay, mins));
			}
			if (!string.IsNullOrEmpty(eve.RepeatFlag))
			{
				List<DateTime> exDateList = null;
				if (!string.IsNullOrEmpty(eve.ExDates))
				{
					try
					{
						exDateList = JsonConvert.DeserializeObject<List<DateTime>>(eve.ExDates);
					}
					catch (Exception)
					{
					}
				}
				DateTime calStart = (eve.IsAllDay ? eve.DueStart.Value : TimeZoneUtils.LocalToTargetTzTime(eve.DueStart.Value, eve.TimeZone));
				List<DateTime> eventRepeatDates = RepeatDao.GetEventRepeatDates(eve.RepeatFlag, calStart, DateTime.Today.AddDays(-1.0), DateTime.Today.AddDays(4.0), eve.TimeZone, eve.IsAllDay);
				foreach (DateTime item2 in eventRepeatDates)
				{
					CalendarEventModel calendarEventModel = CalendarEventModel.Copy(eve);
					calendarEventModel.DueStart = item2;
					if (eve.DueEnd.HasValue)
					{
						double totalMinutes = (eve.DueEnd.Value - eve.DueStart.Value).TotalMinutes;
						calendarEventModel.DueEnd = item2.AddMinutes(totalMinutes);
					}
					DateTime dateTime = (calendarEventModel.IsAllDay ? item2 : TimeZoneUtils.LocalToTargetTzTime(item2, calendarEventModel.TimeZone));
					bool flag = false;
					bool flag2 = false;
					if (exDateList != null && exDateList.Any() && exDateList.Contains(dateTime.Date))
					{
						flag = true;
					}
					if (skipEvents != null && skipEvents.Any())
					{
						flag2 = calendarEventModel.IsSkipped(skipEvents, item2);
					}
					if (!flag && !flag2)
					{
						string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calendarEventModel));
						if (!hiddenKeys.Contains(item))
						{
							result.AddRange(await AddReminder(calendarEventModel, item2, delay, mins));
						}
					}
				}
			}
		}
		return result;
	}

	private static async Task<List<ReminderTimeModel>> AddReminder(CalendarEventModel ev, DateTime date, ReminderDelayModel delay, List<int> mins)
	{
		string eventId = ((!string.IsNullOrEmpty(ev.EventId)) ? ev.EventId : ev.Id);
		List<string> keys = new List<string>();
		DateTime? dateTime = null;
		List<ReminderTimeModel> result = new List<ReminderTimeModel>();
		DateTime? delayRemindTime = null;
		if (delay != null && delay.RemindTime == date && delay.NextTime.HasValue)
		{
			delayRemindTime = delay.NextTime.Value;
		}
		DateTime? suppressOriginalUntil = ((delayRemindTime >= DateTime.Now) ? delayRemindTime : ((DateTime?)null));
		if (!string.IsNullOrEmpty(ev.Reminders) && (mins?.Any() ?? false))
		{
			foreach (int min in mins)
			{
				DateTime dateTime2 = date.AddMinutes(min * -1);
				if (dateTime2 > DateTime.Now && (!suppressOriginalUntil.HasValue || dateTime2 > suppressOriginalUntil.Value))
				{
					AddReminderTime(dateTime2);
				}
			}
		}
		else
		{
			CalendarSubscribeProfileModel calendarSubscribeProfileModel = await CalendarSubscribeProfileDao.GetProfileById(ev.CalendarId);
			dateTime = ((calendarSubscribeProfileModel == null || calendarSubscribeProfileModel.ReminderEnable == true) ? new DateTime?(date) : ((DateTime?)null));
		}
		if (delayRemindTime.HasValue && delayRemindTime.Value >= DateTime.Now)
		{
			AddReminderTime(delayRemindTime.Value);
		}
		if (dateTime.HasValue)
		{
			DateTime value = dateTime.Value;
			if (value >= DateTime.Now && (!suppressOriginalUntil.HasValue || value > suppressOriginalUntil.Value))
			{
				AddReminderTime(value);
			}
		}
		return result;
		void AddReminderTime(DateTime remindTime)
		{
			string item = $"{eventId}_{remindTime.Ticks}";
			if (!keys.Contains(item))
			{
				result.Add(new ReminderTimeModel(eventId, 2, remindTime.Ticks));
				keys.Add(item);
			}
		}
	}

	public static async Task RecalEventReminders(CalendarEventModel eve)
	{
		if (eve == null || !ABTestManager.IsNewRemindCalculate())
		{
			return;
		}
		await ReminderTimeDao.DeleteReminderTimeByIdAndType(string.IsNullOrEmpty(eve.EventId) ? eve.Id : eve.EventId, 2);
		if (eve.Deleted != 1)
		{
			List<ReminderDelayModel> source = (await ReminderDelayDao.GetDelayModelByType("calendar")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList();
			List<ReminderTimeModel> list = await GetEventReminders(delayDict: source.ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d), skipEvents: await CalendarEventDao.GetSkipEvents(), eve: eve, hiddenKeys: await ArchivedDao.GetArchivedKeys());
			if (list != null)
			{
				await ReminderTimeDao.AddReminderTimes(list);
			}
		}
	}
}
