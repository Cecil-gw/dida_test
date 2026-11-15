using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KotlinModels;
using Newtonsoft.Json;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.KotlinUtils;

public class ModelsDao
{
	public static DateTime TTCalendarToDateTime(TTCalendar calTime)
	{
		try
		{
			return new DateTime(1, 1, 1).AddYears(calTime.year - 1).AddMonths(calTime.month - 1).AddDays(calTime.day - 1)
				.AddHours(calTime.hour)
				.AddMinutes(calTime.minute)
				.AddSeconds(calTime.second);
		}
		catch (Exception)
		{
			return default(DateTime);
		}
	}

	public static TTCalendar NewTTCalendar(DateTime date, string zoneId, bool handleTimezone = false)
	{
		if (handleTimezone)
		{
			date = TimeZoneUtils.LocalToTargetTzTime(date, zoneId);
		}
		return new TTCalendar
		{
			year = date.Year,
			month = date.Month,
			day = date.Day,
			hour = date.Hour,
			minute = date.Minute,
			second = date.Second,
			milliSecond = date.Millisecond,
			zone = (zoneId ?? TimeZoneData.LocalTimeZoneModel.TimeZoneName)
		};
	}

	public static TTCalendar NewTTCalendarNullable(DateTime? date, string zoneId, bool handleTimezone = false)
	{
		if (date.HasValue)
		{
			return NewTTCalendar(date.Value, zoneId, handleTimezone);
		}
		return new TTCalendar();
	}

