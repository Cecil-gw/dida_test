using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Dal;

public static class TaskReminderDao
{
	private static readonly List<string> OnTimeList = new List<string> { "TRIGGER:PT0S", "TRIGGER:-PT0S" };

	public static async Task<ObservableCollection<TaskReminderModel>> GetAllReminders()
	{
		return await Task.Run(async () => new ObservableCollection<TaskReminderModel>(await App.Connection.Table<TaskReminderModel>().ToListAsync()));
	}

	public static async Task<List<TaskReminderModel>> GetRemindersByTaskId(string taskServerId)
	{
		return await Task.Run(async () => (await (from v in App.Connection.Table<TaskReminderModel>()
			where v.taskserverid == taskServerId
			select v).ToListAsync())?.DistinctEx((TaskReminderModel m) => m.trigger));
	}

	public static async Task<List<TaskReminderModel>> GetRemindersByTaskIds(List<string> taskServerIds)
	{
		List<TaskReminderModel> reminders = new List<TaskReminderModel>();
		for (int index = 0; index < taskServerIds.Count; index += 500)
		{
			List<string> ids = taskServerIds.Skip(index).Take(500).ToList();
			List<TaskReminderModel> list = reminders;
			list.AddRange(await (from model in App.Connection.Table<TaskReminderModel>()
				where ids.Contains(model.taskserverid)
				select model).ToListAsync());
		}
		return (from reminder in reminders
			group reminder by reminder.taskserverid).SelectMany((IGrouping<string, TaskReminderModel> group) => group.DistinctEx((TaskReminderModel reminder) => reminder.trigger)).ToList();
	}

	public static async Task<int> SaveReminders(TaskReminderModel taskReminderModel)
	{
		return await Task.Run(async delegate
		{
			_ = 2;
			try
			{
				List<TaskReminderModel> list = await (from v in App.Connection.Table<TaskReminderModel>()
					where v.id.Equals(taskReminderModel.id)
					select v).ToListAsync();
				if (list.Count != 0)
				{
					taskReminderModel._Id = list[0]._Id;
					await App.Connection.UpdateAsync(taskReminderModel);
					return 0;
				}
				await App.Connection.InsertAsync(taskReminderModel);
				return taskReminderModel._Id;
			}
			catch (Exception)
			{
				return -1;
			}
		});
	}

	public static async Task InsertTaskReminder(TaskReminderModel taskReminderModel)
	{
		await App.Connection.InsertAsync(taskReminderModel);
	}

	public static async Task InsertTaskReminders(List<TaskReminderModel> taskReminderModels)
	{
		await App.Connection.InsertAllAsync(taskReminderModels);
	}

	public static async Task<bool> DeleteRemindersByTaskId(string taskId)
	{
		return await Task.Run(async delegate
		{
			List<TaskReminderModel> list = await (from v in App.Connection.Table<TaskReminderModel>()
				where v.taskserverid == taskId
				select v).ToListAsync();
			if (list.Count != 0)
			{
				foreach (TaskReminderModel item in list)
				{
					await App.Connection.DeleteAsync(item);
				}
				return true;
			}
			return false;
		});
	}

