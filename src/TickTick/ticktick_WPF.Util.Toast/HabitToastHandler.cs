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
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.Util.Toast;

public class HabitToastHandler : IToastHandler
{
	private void AddClickEvent(string label)
	{
		UserActCollectUtils.AddClickEvent("reminder", "habit_reminder_system", label);
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

	public async Task Exec(ToastOptionModel optModel, List<KeyValuePair<string, object>> kvs)
	{
		if (optModel.CurrentOptionName == "Delay")
		{
			AddClickEvent("snooze");
			await Delay(optModel, kvs);
			return;
		}
		switch (optModel.ToastType)
		{
		case ToastType.BoolHabitCompleted:
			if (optModel.CurrentOptionName == "Complete")
			{
				if (await CompleteHabitFromToast(optModel.TargetId))
				{
					AddClickEvent("done");
				}
			}
			else if (!(optModel.CurrentOptionName == "Dismiss"))
			{
			}
			break;
		case ToastType.MultiHabitCompleted:
			if (optModel.CurrentOptionName == "AddStep")
			{
				if (await AddStepFromToast(optModel.TargetId))
				{
					AddClickEvent("auto_record");
				}
			}
			else if (!(optModel.CurrentOptionName == "Dismiss") && optModel.CurrentOptionName == "Manual")
			{
				AddClickEvent("manual_record");
				App.NavigateHabit(optModel.TargetId, showRecord: true);
			}
			break;
		}
	}

	public async Task Delay(ToastOptionModel optionModel, List<KeyValuePair<string, object>> kvs)
	{
		if (kvs != null && kvs.Count > 0)
		{
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
			default:
				reminder.ReminderTime = null;
				break;
			}
			if (!string.IsNullOrEmpty(text))
			{
				UserActCollectUtils.AddClickEvent("reminder_data", "snooze", text);
			}
			await ReminderDelayDao.AddModelAsync(new ReminderDelayModel
			{
				UserId = LocalSettings.Settings.LoginUserId,
				ObjId = optionModel.TargetId,
				Type = "habit",
				RemindTime = optionModel.StartTime,
				NextTime = reminder.ReminderTime,
				SyncStatus = 0
			});
			await HabitReminderCalculator.RecalHabitReminder(optionModel.TargetId);
			if (reminder.ReminderTime.HasValue)
			{
				App.AddLegacyReminder(reminder);
			}
			SyncManager.TryDelaySync();
		}
	}

	private async Task<bool> CompleteHabitFromToast(string habitId)
	{
		bool completed = false;
		await RunOnDispatcherAsync(async delegate
		{
			HabitModel habitModel = await HabitDao.GetHabitById(habitId);
			if (habitModel != null)
			{
				HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
				if (habitCheckInType != HabitCheckInType.BoolCompleted && habitCheckInType != HabitCheckInType.BoolUnCompleted && habitCheckInType != HabitCheckInType.CompletedAllCompleted && habitCheckInType != HabitCheckInType.CompletedAllUnCompleted)
				{
					App.NavigateHabit(habitModel.Id, showRecord: true);
				}
				else
				{
					if (habitModel.IsBoolHabit())
					{
						completed = await HabitService.CheckInHabit(habitModel.Id, DateTime.Today, -1.0, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn) != null;
					}
					else
					{
						completed = await HabitService.CheckInHabit(habitModel.Id, DateTime.Today, habitModel.Step, increment: true, checkShowRecord: true, 0, null, null, HabitAction.CheckIn) != null;
					}
					if (completed)
					{
						Utils.PlayCompletionSound();
					}
				}
			}
		});
		return completed;
	}

	private async Task<bool> AddStepFromToast(string habitId)
	{
		bool completed = false;
		await RunOnDispatcherAsync(async delegate
		{
			HabitModel habitModel = await HabitDao.GetHabitById(habitId);
			if (habitModel != null)
			{
				HabitCheckInType habitCheckInType = HabitUtils.GetHabitCheckInType(0, habitModel.Type, habitModel.Step, habitModel.Goal);
				if (habitCheckInType != HabitCheckInType.AutoCompleted && habitCheckInType != HabitCheckInType.AutoUncompleted)
				{
					App.NavigateHabit(habitModel.Id, showRecord: true);
				}
				else
				{
					completed = await HabitService.CheckInHabit(habitModel.Id, DateTime.Today, habitModel.Step, increment: true, checkShowRecord: true, 0, null, null, HabitAction.AutoRecord) != null;
					if (completed)
					{
						Utils.PlayCompletionSound();
					}
				}
			}
		});
		return completed;
	}
}