	public static DateTime? TTCalendarToLocalDateTime(TTCalendar cal)
	{
		if (cal.year <= 0)
		{
			return null;
		}
		try
		{
			DateTime time = new DateTime(1, 1, 1).AddYears(cal.year - 1).AddMonths(cal.month - 1).AddDays(cal.day - 1)
				.AddHours(cal.hour)
				.AddMinutes(cal.minute)
				.AddSeconds(cal.second);
			if (time.Year < 1970)
			{
				return null;
			}
			return TimeZoneUtils.ToLocalTime(time, cal.zone);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static CheckItem NewCheckItem(TaskDetailItemModel item, string timeZone, bool isClone = false)
	{
		return new CheckItem
		{
			uniqueId = item._Id,
			id = (isClone ? Utils.GetGuid() : item.id),
			startDate = ((!item.startDate.HasValue) ? new TTCalendar() : NewTTCalendar(item.startDate.Value, timeZone)),
			completedDate = ((!item.completedTime.HasValue) ? new TTCalendar() : NewTTCalendar(item.completedTime.Value, timeZone)),
			isChecked = item.status
		};
	}

	public static Reminder NewReminder(TaskReminderModel reminder, string userId, string taskId = null)
	{
		return new Reminder
		{
			id = reminder._Id,
			sid = reminder.id,
			userId = userId,
			taskId = 0L,
			taskSid = (taskId ?? reminder.taskserverid),
			duration = reminder.trigger
		};
	}

	public static DueDataModel NewDueDataModel(TaskModel task, List<TaskReminderModel> taskReminders, DateTime? recurringStart, DateTime? recurringEnd)
	{
		DueDataModel dueDataModel = new DueDataModel();
		dueDataModel.repeatFlag = task.repeatFlag;
		dueDataModel.repeatFrom = ((task.repeatFlag != null && task.repeatFlag.Contains("FORGETTINGCURVE")) ? "0" : task.repeatFrom);
		dueDataModel.timeZone = task.timeZone ?? TimeZoneData.LocalTimeZoneModel.TimeZoneName;
		dueDataModel.reminders = taskReminders?.Select((TaskReminderModel reminder) => NewReminder(reminder, task.userId, task.id)).ToList() ?? new List<Reminder>();
		dueDataModel.isAllDay = ((task.isAllDay ?? true) ? 1 : 0);
		dueDataModel.isFloating = ((task.isFloating == true) ? 1 : 0);
		dueDataModel.isClearDate = 0;
		DateTime? dateTime = recurringStart ?? task.startDate;
		dueDataModel.startDate = ((!dateTime.HasValue) ? new TTCalendar() : NewTTCalendar(dateTime.Value, task.timeZone, handleTimezone: true));
		DateTime? dateTime2 = recurringEnd ?? task.dueDate;
		dueDataModel.dueDate = ((!dateTime2.HasValue) ? new TTCalendar() : NewTTCalendar(dateTime2.Value, task.timeZone, handleTimezone: true));
		dueDataModel.completedDate = ((!task.completedTime.HasValue) ? new TTCalendar() : NewTTCalendar(task.completedTime.Value, task.timeZone, handleTimezone: true));
		dueDataModel.repeatOriginStartDate = ((!task.startDate.HasValue) ? new TTCalendar() : NewTTCalendar(task.startDate.Value, task.timeZone, handleTimezone: true));
		List<string> list = (string.IsNullOrEmpty(task.exDates) ? null : JsonConvert.DeserializeObject<List<string>>(task.exDates));
		List<TTCalendar> list2 = new List<TTCalendar>();
		if (list != null)
		{
			list = (from text in list
				where !string.IsNullOrEmpty(text)
				select text.Replace("\"", "")).ToList();
			foreach (string item2 in list)
			{
				if (DateTime.TryParseExact(item2, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					TTCalendar item = NewTTCalendar(result, null);
					list2.Add(item);
				}
			}
		}
		dueDataModel.exDate = list2;
		return dueDataModel;
	}

	public static DueDataModel NewDueDataModel(TaskModel task, TimeData timeData)
	{
		DueDataModel dueDataModel = new DueDataModel();
		dueDataModel.repeatFlag = timeData.RepeatFlag;
		dueDataModel.repeatFrom = timeData.RepeatFrom;
		dueDataModel.timeZone = timeData.TimeZone?.TimeZoneName ?? TimeZoneData.LocalTimeZoneModel.TimeZoneName;
		dueDataModel.reminders = timeData.Reminders?.Select((TaskReminderModel reminder) => NewReminder(reminder, task.userId, task.id)).ToList() ?? new List<Reminder>();
		dueDataModel.isAllDay = ((timeData.IsAllDay ?? true) ? 1 : 0);
		dueDataModel.isFloating = ((timeData.TimeZone?.IsFloat ?? false) ? 1 : 0);
		dueDataModel.isClearDate = 0;
		dueDataModel.startDate = ((!timeData.StartDate.HasValue) ? new TTCalendar() : NewTTCalendar(timeData.StartDate.Value, task.timeZone, handleTimezone: true));
		dueDataModel.dueDate = ((!timeData.DueDate.HasValue) ? new TTCalendar() : NewTTCalendar(timeData.DueDate.Value, task.timeZone, handleTimezone: true));
		dueDataModel.completedDate = ((!task.completedTime.HasValue) ? new TTCalendar() : NewTTCalendar(task.completedTime.Value, task.timeZone, handleTimezone: true));
		dueDataModel.repeatOriginStartDate = ((!task.startDate.HasValue) ? new TTCalendar() : NewTTCalendar(task.startDate.Value, task.timeZone, handleTimezone: true));
		List<TTCalendar> list = new List<TTCalendar>();
		if (timeData.ExDates != null)
		{
			timeData.ExDates = timeData.ExDates.Select((string text) => text.Replace("\"", "")).ToList();
			foreach (string exDate in timeData.ExDates)
			{
				if (DateTime.TryParseExact(exDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					TTCalendar item = NewTTCalendar(result, null);
					list.Add(item);
				}
			}
		}
		dueDataModel.exDate = list;
		return dueDataModel;
	}

	public static async Task SaveModels(List<CheckItem> list)
	{
		list?.ForEach(async delegate(CheckItem item)
		{
			TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetCheckItemById(item.id);
			if (taskDetailItemModel != null)
			{
				taskDetailItemModel.status = item.isChecked;
				taskDetailItemModel.startDate = TTCalendarToLocalDateTime(item.startDate);
				taskDetailItemModel.completedTime = TTCalendarToLocalDateTime(item.completedDate);
				await TaskDetailItemDao.SaveChecklistItem(taskDetailItemModel);
			}
		});
	}

	public static async Task InsertModels(List<CheckItem> list, string taskId, bool keepStatus = true)
	{
		if (list == null)
		{
			return;
		}
		foreach (CheckItem item in list)
		{
			TaskDetailItemModel taskDetailItemModel = await (from t in App.Connection.Table<TaskDetailItemModel>()
				where (long)t._Id == item.uniqueId
				select t).FirstOrDefaultAsync();
			if (taskDetailItemModel == null)
			{
				return;
			}
			await TaskDetailItemDao.SaveChecklistItem(new TaskDetailItemModel
			{
				id = item.id,
				title = taskDetailItemModel.title,
				status = (keepStatus ? item.isChecked : 0),
				isAllDay = taskDetailItemModel.isAllDay,
				startDate = TTCalendarToLocalDateTime(item.startDate),
				completedTime = TTCalendarToLocalDateTime(item.completedDate),
				TaskServerId = taskId,
				sortOrder = taskDetailItemModel.sortOrder
			});
		}
	}

	public static async Task InsertModels(List<Reminder> list, string taskId)
	{
		if (list == null)
		{
			return;
		}
		foreach (Reminder item in list)
		{
			TaskReminderModel taskReminderModel = new TaskReminderModel();
			taskReminderModel.Taskid = (int)item.taskId;
			taskReminderModel.taskserverid = taskId;
			try
			{
				taskReminderModel.id = item.sid;
				taskReminderModel.trigger = item.duration;
			}
			catch (Exception)
			{
			}
			await TaskReminderDao.InsertTaskReminder(taskReminderModel);
		}
	}
}
