using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.ReminderTime;

public class TaskReminderCalculator
{
	public static async Task OnTasksChanged(TasksChangeEventArgs args)
	{
		BlockingSet<string> blockingSet = new BlockingSet<string>();
		blockingSet.AddRange(args.AddIds);
		blockingSet.AddRange(args.StatusChangedIds);
		blockingSet.AddRange(args.DeletedChangedIds);
		blockingSet.AddRange(args.UndoDeletedIds);
		blockingSet.AddRange(args.KindChangedIds);
		blockingSet.AddRange(args.ProjectChangedIds);
		blockingSet.AddRange(args.DateChangedIds);
		blockingSet.AddRange(args.AssignChangedIds);
		blockingSet.AddRange(args.BatchChangedIds);
		foreach (string item in blockingSet.ToList())
		{
			TaskBaseViewModel task = TaskCache.GetTaskById(item);
			if (task != null)
			{
				List<TaskReminderModel> reminderModels = await TaskReminderDao.GetRemindersByTaskId(task.Id);
				List<ReminderTimeModel> reminderTimeModels = ((!string.IsNullOrEmpty(task.RepeatFlag)) ? GetRepeatTaskReminders(task, reminderModels) : GetTaskReminders(task, reminderModels));
				await ReminderTimeDao.DeleteReminderTimeByIdAndType(task.Id, 0);
				await ReminderTimeDao.AddReminderTimes(reminderTimeModels);
			}
		}
	}

	public static List<ReminderTimeModel> GetTaskReminders(TaskModel task, List<TaskReminderModel> reminderModels)
	{
		TaskBaseViewModel taskBaseViewModel = TaskCache.GetTaskById(task.id);
		if (taskBaseViewModel == null)
		{
			taskBaseViewModel = new TaskBaseViewModel(task);
		}
		return GetTaskReminders(taskBaseViewModel, reminderModels);
	}

	public static List<ReminderTimeModel> GetTaskReminders(TaskBaseViewModel task, List<TaskReminderModel> reminderModels)
	{
		if (task == null || task.Deleted != 0 || task.Status != 0)
		{
			return null;
		}
		List<ReminderTimeModel> list = new List<ReminderTimeModel>();
		if (task.StartDate.HasValue && !Utils.IsEmptyDate(task.StartDate))
		{
			DateTime value = task.StartDate.Value;
			value = value.AddSeconds(value.Second * -1);
			foreach (TaskReminderModel item in reminderModels ?? new List<TaskReminderModel>())
			{
				if (!string.IsNullOrEmpty(item.trigger))
				{
					TimeSpan timeSpan = TriggerUtils.ParseTrigger(item.trigger);
					DateTime dateTime = value - timeSpan;
					bool isEnd = false;
					if (item.trigger.Contains("END") && task.DueDate.HasValue)
					{
						bool flag = task.IsAllDay ?? true;
						dateTime = task.DueDate.Value.AddDays(flag ? (-1) : 0) - timeSpan;
						isEnd = true;
					}
					if ((!task.RemindTime.HasValue || task.RemindTime.Value < dateTime) && dateTime > DateTime.Now)
					{
						list.Add(new ReminderTimeModel(task.Id, 0, dateTime.Ticks)
						{
							IsEnd = isEnd
						});
					}
				}
			}
		}
		AddSnoozeReminder(list, task);
		return list;
	}

	public static List<ReminderTimeModel> GetRepeatTaskReminders(TaskModel task, List<TaskReminderModel> reminderModels)
	{
		TaskBaseViewModel taskBaseViewModel = TaskCache.GetTaskById(task.id);
		if (taskBaseViewModel == null)
		{
			taskBaseViewModel = new TaskBaseViewModel(task);
		}
		return GetRepeatTaskReminders(taskBaseViewModel, reminderModels);
	}

