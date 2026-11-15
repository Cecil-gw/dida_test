using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Util.Toast;

public class ReminderToastHandler : IToastHandler
{
	public async Task Exec(ToastOptionModel optionModel, List<KeyValuePair<string, object>> kvs)
	{
		if (optionModel.CurrentOptionName == "Complete")
		{
			await Complete(optionModel);
			AddClickEvent(optionModel, "done");
		}
		else if (optionModel.CurrentOptionName == "Focus")
		{
			OnFocus(optionModel);
			AddClickEvent(optionModel, "start_focus");
		}
		else if (optionModel.CurrentOptionName == "Delay")
		{
			await Delay(optionModel, kvs);
			AddClickEvent(optionModel, "snooze");
		}
		else if (optionModel.CurrentOptionName == "GotIt")
		{
			AddClickEvent(optionModel, "got_it_btn");
		}
	}

	private void AddClickEvent(ToastOptionModel optionModel, string label)
	{
		string ctype = "task_reminder_system";
		if (optionModel.ToastType == ToastType.RemindEvent)
		{
			ctype = "calendar_reminder_system";
		}
		if (optionModel.ToastType == ToastType.RemindCourse)
		{
			ctype = "timetable_reminder_system";
		}
		UserActCollectUtils.AddClickEvent("reminder", ctype, label);
	}

	private static async Task RunOnDispatcherAsync(Func<Task> action)
	{
		Dispatcher dispatcher = Application.Current?.Dispatcher;
		if (dispatcher == null || dispatcher.CheckAccess())
		{
			await action();
		}
		else
		{
			await dispatcher.InvokeAsync(action).Task.Unwrap();
		}
	}

	private void OnFocus(ToastOptionModel optionModel)
	{
	}

	public async Task Complete(ToastOptionModel optionModel)
	{
		await RunOnDispatcherAsync(async delegate
		{
			App.Instance.StopReminderTimer();
			try
			{
				switch (optionModel.ToastType)
				{
				case ToastType.RemindTask:
				{
					TaskModel taskModel = await TaskDao.GetThinTaskById(optionModel.TargetId);
					if (taskModel != null && taskModel.kind == "NOTE")
					{
						await SystemToastUtils.TryShowDetail(optionModel.TargetId, "task");
					}
					else if (taskModel != null && taskModel.status == 0)
					{
						await TaskService.SetTaskStatus(taskModel, 2);
					}
					break;
				}
				case ToastType.RemindCheckItem:
					await TaskService.ReminderCompleteCheckItem(optionModel.TargetId);
					break;
				}
			}
			finally
			{
				App.Instance.StartReminderTimer();
			}
			SyncManager.TryDelaySync();
		});
	}

