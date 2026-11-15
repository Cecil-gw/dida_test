using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Remind;

namespace ticktick_WPF.Util.ReminderTime;

public class ReminderBase
{
	public static TTAsyncLocker Locker = new TTAsyncLocker(1, 1);

	private static DateTime _lastReminderDate;

	public static bool? UseNewReminderCalculate { get; set; }

	public static async Task InitReminders(bool force)
	{
		bool flag = ABTestManager.IsNewRemindCalculate();
		UseNewReminderCalculate = flag;
		if (!flag || (!force && LocalSettings.Settings.ExtraSettings.ReminderCalculated))
		{
			if (flag)
			{
				await LoadAllRepeatReminders();
			}
			return;
		}
		LocalSettings.Settings.ExtraSettings.ReminderCalculated = true;
		await Locker.RunAsync(async delegate
		{
			await ReminderTimeDao.DeleteAllReminderTime();
			List<TaskBaseViewModel> tasks = TaskCache.GetAllTask();
			Dictionary<string, List<TaskReminderModel>> reminderDict = (await TaskReminderDao.GetAllReminders()).GroupEx((TaskReminderModel r) => r.taskserverid, (TaskReminderModel r) => r);
			foreach (TaskBaseViewModel item in tasks)
			{
				if (string.IsNullOrEmpty(item.RepeatFlag) && !string.IsNullOrEmpty(item.Id))
				{
					reminderDict.TryGetValue(item.Id, out var value);
					if ((value != null && value.Count != 0) || item.RemindTime.HasValue)
					{
						await ReminderTimeDao.AddReminderTimes(TaskReminderCalculator.GetTaskReminders(item, value));
					}
				}
			}
			List<TaskBaseViewModel> allCheckItems = TaskDetailItemCache.GetAllCheckItems();
			foreach (TaskBaseViewModel item2 in allCheckItems)
			{
				ReminderTimeModel checkItemReminder = CheckItemReminderCalculator.GetCheckItemReminder(item2);
				if (checkItemReminder != null)
				{
					await ReminderTimeDao.AddReminderTimes(new List<ReminderTimeModel> { checkItemReminder });
				}
			}
		});
		await LoadAllRepeatReminders();
	}

	private static async Task ReloadRepeatRemindersOnDayChanged()
	{
		if (_lastReminderDate != DateTime.Now.AddMinutes(1.5).Date)
		{
			await LoadAllRepeatReminders();
		}
	}

	public static async Task LoadAllRepeatReminders()
	{
		await Locker.RunAsync(async delegate
		{
			_lastReminderDate = DateTime.Today;
			List<TaskBaseViewModel> allTask = TaskCache.GetAllTask();
			foreach (TaskBaseViewModel task in allTask)
			{
				if (task != null && task.Deleted == 0 && task.Status == 0 && !string.IsNullOrEmpty(task.RepeatFlag))
				{
					List<ReminderTimeModel> reminderTimeModels = TaskReminderCalculator.GetRepeatTaskReminders(task, await TaskReminderDao.GetRemindersByTaskId(task.Id));
					await ReminderTimeDao.DeleteReminderTimeByIdAndType(task.Id, 0);
					await ReminderTimeDao.AddReminderTimes(reminderTimeModels);
				}
			}
			await EventReminderCalculator.InitAllEventsReminderTimes();
			await HabitReminderCalculator.InitHabitReminders();
			await CourseReminderCalculator.InitCourseReminders();
		});
	}