	public static List<ReminderTimeModel> GetRepeatTaskReminders(TaskBaseViewModel task, List<TaskReminderModel> reminderModels)
	{
		if (task == null || task.Deleted != 0 || task.Status != 0 || string.IsNullOrEmpty(task.RepeatFlag) || (reminderModels == null && !task.RemindTime.HasValue))
		{
			return null;
		}
		if (CacheManager.GetProjectById(task.ProjectId) == null)
		{
			return null;
		}
		List<ReminderTimeModel> list = new List<ReminderTimeModel>();
		if (task.StartDate.HasValue && !Utils.IsEmptyDate(task.StartDate))
		{
			DateTime value = task.StartDate.Value;
			value = value.AddSeconds(value.Second * -1);
			List<(TimeSpan, bool)> list2 = (from r in reminderModels ?? new List<TaskReminderModel>()
				where !string.IsNullOrEmpty(r.trigger)
				select (TriggerUtils.ParseTrigger(r.trigger), r.IsEnd())).ToList();
			foreach (var item in list2)
			{
				bool isEnd = false;
				DateTime dateTime;
				if (item.Item2 && task.DueDate.HasValue)
				{
					bool flag = task.IsAllDay ?? true;
					dateTime = task.DueDate.Value.AddDays(flag ? (-1) : 0) - item.Item1;
					isEnd = true;
				}
				else
				{
					dateTime = value - item.Item1;
				}
				if ((!task.RemindTime.HasValue || task.RemindTime.Value < dateTime) && dateTime > DateTime.Now)
				{
					list.Add(new ReminderTimeModel(task.Id, 0, dateTime.Ticks)
					{
						IsEnd = isEnd
					});
				}
			}
			List<DateTime> repeats = GetRepeats(task, value);
			if (repeats != null && repeats.Any())
			{
				foreach (DateTime item2 in repeats)
				{
					if (!(item2.Date != value.Date))
					{
						continue;
					}
					value = DateUtils.SetDateOnly(value, item2.Date);
					foreach (var item3 in list2)
					{
						bool isEnd2 = false;
						DateTime dateTime3;
						if (item3.Item2 && task.DueDate.HasValue)
						{
							DateTime dateTime2 = task.DueDate.Value.AddDays((item2.Date - task.StartDate.Value.Date).TotalDays);
							bool flag2 = task.IsAllDay ?? true;
							dateTime3 = dateTime2.AddDays(flag2 ? (-1) : 0) - item3.Item1;
							isEnd2 = true;
						}
						else
						{
							dateTime3 = value - item3.Item1;
						}
						if ((!task.RemindTime.HasValue || task.RemindTime.Value < dateTime3) && dateTime3 > DateTime.Now)
						{
							list.Add(new ReminderTimeModel(task.Id, 0, dateTime3.Ticks)
							{
								IsEnd = isEnd2
							});
						}
					}
				}
			}
		}
		AddSnoozeReminder(list, task);
		return list;
	}

	private static void AddSnoozeReminder(List<ReminderTimeModel> result, TaskBaseViewModel task)
	{
		if (task != null && task.RemindTime.HasValue && task.RemindTime.Value > DateTime.Now)
		{
			result.Add(new ReminderTimeModel(task.Id, 0, task.RemindTime.Value.Ticks));
		}
	}

	private static List<DateTime> GetRepeats(TaskBaseViewModel task, DateTime startDate)
	{
		if (task.RepeatFrom == "1")
		{
			return new List<DateTime>();
		}
		List<string> exDate = null;
		if (!string.IsNullOrEmpty(task.ExDates))
		{
			exDate = ExDateSerilizer.ToArray(task.ExDates).ToList();
		}
		string text = (((!task.IsAllDay.HasValue || !task.IsAllDay.Value) && task.IsFloating && task.TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? task.TimeZoneName : TimeZoneData.LocalTimeZoneModel?.TimeZoneName);
		DateTime startDate2 = TimeZoneUtils.LocalToTargetTzTime(startDate, text);
		int num = (int)((task.DueDate - task.StartDate)?.TotalDays ?? 0.0);
		return RepeatUtils.GetValidRepeatDates(task.RepeatFlag, "0", startDate2, DateTime.Today.AddDays(-1 - num), DateTime.Today.AddDays(30.0), exDate, text, toLocalTime: true, 0, preloadRange: false);
	}
}