	public async Task Delay(ToastOptionModel optionModel, List<KeyValuePair<string, object>> kvs)
	{
		if (kvs == null || kvs.Count <= 0)
		{
			return;
		}
		ReminderModel reminder = new ReminderModel();
		object value = kvs.FirstOrDefault().Value;
		string text = "";
		switch (value.ToString())
		{
		case "15mins":
			reminder.ReminderTime = DateTime.Now.AddMinutes(15.0);
			text = "15m";
			break;
		case "30mins":
			reminder.ReminderTime = DateTime.Now.AddMinutes(30.0);
			text = "30m";
			break;
		case "1hr":
			reminder.ReminderTime = DateTime.Now.AddHours(1.0);
			text = "1h";
			break;
		case "3hrs":
			reminder.ReminderTime = DateTime.Now.AddHours(3.0);
			text = "3h";
			break;
		case "1day":
			reminder.ReminderTime = DateTime.Now.AddDays(1.0);
			text = "tomorrow";
			break;
		case "TodayMorning":
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(9.0);
			text = "smart_time";
			break;
		case "TodayAfternoon":
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(13.0);
			text = "smart_time";
			break;
		case "TodayEvening":
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(17.0);
			text = "smart_time";
			break;
		case "TodayNight":
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddHours(20.0);
			text = "smart_time";
			break;
		case "TomorrowMorning":
			reminder.ReminderTime = (reminder.ReminderTime ?? DateTime.Now).Date.AddDays(1.0).AddHours(9.0);
			text = "smart_time";
			break;
		default:
			reminder.ReminderTime = null;
			break;
		}
		if (!string.IsNullOrEmpty(text))
		{
			UserActCollectUtils.AddClickEvent("reminder_data", "snooze", text);
		}
		switch (optionModel.ToastType)
		{
		case ToastType.RemindTask:
		{
			TaskModel taskModel2 = await TaskDao.GetThinTaskById(optionModel.TargetId);
			if (taskModel2 != null && taskModel2.status == 0 && taskModel2.deleted == 0)
			{
				reminder.TaskId = taskModel2.id;
				reminder.Type = ReminderModel.RemindType.Task;
				reminder.CheckItemId = "";
				reminder.StartDate = taskModel2.startDate;
				reminder.IsAllDay = taskModel2.isAllDay;
				reminder.Trigger = "TRIGGER:PT0S";
				reminder.Assignee = taskModel2.assignee;
				reminder.ProjectId = taskModel2.projectId;
				reminder.RepeatFlag = "";
				if (reminder.ReminderTime.HasValue)
				{
					await RunOnDispatcherAsync(async delegate
					{
						await TaskService.SetSnoozeTime(reminder.TaskId, reminder.ReminderTime.Value);
						App.AddLegacyReminder(reminder);
					});
				}
			}
			await ReminderDelayDao.AddModelAsync(new ReminderDelayModel
			{
				UserId = LocalSettings.Settings.LoginUserId,
				ObjId = optionModel.TargetId,
				Type = "task",
				RemindTime = optionModel.StartTime,
				NextTime = reminder.ReminderTime,
				SyncStatus = 0
			});
			await Task.Delay(100);
			SyncManager.TryDelaySync();
			break;
		}
		case ToastType.RemindCheckItem:
		{
			TaskDetailItemModel subtask = await TaskDetailItemDao.GetChecklistItemById(optionModel.TargetId);
			if (subtask == null || subtask.status != 0)
			{
				break;
			}
			TaskModel taskModel = await TaskDao.GetThinTaskById(subtask.TaskServerId);
			if (taskModel == null || taskModel.status != 0 || taskModel.deleted != 0)
			{
				break;
			}
			reminder.TaskId = subtask.TaskServerId;
			reminder.Type = ReminderModel.RemindType.Checklist;
			reminder.CheckItemId = subtask.id;
			reminder.StartDate = subtask.startDate;
			reminder.IsAllDay = subtask.isAllDay;
			reminder.Trigger = "TRIGGER:PT0S";
			reminder.Assignee = taskModel.assignee;
			reminder.ProjectId = taskModel.projectId;
			reminder.RepeatFlag = "";
			if (reminder.ReminderTime.HasValue)
			{
				await RunOnDispatcherAsync(async delegate
				{
					subtask.snoozeReminderTime = reminder.ReminderTime.Value;
					await TaskDetailItemDao.SaveChecklistItem(subtask);
					await CheckItemReminderCalculator.ResetCheckItemReminder(subtask.id);
					await SyncStatusDao.AddSyncStatus(reminder.TaskId, 0);
					App.AddLegacyReminder(reminder);
					await Task.Delay(100);
					SyncManager.TryDelaySync();
				});
			}
			break;
		}
		case ToastType.RemindCourse:
		case ToastType.RemindEvent:
		case ToastType.RemindHabit:
		case ToastType.RemindCountdown:
		{
			ReminderDelayModel reminderDelayModel = new ReminderDelayModel
			{
				UserId = LocalSettings.Settings.LoginUserId,
				ObjId = optionModel.TargetId,
				Type = ((optionModel.ToastType == ToastType.RemindEvent) ? "calendar" : ((optionModel.ToastType == ToastType.RemindHabit) ? "habit" : ((optionModel.ToastType == ToastType.RemindCountdown) ? "countdown" : "course"))),
				RemindTime = ((optionModel.ToastType != ToastType.RemindCountdown) ? optionModel.StartTime : ((optionModel.Parma is DateTime dateTime) ? dateTime : optionModel.StartTime)),
				NextTime = reminder.ReminderTime,
				SyncStatus = 0
			};
			if (optionModel.ToastType == ToastType.RemindEvent && !string.IsNullOrEmpty(optionModel.EventArchivedId))
			{
				reminderDelayModel.ObjId = optionModel.EventArchivedId;
			}
			await ReminderDelayDao.AddModelAsync(reminderDelayModel);
			if (optionModel.ToastType == ToastType.RemindEvent)
			{
				CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventIdOrId(optionModel.TargetId);
				if (calendarEventModel == null)
				{
					calendarEventModel = await CalendarEventDao.GetEventByStartTimeArchiveId(optionModel.StartTime, optionModel.EventArchivedId);
				}
				await EventReminderCalculator.RecalEventReminders(calendarEventModel);
			}
			if (optionModel.ToastType == ToastType.RemindHabit)
			{
				await HabitReminderCalculator.RecalHabitReminder(optionModel.TargetId);
			}
			if (optionModel.ToastType == ToastType.RemindCourse)
			{
				await CourseReminderCalculator.InitCourseReminders();
			}
			if (reminder.ReminderTime.HasValue)
			{
				App.AddLegacyReminder(reminder);
			}
			SyncManager.TryDelaySync();
			break;
		}
		}
	}
}
