using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util;

public static class ReminderCalculator
{
	private static bool _assembling;

	private static bool _needRedo;

	public static void AssembleReminders()
	{
		if (!ABTestManager.IsNewRemindCalculate())
		{
			DelayActionHandlerCenter.TryDoAction("ReminderCalculator.AssembleReminders", delegate
			{
				ThreadUtil.DetachedRunOnUiBackThread(Assemble);
			}, 3000);
		}
	}

	public static async void Assemble()
	{
		if (_assembling)
		{
			_needRedo = true;
			return;
		}
		_needRedo = false;
		_assembling = true;
		List<ReminderModel> reminders = new List<ReminderModel>();
		if (UserDao.IsPro() && LocalSettings.Settings.UserPreference?.TimeTable?.isEnabled == true)
		{
			List<ReminderModel> list = await ScheduleService.LoadCourseReminders();
			if (list != null && list.Count > 0)
			{
				reminders.AddRange(list);
			}
		}
		if (LocalSettings.Settings.ShowHabit)
		{
			List<ReminderModel> list2 = await TaskReminderDao.LoadNonFireHabits();
			if (list2 != null && list2.Count > 0)
			{
				reminders.AddRange(list2);
			}
		}
		if (!LocalSettings.Settings.ExtraSettings.DoNotDisturbInCalendar)
		{
			List<ReminderModel> list3 = await TaskReminderDao.LoadNoneFireBindEvents();
			if (list3 != null && list3.Count > 0)
			{
				reminders.AddRange(list3);
			}
		}
		List<ReminderModel> list4 = await TaskReminderDao.LoadNonFireReminders();
		if (list4 != null && list4.Count > 0)
		{
			reminders.AddRange(list4);
		}
		App.SetLegacyReminders(reminders);
		_assembling = false;
		if (_needRedo)
		{
			AssembleReminders();
		}
	}

	public static async Task<bool> IsReminderExpired(ReminderModel reminder)
	{
		if (!string.IsNullOrEmpty(reminder.CheckItemId))
		{
			TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetChecklistItemById(reminder.CheckItemId);
			if (taskDetailItemModel == null || taskDetailItemModel.status != 0 || taskDetailItemModel.snoozeReminderTime.HasValue || taskDetailItemModel.startDate != reminder.StartDate)
			{
				return true;
			}
			if (!string.IsNullOrEmpty(reminder.TaskId))
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(reminder.TaskId);
				if (taskModel == null || taskModel.deleted != 0)
				{
					return true;
				}
			}
			return false;
		}
		if (!string.IsNullOrEmpty(reminder.TaskId))
		{
			TaskModel taskModel2 = await TaskDao.GetThinTaskById(reminder.TaskId);
			if (taskModel2 == null || taskModel2.deleted != 0 || taskModel2.status != 0 || (taskModel2.remindTime.HasValue && taskModel2.remindTime.Value > DateTime.Now) || reminder.StartDate != taskModel2.startDate)
			{
				return true;
			}
		}
		return false;
	}
}