	public static async Task<List<ReminderModel>> LoadNoneFireBindEvents()
	{
		List<ReminderDelayModel> source = (await ReminderDelayDao.GetDelayModelByType("calendar")).Where((ReminderDelayModel reminderDelayModel) => reminderDelayModel.SyncStatus != -1).ToList();
		Dictionary<string, ReminderDelayModel> delayDict = source.ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
		string text = Utils.GetCurrentUserIdInt().ToString();
		List<ReminderModel> validReminders = new List<ReminderModel>();
		List<CalendarEventModel> events = await App.Connection.QueryAsync<CalendarEventModel>("select * from CalendarEventModel where (CalendarId in (select Id as CalendarId from BindCalendarModel where Show = 'show') or  CalendarId in (select Id as CalendarId from CalendarSubscribeProfileModel where Show = 'show')) and UserId = '" + text + "' and Deleted = '0'", Array.Empty<object>());
		List<CalendarEventModel> skipEvents = await CalendarEventDao.GetSkipEvents();
		List<string> hiddenKeys = await ArchivedDao.GetArchivedKeys();
		List<string> connectedCalendars = await CalendarConnectService.GetConnectedCalendarIds();
		List<string> list = (from c in CacheManager.GetBindCalendars()?.Where((BindCalendarModel c) => connectedCalendars.Contains(c.OutId))
			select c.Id)?.ToList();
		if (events != null && events.Any())
		{
			foreach (CalendarEventModel eve in events)
			{
				if ((!string.IsNullOrEmpty(eve.TickTaskId) && TaskCache.ExistTask(eve.TickTaskId)) || (list != null && list.Contains(eve.CalendarId)) || (CacheManager.GetBindCalendarById(eve.CalendarId) == null && CacheManager.GetSubscribeCalById(eve.CalendarId) == null) || !eve.DueStart.HasValue)
				{
					continue;
				}
				ReminderDelayModel delay = null;
				if (!string.IsNullOrEmpty(eve.EventId) && delayDict.ContainsKey(eve.EventId))
				{
					delay = delayDict[eve.EventId];
				}
				string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(eve));
				if (!hiddenKeys.Contains(item))
				{
					AddReminder(eve, eve.DueStart.Value, delay);
				}
				if (string.IsNullOrEmpty(eve.RepeatFlag))
				{
					continue;
				}
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
				RepeatDao.GetEventRepeatDates(eve.RepeatFlag, calStart, DateTime.Today.AddDays(-1.0), DateTime.Today.AddDays(4.0), eve.TimeZone, eve.IsAllDay).ForEach(delegate(DateTime date)
				{
					CalendarEventModel calendarEventModel = CalendarEventModel.Copy(eve);
					calendarEventModel.DueStart = date;
					if (eve.DueEnd.HasValue)
					{
						double totalMinutes = (eve.DueEnd.Value - eve.DueStart.Value).TotalMinutes;
						calendarEventModel.DueEnd = date.AddMinutes(totalMinutes);
					}
					DateTime dateTime = (calendarEventModel.IsAllDay ? date : TimeZoneUtils.LocalToTargetTzTime(date, calendarEventModel.TimeZone));
					bool flag = false;
					bool flag2 = false;
					if (exDateList != null && exDateList.Any() && exDateList.Contains(dateTime.Date))
					{
						flag = true;
					}
					if (skipEvents.Any())
					{
						flag2 = calendarEventModel.IsSkipped(skipEvents, date);
					}
					if (!flag && !flag2)
					{
						string item2 = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calendarEventModel));
						if (!hiddenKeys.Contains(item2))
						{
							AddReminder(calendarEventModel, date, delay);
						}
					}
				});
			}
		}
		return validReminders;
		void AddReminder(CalendarEventModel calendarEventModel, DateTime date, ReminderDelayModel reminderDelayModel)
		{
			ReminderModel reminderModel = new ReminderModel
			{
				EventId = (string.IsNullOrEmpty(calendarEventModel.EventId) ? calendarEventModel.Id : calendarEventModel.EventId),
				Type = ReminderModel.RemindType.Calendar,
				IsAllDay = calendarEventModel.IsAllDay,
				StartDate = date,
				ReminderTime = date,
				Title = calendarEventModel.Title,
				Content = calendarEventModel.Content,
				ProjectId = calendarEventModel.CalendarId,
				RepeatFlag = calendarEventModel.RepeatFlag
			};
			if (reminderDelayModel != null)
			{
				DateTime? remindTime = reminderDelayModel.RemindTime;
				DateTime dateTime = date;
				if (remindTime.HasValue && remindTime.GetValueOrDefault() == dateTime && reminderDelayModel.NextTime.HasValue)
				{
					reminderModel.ReminderTime = reminderDelayModel.NextTime.Value;
				}
			}
			if (CheckReminderValid(reminderModel.ReminderTime))
			{
				validReminders.Add(reminderModel);
			}
			if (!string.IsNullOrEmpty(calendarEventModel.Reminders))
			{
				List<int> list2 = JsonConvert.DeserializeObject<List<int>>(calendarEventModel.Reminders);
				if (list2 != null && list2.Any())
				{
					foreach (int item3 in list2)
					{
						if (item3 != 0)
						{
							ReminderModel reminderModel2 = new ReminderModel
							{
								EventId = (string.IsNullOrEmpty(calendarEventModel.EventId) ? calendarEventModel.Id : calendarEventModel.EventId),
								Type = ReminderModel.RemindType.Calendar,
								IsAllDay = calendarEventModel.IsAllDay,
								StartDate = date,
								Title = calendarEventModel.Title,
								Content = calendarEventModel.Content,
								ProjectId = calendarEventModel.CalendarId,
								RepeatFlag = calendarEventModel.RepeatFlag,
								ReminderTime = date.AddMinutes(item3 * -1)
							};
							if (CheckReminderValid(reminderModel2.ReminderTime))
							{
								validReminders.Add(reminderModel2);
							}
						}
					}
				}
			}
		}
	}

	public static async Task<List<ReminderModel>> LoadNonFireHabits()
	{
		List<ReminderModel> validReminders = new List<ReminderModel>();
		List<HabitModel> habits = await HabitDao.GetNeedCheckHabits();
		List<HabitCheckInModel> monthCheckIns = await HabitCheckInDao.GetCheckInsInSpan(DateTime.Today.AddDays(-30.0), DateTime.Today.AddDays(1.0));
		if (habits != null && habits.Any())
		{
			List<ReminderDelayModel> source = (await ReminderDelayDao.GetDelayModelByType("habit")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList();
			Dictionary<string, ReminderDelayModel> delayDict = source.ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
			foreach (HabitModel habit in habits)
			{
				if (string.IsNullOrEmpty(habit.Reminder))
				{
					continue;
				}
				List<HabitCheckInModel> list = monthCheckIns.Where((HabitCheckInModel c) => c.HabitId == habit.Id).ToList();
				HabitCheckInModel habitCheckInModel = list.FirstOrDefault((HabitCheckInModel v) => v.HabitId == habit.Id && v.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
				if ((habitCheckInModel != null && (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1)) || !(await HabitUtils.IsHabitValidInToday(habit, list)))
				{
					continue;
				}
				string[] array = habit.Reminder.Split(',');
				if (!array.Any())
				{
					continue;
				}
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!text.Contains(":"))
					{
						continue;
					}
					string[] array3 = text.Split(':');
					if (array3.Length != 2 || !int.TryParse(array3[0], out var result) || !int.TryParse(array3[1], out var result2))
					{
						continue;
					}
					DateTime dateTime = DateTime.Today.AddHours(result).AddMinutes(result2);
					if (!string.IsNullOrEmpty(habit.Id) && delayDict.ContainsKey(habit.Id))
					{
						ReminderDelayModel reminderDelayModel = delayDict[habit.Id];
						if (reminderDelayModel.RemindTime == dateTime && reminderDelayModel.NextTime.HasValue)
						{
							dateTime = reminderDelayModel.NextTime.Value;
						}
					}
					if (dateTime > DateTime.Now)
					{
						ReminderModel item = new ReminderModel
						{
							IsAllDay = false,
							HabitId = habit.Id,
							Type = ReminderModel.RemindType.Habit,
							StartDate = dateTime,
							ReminderTime = dateTime
						};
						validReminders.Add(item);
					}
				}
			}
		}
		return validReminders;
	}

	public static async Task<List<ReminderModel>> LoadNonFireReminders()
	{
		List<ReminderModel> validReminders = new List<ReminderModel>();
		List<ReminderModel> list = await App.Connection.QueryAsync<ReminderModel>("SELECT 0           AS Type,        b.id        AS TaskId,        ''          AS CheckItemId,        b.startdate AS StartDate,        b.isallday  AS IsAllDay,        b.creator   AS Creator,        a.TRIGGER   AS Trigger,        NULL        AS ReminderTime,        b.projectId AS ProjectId,        b.assignee AS Assignee,        b.repeatFlag          AS RepeatFlag FROM   taskremindermodel a,        taskmodel b WHERE  a.taskserverid = b.id        AND b.status = 0        AND b.deleted = 0 AND b.userId = '**' UNION SELECT 0              AS Type,        id             AS TaskId,        ''             AS CheckItemId,        startdate      AS StartDate,        isallday       AS IsAllDay,        creator   AS Creator,        'TRIGGER:PT0S' AS Trigger,        remindtime     AS ReminderTime,        projectId      AS ProjectId,        assignee AS Assignee,        ''          AS RepeatFlag FROM   taskmodel WHERE  remindertime IS NOT NULL AND deleted = 0 AND status = 0 AND userId = '**' UNION SELECT 1                    AS Type,        a.taskserverid       AS TaskId,        a.id                 AS CheckItemId,        a.startdate          AS StartDate,        a.isallday           AS IsAllDay,        b.creator   AS Creator,        'TRIGGER:PT0S'       AS Trigger,        a.snoozeremindertime AS ReminderTime,        b.projectId AS ProjectId,        b.assignee AS Assignee,        ''          AS RepeatFlag FROM   taskdetailitemmodel a, taskmodel b WHERE  a.taskserverid = b.id         AND a.status = 0         AND b.status = 0         AND b.userId = '**'         AND a.startdate is not null         AND b.deleted = 0 ".Replace("**", LocalSettings.Settings.LoginUserId), Array.Empty<object>());
		List<string> list2 = (from p in CacheManager.GetProjects()
			where ((p.closed.HasValue && !p.closed.Value) || !p.closed.HasValue) && !TeamDao.IsTeamExpired(p.teamId)
			select p.id).ToList();
		Dictionary<string, List<DateTime>> taskRepeats;
		if (list != null && list.Count > 0)
		{
			taskRepeats = new Dictionary<string, List<DateTime>>();
			foreach (ReminderModel item in list)
			{
				ReminderModel reminder = item;
				if (!list2.Contains(reminder.ProjectId) || ReminderBase.IsTaskProjectMuted(reminder))
				{
					continue;
				}
				TaskBaseViewModel task;
				TimeSpan reminderSpan;
				if (!reminder.ReminderTime.HasValue)
				{
					if (!reminder.StartDate.HasValue || Utils.IsEmptyDate(reminder.StartDate))
					{
						continue;
					}
					task = TaskCache.GetTaskById(reminder.TaskId);
					if (task == null)
					{
						continue;
					}
					DateTime? time = reminder.StartDate;
					reminderSpan = TriggerUtils.ParseTrigger(reminder.Trigger);
					List<DateTime> list3 = GetRepeats(task, time.Value);
					if (time.Value.Date < DateTime.Today.Date.AddDays(2.0) && !string.IsNullOrEmpty(reminder.RepeatFlag) && list3.Any())
					{
						foreach (DateTime item2 in list3)
						{
							time = DateUtils.SetDateOnly(time.Value, item2.Date);
							(bool, DateTime?) tuple = IsReminderTimeValid(time, null);
							if (tuple.Item1)
							{
								ReminderModel reminderModel = reminder.Copy();
								reminderModel.ReminderTime = tuple.Item2;
								validReminders.Add(reminderModel);
							}
						}
					}
					(bool, DateTime?) tuple2 = IsReminderTimeValid(reminder.StartDate, reminder.ReminderTime);
					if (tuple2.Item1)
					{
						reminder.ReminderTime = tuple2.Item2;
						validReminders.Add(reminder);
					}
				}
				else if (CheckReminderValid(reminder.ReminderTime))
				{
					validReminders.Add(reminder);
				}
				(bool, DateTime?) IsReminderTimeValid(DateTime? dateTime, DateTime? reminderTime)
				{
					if (!string.IsNullOrEmpty(reminder.Trigger) && !Utils.IsEmptyDate(dateTime))
					{
						reminderTime = dateTime - reminderSpan;
						if (task.RemindTime.HasValue && task.RemindTime > reminderTime)
						{
							return (false, reminderTime);
						}
					}
					return (CheckReminderValid(reminderTime), reminderTime);
				}
			}
		}
		return validReminders;
		List<DateTime> GetRepeats(TaskBaseViewModel taskBaseViewModel, DateTime startDate)
		{
			if (taskRepeats.ContainsKey(taskBaseViewModel.Id))
			{
				return taskRepeats[taskBaseViewModel.Id];
			}
			List<string> exDate = null;
			if (!string.IsNullOrEmpty(taskBaseViewModel.ExDates))
			{
				exDate = ExDateSerilizer.ToArray(taskBaseViewModel.ExDates).ToList();
			}
			string text = (((!taskBaseViewModel.IsAllDay.HasValue || !taskBaseViewModel.IsAllDay.Value) && !taskBaseViewModel.IsFloating && taskBaseViewModel.TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? taskBaseViewModel.TimeZoneName : TimeZoneData.LocalTimeZoneModel?.TimeZoneName);
			DateTime startDate2 = TimeZoneUtils.LocalToTargetTzTime(startDate, text);
			List<DateTime> validRepeatDates = RepeatUtils.GetValidRepeatDates(taskBaseViewModel.RepeatFlag, "2", startDate2, DateTime.Today.AddDays(-1.0), DateTime.Today.AddDays(4.0), exDate, text);
			taskRepeats[taskBaseViewModel.Id] = validRepeatDates;
			return validRepeatDates;
		}
	}

	private static bool CheckReminderValid(DateTime? reminderTime)
	{
		if (reminderTime.HasValue && reminderTime > DateTime.Now)
		{
			TimeSpan timeSpan = reminderTime.Value - DateTime.Now;
			if (timeSpan.TotalMinutes >= 0.0 && timeSpan.TotalMinutes < 30.0)
			{
				return true;
			}
		}
		return false;
	}

	public static async Task BatchAddTaskReminders(IEnumerable<TaskReminderModel> reminders)
	{
		await App.Connection.InsertAllAsync(reminders);
	}

	public static bool IsEquals(List<TaskReminderModel> left, List<TaskReminderModel> right)
	{
		if (left == null && right == null)
		{
			return true;
		}
		if (left != null && right != null)
		{
			if (left.Count != right.Count)
			{
				return false;
			}
			if (left.All((TaskReminderModel lr) => right.Any((TaskReminderModel rr) => rr.trigger == lr.trigger || (OnTimeList.Contains(rr.trigger) && OnTimeList.Contains(lr.trigger)))))
			{
				return true;
			}
		}
		return false;
	}
}
