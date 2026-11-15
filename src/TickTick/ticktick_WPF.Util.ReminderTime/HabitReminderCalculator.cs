using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Util.ReminderTime;

public class HabitReminderCalculator
{
	public static async Task InitHabitReminders()
	{
		await ReminderTimeDao.DeleteReminderTimeByType(4);
		if (!LocalSettings.Settings.ShowHabit)
		{
			return;
		}
		List<ReminderTimeModel> validReminders = new List<ReminderTimeModel>();
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
						ReminderTimeModel item = new ReminderTimeModel
						{
							EntityId = habit.Id,
							Type = 4,
							ReminderTime = dateTime.Ticks,
							Time = dateTime
						};
						validReminders.Add(item);
					}
				}
			}
		}
		await ReminderTimeDao.AddReminderTimes(validReminders);
	}

	public static async Task RecalHabitReminder(string habitId)
	{
		if (!ABTestManager.IsNewRemindCalculate())
		{
			return;
		}
		HabitModel habit = await HabitDao.GetHabitById(habitId);
		if (habit == null)
		{
			return;
		}
		await ReminderTimeDao.DeleteReminderTimeByIdAndType(habit.Id, 4);
		List<ReminderTimeModel> validReminders = new List<ReminderTimeModel>();
		string[] reminders = habit.Reminder?.Split(',');
		if (reminders == null || reminders.Length == 0 || habit.Status == 1 || habit.IsSkipToday())
		{
			return;
		}
		List<HabitCheckInModel> habitCheckIns = await HabitCheckInDao.GetHabitCheckInsByHabitIdInSpan(habitId, DateTime.Today.AddDays(-30.0), DateTime.Today.AddDays(1.0));
		List<ReminderDelayModel> source = (await ReminderDelayDao.GetDelayModelByType("habit")).Where((ReminderDelayModel delay) => delay.SyncStatus != -1).ToList();
		Dictionary<string, ReminderDelayModel> delayDict = source.ToDictionaryEx((ReminderDelayModel d) => d.ObjId, (ReminderDelayModel d) => d);
		HabitCheckInModel habitCheckInModel = habitCheckIns.FirstOrDefault((HabitCheckInModel v) => v.HabitId == habit.Id && v.CheckinStamp == DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
		if ((habitCheckInModel != null && (habitCheckInModel.Value >= habitCheckInModel.Goal || habitCheckInModel.CheckStatus == 1)) || !(await HabitUtils.IsHabitValidInToday(habit, habitCheckIns)))
		{
			return;
		}
		string[] array = reminders;
		for (int num = 0; num < array.Length; num++)
		{
			string[] array2 = array[num].Split(':');
			if (array2.Length != 2 || !int.TryParse(array2[0], out var result) || !int.TryParse(array2[1], out var result2))
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
				ReminderTimeModel item = new ReminderTimeModel
				{
					EntityId = habit.Id,
					Type = 4,
					ReminderTime = dateTime.Ticks,
					Time = dateTime
				};
				validReminders.Add(item);
			}
		}
		await ReminderTimeDao.AddReminderTimes(validReminders);
	}
}
