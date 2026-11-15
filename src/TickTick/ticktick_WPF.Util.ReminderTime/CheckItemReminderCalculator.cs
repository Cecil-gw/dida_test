using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.ReminderTime;

public class CheckItemReminderCalculator
{
	public static ReminderTimeModel GetCheckItemReminder(TaskDetailItemModel checkItem)
	{
		TaskBaseViewModel taskBaseViewModel = TaskDetailItemCache.GetCheckItemById(checkItem.id);
		if (taskBaseViewModel == null)
		{
			taskBaseViewModel = new TaskBaseViewModel(checkItem);
		}
		return GetCheckItemReminder(taskBaseViewModel);
	}

	public static ReminderTimeModel GetCheckItemReminder(TaskBaseViewModel checkItem)
	{
		if (checkItem == null || checkItem.Status != 0 || !checkItem.StartDate.HasValue || Utils.IsEmptyDate(checkItem.StartDate) || (checkItem.IsAllDay ?? true))
		{
			return null;
		}
		TaskBaseViewModel taskById = TaskCache.GetTaskById(checkItem.ParentId);
		if (taskById == null || taskById.Deleted != 0 || taskById.Status != 0)
		{
			return null;
		}
		if (checkItem.StartDate.HasValue && !Utils.IsEmptyDate(checkItem.StartDate))
		{
			DateTime value = checkItem.StartDate.Value;
			if ((!checkItem.RemindTime.HasValue || checkItem.RemindTime < value) && value > DateTime.Now)
			{
				return new ReminderTimeModel(checkItem.Id, 1, value.Ticks);
			}
		}
		if (checkItem.RemindTime.HasValue && checkItem.RemindTime.Value > DateTime.Now)
		{
			return new ReminderTimeModel(checkItem.Id, 1, checkItem.RemindTime.Value.Ticks);
		}
		return null;
	}

	public static async Task OnTasksChanged(TasksChangeEventArgs args)
	{
		foreach (string item in args.CheckItemChangedIds.ToList())
		{
			await ResetCheckItemReminder(item);
		}
		BlockingSet<string> blockingSet = new BlockingSet<string>();
		blockingSet.AddRange(args.AddIds);
		blockingSet.AddRange(args.BatchChangedIds);
		foreach (string item2 in blockingSet.ToList())
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(item2);
			if (taskById == null || taskById.Status != 0 || taskById.Deleted != 0 || !(taskById.Kind == "CHECKLIST"))
			{
				continue;
			}
			BlockingList<TaskBaseViewModel> checkItems = taskById.CheckItems;
			if (checkItems == null || checkItems.Count <= 0)
			{
				continue;
			}
			foreach (TaskBaseViewModel item3 in checkItems.ToList())
			{
				await ResetCheckItemReminder(item3?.Id);
			}
		}
	}

	public static async Task ResetCheckItemReminder(string checkItemId)
	{
		await ReminderTimeDao.DeleteReminderTimeByIdAndType(checkItemId, 1);
		TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(checkItemId);
		if (checkItemById != null)
		{
			ReminderTimeModel checkItemReminder = GetCheckItemReminder(checkItemById);
			if (checkItemReminder != null)
			{
				await ReminderTimeDao.AddReminderTimes(new List<ReminderTimeModel> { checkItemReminder });
			}
		}
	}
}