	public static async Task TryRemind(DateTime time)
	{
		if (!ABTestManager.IsNewRemindCalculate() || !LocalSettings.Settings.ShowReminder)
		{
			return;
		}
		List<ReminderTimeModel> list = await ReminderTimeDao.GetReminderTimeModelBetweenTime(time.AddSeconds(-30.0), time.AddSeconds(30.0));
		List<Task> showReminderTasks = new List<Task>();
		if (list.Any())
		{
			HashSet<string> reminded = new HashSet<string>();
			foreach (ReminderTimeModel r in list)
			{
				if (Math.Abs(r.ReminderTime - time.Ticks) > 290000000)
				{
					continue;
				}
				await App.Connection.DeleteAsync(r);
				if (reminded.Contains(r.EntityId))
				{
					continue;
				}
				reminded.Add(r.EntityId);
				ReminderModel reminder = null;
				ReminderModel.RemindType type = (ReminderModel.RemindType)r.Type;
				switch (type)
				{
				case ReminderModel.RemindType.Task:
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(r.EntityId);
					if (taskById == null || taskById.Status != 0 || taskById.Deleted != 0)
					{
						continue;
					}
					reminder = new ReminderModel
					{
						Type = type,
						TaskId = taskById.Id,
						StartDate = taskById.StartDate,
						ReminderTime = new DateTime(r.ReminderTime),
						ProjectId = taskById.ProjectId,
						Creator = taskById.Creator,
						IsAllDay = taskById.IsAllDay,
						Assignee = taskById.Assignee,
						IsEnd = r.IsEnd,
						DueDate = taskById.DueDate
					};
					break;
				}
				case ReminderModel.RemindType.Checklist:
				{
					TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(r.EntityId);
					if (checkItemById != null)
					{
						TaskBaseViewModel ownerTask = checkItemById.OwnerTask;
						if (ownerTask.Kind == "CHECKLIST" && ownerTask.Status == 0 && ownerTask.Deleted == 0 && checkItemById.Status == 0 && !(checkItemById.IsAllDay ?? true) && checkItemById.StartDate.HasValue)
						{
							reminder = new ReminderModel
							{
								Type = type,
								TaskId = ownerTask.Id,
								CheckItemId = checkItemById.Id,
								StartDate = checkItemById.StartDate,
								ReminderTime = new DateTime(r.ReminderTime),
								ProjectId = ownerTask.ProjectId,
								Creator = ownerTask.Creator,
								IsAllDay = false,
								Assignee = ownerTask.Assignee
							};
						}
					}
					break;
				}
				case ReminderModel.RemindType.Calendar:
				{
					if (LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar)
					{
						continue;
					}
					CalendarEventModel eve = await CalendarEventDao.GetEventByEventIdOrId(r.EntityId);
					if (eve == null)
					{
						break;
					}
					if ((!string.IsNullOrEmpty(eve.TickTaskId) && TaskCache.ExistTask(eve.TickTaskId)) || eve.ResponseStatusType == ResponseStatusType.Declined)
					{
						continue;
					}
					BindCalendarModel calendar = CacheManager.GetBindCalendarById(eve.CalendarId);
					if (calendar != null && (await CalendarConnectService.GetConnectedCalendarIds()).Contains(calendar.OutId))
					{
						continue;
					}
					if (calendar == null)
					{
						CalendarSubscribeProfileModel subscribeCalById = CacheManager.GetSubscribeCalById(eve.CalendarId);
						if (subscribeCalById == null)
						{
							continue;
						}
						UtilLog.Info("TryRemindEvent " + subscribeCalById.Name + " , " + eve.CalendarId + " ," + eve.Id + " ," + eve.EventId);
					}
					else
					{
						UtilLog.Info("TryRemindEvent " + calendar?.Name + " ," + eve.Id + " ," + eve.EventId);
					}
					reminder = new ReminderModel
					{
						Type = type,
						EventId = r.EntityId,
						StartDate = eve.DueStart,
						ReminderTime = new DateTime(r.ReminderTime),
						ProjectId = eve.CalendarId,
						DueDate = eve.DueEnd
					};
					reminder.Title = eve.Title;
					reminder.Content = eve.Content;
					reminder.EventArchivedId = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(eve));
					break;
				}
				case ReminderModel.RemindType.Habit:
					if (await HabitDao.GetHabitById(r.EntityId) != null)
					{
						DateTime value = new DateTime(r.ReminderTime);
						reminder = new ReminderModel
						{
							Type = type,
							HabitId = r.EntityId,
							StartDate = value,
							ReminderTime = value
						};
					}
					break;
				}
				if (reminder != null)
				{
					showReminderTasks.Add(ShowReminderWindow(reminder));
				}
			}
		}
		if (UserDao.IsPro() && LocalSettings.Settings.UserPreference?.TimeTable?.isEnabled == true)
		{
			List<ReminderModel> courseReminders = CourseReminderCalculator.GetCourseReminders(time.AddSeconds(-30.0), time.AddSeconds(30.0));
			if (courseReminders != null && courseReminders.Count > 0)
			{
				foreach (ReminderModel item in courseReminders)
				{
					showReminderTasks.Add(ShowReminderWindow(item));
				}
			}
		}
		if (LocalSettings.Settings.EnableCountDownDays)
		{
			List<ReminderModel> reminders = new List<ReminderModel>();
			List<CountdownModel> countdowns = await CountdownDao.GetActiveCountdowns();
			if (countdowns != null && countdowns.Count > 0)
			{
				Dictionary<string, ReminderDelayModel> dictionary = (await ReminderDelayDao.GetDelayModelByType("countdown")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList().ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
				foreach (CountdownModel item2 in countdowns)
				{
					if (item2.Reminders == null || item2.Reminders.Length == 0)
					{
						continue;
					}
					if (dictionary.TryGetValue(item2.Id, out var value2))
					{
						double totalSeconds = (DateTime.Now - value2.NextTime.Value).TotalSeconds;
						if (totalSeconds >= -30.0 && totalSeconds <= 30.0)
						{
							string text = item2.Name;
							if (item2.CheckActualCanShowAge(DisplayTimerMode.CountDown))
							{
								text += item2.GetAge(item2.GetTargetDate());
							}
							reminders.Add(new ReminderModel
							{
								Id = item2.Id,
								Type = ReminderModel.RemindType.Countdown,
								IsAllDay = true,
								StartDate = item2.GetTargetDate(),
								DueDate = null,
								ReminderTime = value2.NextTime.Value,
								Title = text,
								Content = ScheduleService.GetDaysDescription(item2.GetTargetDate(), item2.ShowCalendarType == ShowCalendarType.Lunar)
							});
						}
					}
					string[] reminders2 = item2.Reminders;
					foreach (string trigger in reminders2)
					{
						DateTime dateTime = item2.GetTargetDate() - TriggerUtils.ParseTrigger(trigger);
						double totalSeconds2 = (DateTime.Now - dateTime).TotalSeconds;
						if (totalSeconds2 >= -30.0 && totalSeconds2 <= 30.0)
						{
							string text2 = item2.Name;
							if (item2.CheckActualCanShowAge(DisplayTimerMode.CountDown))
							{
								text2 += item2.GetAge(item2.GetTargetDate());
							}
							reminders.Add(new ReminderModel
							{
								Id = item2.Id,
								Type = ReminderModel.RemindType.Countdown,
								IsAllDay = true,
								StartDate = item2.GetTargetDate(),
								DueDate = null,
								ReminderTime = dateTime,
								Title = text2,
								Content = ScheduleService.GetDaysDescription(item2.GetTargetDate(), item2.ShowCalendarType == ShowCalendarType.Lunar)
							});
						}
					}
				}
			}
			if (reminders.Count > 0)
			{
				foreach (ReminderModel item3 in reminders)
				{
					showReminderTasks.Add(ShowReminderWindow(item3));
				}
			}
		}
		if (showReminderTasks.Count > 0)
		{
			await Task.WhenAll(showReminderTasks);
		}
		await ReloadRepeatRemindersOnDayChanged();
	}

	public static async Task ShowReminderWindow(ReminderModel reminder)
	{
		if (IsTaskProjectMuted(reminder) || FullscreenReminderGuard.TryDefer(reminder))
		{
			return;
		}
		if (!reminder.HasResolvedAnnoyingAlert)
		{
			reminder.IsAnnoyingAlert = await ResolveAnnoyingAlert(reminder);
			reminder.HasResolvedAnnoyingAlert = true;
		}
		UtilLog.Info($"TimeToReminder {reminder.Type} HabitId " + reminder.HabitId + " TaskId " + reminder.TaskId + " EventId " + reminder.EventId + " ItemId " + reminder.CheckItemId + $" inClient {LocalSettings.Settings.ShowReminderInClient} showreminder" + LocalSettings.Settings.ShowReminder);
		if (!LocalSettings.Settings.ShowReminder || string.IsNullOrEmpty(LocalSettings.Settings.LoginUserId))
		{
			return;
		}
		if (LocalSettings.Settings.ShowReminderInClient || Utils.IsWindows7() || !SystemToastUtils.CheckSystemToastEnable())
		{
			await ShowReminderPopup(reminder);
			return;
		}
		string remindSound = LocalSettings.Settings.ExtraSettings.RemindSound;
		bool playSound = !reminder.IsAnnoyingAlert && !AnnoyingReminderManager.HasActiveSession() && (remindSound == "Harp" || string.IsNullOrEmpty(remindSound));
		try
		{
			UserActCollectUtils.AddClickEvent("reminder_data", "style", "remind_with_system_notification");
			switch (reminder.Type)
			{
			case ReminderModel.RemindType.Countdown:
				await SystemToastUtils.ToastCountdown(reminder, playSound);
				break;
			case ReminderModel.RemindType.Course:
				UserActCollectUtils.AddClickEvent("reminder_data", "type", "timetable_reminder");
				await SystemToastUtils.ToastCourse(reminder, playSound);
				break;
			case ReminderModel.RemindType.Habit:
				UserActCollectUtils.AddClickEvent("reminder_data", "type", "habit_reminder");
				await SystemToastUtils.ToastHabit(reminder, playSound);
				break;
			default:
				await SystemToastUtils.SystemToast(reminder, playSound);
				break;
			}
			AnnoyingReminderManager.TryStart(reminder, AnnoyingReminderChannel.SystemToast);
			if (!reminder.IsAnnoyingAlert)
			{
				PlayReminderSound();
			}
		}
		catch (Exception ex)
		{
			await ShowReminderPopup(reminder);
			UtilLog.Warn(ex.Message);
		}
	}

	public static async Task<bool> ResolveAnnoyingAlert(ReminderModel reminder)
	{
		if (!UserDao.IsPro())
		{
			return false;
		}
		int? entitySetting = null;
		DisplayModule? module = null;
		switch (reminder.Type)
		{
		case ReminderModel.RemindType.Task:
		case ReminderModel.RemindType.Checklist:
		{
			module = DisplayModule.Task;
			TaskBaseViewModel taskById = TaskCache.GetTaskById(reminder.TaskId);
			entitySetting = ((taskById == null) ? (await TaskDao.GetThinTaskById(reminder.TaskId))?.annoyingAlert : taskById.AnnoyingAlert);
			break;
		}
		case ReminderModel.RemindType.Habit:
			module = DisplayModule.Habit;
			entitySetting = (await HabitDao.GetHabitById(reminder.HabitId))?.AnnoyingAlert;
			break;
		case ReminderModel.RemindType.Countdown:
			module = DisplayModule.CountDown;
			entitySetting = (await CountdownDao.GetCountdownById(reminder.Id))?.AnnoyingAlert;
			break;
		}
		return module.HasValue && AnnoyingReminderManager.ShouldUseAnnoyingAlert(entitySetting, module.Value);
	}

	private static async Task ShowReminderPopup(ReminderModel reminder)
	{
		UserActCollectUtils.AddClickEvent("reminder_data", "style", "remind_me_on_client");
		switch (reminder.Type)
		{
		case ReminderModel.RemindType.Task:
		case ReminderModel.RemindType.Checklist:
			UserActCollectUtils.AddClickEvent("reminder_data", "type", "task_reminder");
			break;
		case ReminderModel.RemindType.Calendar:
			if (string.IsNullOrEmpty(reminder.Title))
			{
				CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(reminder.EventId);
				if (calendarEventModel == null)
				{
					calendarEventModel = await CalendarEventDao.GetEventByEventId(reminder.EventId);
				}
				CalendarEventModel calendarEventModel2 = calendarEventModel;
				if (calendarEventModel2 != null)
				{
					reminder.Title = calendarEventModel2.Title;
					reminder.Content = calendarEventModel2.Content;
					reminder.EventArchivedId = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(calendarEventModel2));
				}
			}
			UserActCollectUtils.AddClickEvent("reminder_data", "type", "calendar_reminder");
			break;
		case ReminderModel.RemindType.Habit:
			if (!LocalSettings.Settings.ShowHabit)
			{
				return;
			}
			UserActCollectUtils.AddClickEvent("reminder_data", "type", "habit_reminder");
			break;
		case ReminderModel.RemindType.Course:
			UserActCollectUtils.AddClickEvent("reminder_data", "type", "timetable_reminder");
			break;
		}
		if (!reminder.IsAnnoyingAlert)
		{
			DelayActionHandlerCenter.TryDoAction("ShowCustomRemindPopup", delegate
			{
				PlayReminderSound();
			}, 200);
		}
		await Application.Current.Dispatcher.InvokeAsync((Func<Task>)async delegate
		{
			await ReminderWindowManager.TryAddNewWindow(reminder);
		}).Task.Unwrap();
		AnnoyingReminderManager.TryStart(reminder, AnnoyingReminderChannel.ClientPopup);
		await Task.Delay(1000);
	}

	private static void PlayReminderSound()
	{
		if (LocalSettings.Settings.EnableRingtone)
		{
			RemindSoundPlayer.PlayTaskRemindSound(isTry: false);
		}
	}

	public static bool IsTaskProjectMuted(ReminderModel reminder)
	{
		if (reminder.Type == ReminderModel.RemindType.Task || reminder.Type == ReminderModel.RemindType.Checklist)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(reminder.TaskId);
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == reminder.ProjectId);
			if (projectModel == null || !projectModel.IsValid())
			{
				return true;
			}
			if (projectModel.IsShareList())
			{
				bool flag = string.IsNullOrEmpty(taskById.Assignee) || taskById.Assignee == "-1";
				switch (projectModel.ReminderTypeState)
				{
				case ShowReminderType.All:
					return false;
				case ShowReminderType.ExceptOther:
					if (flag || reminder.Assignee == Utils.GetCurrentUserIdInt().ToString())
					{
						return false;
					}
					return true;
				case ShowReminderType.OnlySelf:
					if (!flag && reminder.Assignee == Utils.GetCurrentUserIdInt().ToString())
					{
						return false;
					}
					return true;
				case ShowReminderType.Never:
					return true;
				}
			}
		}
		return false;
	}
}
