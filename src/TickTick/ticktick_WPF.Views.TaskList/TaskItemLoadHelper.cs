using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public static class TaskItemLoadHelper
{
	public static async Task LoadIndicator(List<DisplayItemModel> models, bool loadPomo = true)
	{
		if (models == null || models.Count <= 0 || !loadPomo)
		{
			return;
		}
		List<PomodoroSummaryModel> obj = await PomoSummaryDao.GetPomosByTaskIds((from d in models
			where d.IsTask
			select d.TaskId).ToList());
		Dictionary<string, PomodoroSummaryModel> dictionary = new Dictionary<string, PomodoroSummaryModel>();
		foreach (PomodoroSummaryModel item in obj)
		{
			if (!string.IsNullOrEmpty(item.taskId) && !dictionary.ContainsKey(item.taskId))
			{
				dictionary[item.taskId] = item;
			}
		}
		foreach (DisplayItemModel model in models)
		{
			if (model.IsTask)
			{
				if (dictionary.TryGetValue(model.Id, out var value))
				{
					model.PomoSummary = value;
					model.ShowPomo = !model.PomoSummary.IsEmpty();
				}
				else
				{
					model.PomoSummary = null;
					model.ShowPomo = false;
				}
			}
		}
	}

	public static async void LoadAssigneeName(DisplayItemModel model)
	{
		AssigneeSection assigneeSection = (AssigneeSection)model.Section;
		if (string.IsNullOrEmpty(assigneeSection.Assingee) || !(assigneeSection.Assingee != "-1"))
		{
			return;
		}
		if (assigneeSection.Assingee == LocalSettings.Settings.LoginUserId)
		{
			model.SourceViewModel.Title = Utils.GetString("Me");
			return;
		}
		string text = await AvatarHelper.GetUserName(assigneeSection.Assingee, assigneeSection.ProjectId);
		if (!string.IsNullOrEmpty(text))
		{
			model.SourceViewModel.Title = text;
		}
	}

	private static async Task LoadAvatar(DisplayItemModel model)
	{
		if ((model.IsItem || model.IsTaskOrNote) && !string.IsNullOrEmpty(model.Assignee) && model.Assignee != "-1")
		{
			model.AvatarUrl = await AvatarHelper.GetAvatarUrl(model.Assignee, model.ProjectId);
		}
	}

	public static async Task LoadShowReminders(List<DisplayItemModel> models)
	{
		List<DisplayItemModel> pendingModels = models.Where((DisplayItemModel model) => !model.ShowReminder.HasValue && (model.IsTaskOrNote || model.IsItem || model.IsEvent || model.IsCourse)).ToList();
		List<string> list = (from model in pendingModels
			where model.IsTaskOrNote && model.Status == 0 && model.StartDate.HasValue && model.Deleted == 0
			select model.Id).Distinct().ToList();
		HashSet<string> queriedTaskIds = new HashSet<string>(list);
		ILookup<string, TaskReminderModel> remindersByTaskId = (await TaskReminderDao.GetRemindersByTaskIds(list)).ToLookup((TaskReminderModel reminder) => reminder.taskserverid);
		foreach (DisplayItemModel item in pendingModels)
		{
			await LoadShowReminder(item, queriedTaskIds.Contains(item.Id) ? remindersByTaskId : null);
		}
	}

	public static Task LoadShowReminder(DisplayItemModel model)
	{
		return LoadShowReminder(model, null);
	}

	private static async Task LoadShowReminder(DisplayItemModel model, ILookup<string, TaskReminderModel> remindersByTaskId)
	{
		if (model == null)
		{
			return;
		}
		if (model.IsItem)
		{
			if (model.StartDate.HasValue && model.StartDate > DateTime.Now && model.IsAllDay.HasValue && !model.IsAllDay.Value)
			{
				model.SetShowReminder(val: true);
			}
		}
		else if (model.IsEvent || model.IsCourse)
		{
			if (model.StartDate >= DateTime.Now)
			{
				model.SetShowReminder(!string.IsNullOrEmpty(model.ReminderString));
			}
		}
		else
		{
			if (!model.IsTaskOrNote)
			{
				return;
			}
			DateTime? date = model.StartDate;
			if (model.Status != 0 || !date.HasValue || model.Deleted != 0)
			{
				model.SetShowReminder(val: false);
				return;
			}
			if (model.RemindTime.HasValue && model.RemindTime > DateTime.Now)
			{
				model.SetShowReminder(val: true);
				return;
			}
			List<TaskReminderModel> list = ((remindersByTaskId != null) ? remindersByTaskId[model.Id].ToList() : (await TaskReminderDao.GetRemindersByTaskId(model.Id)));
			List<TaskReminderModel> list2 = list;
			if (list2 != null && list2.Count > 0)
			{
				bool flag = false;
				foreach (TaskReminderModel item in list2)
				{
					if (item.IsEnd())
					{
						date = model.DueDate;
					}
					if (date.HasValue)
					{
						flag = DateTime.Now <= date.Value - TriggerUtils.ParseTrigger(item.trigger);
					}
					if (flag)
					{
						break;
					}
				}
				model.SetShowReminder(flag);
			}
			else
			{
				model.SetShowReminder(val: false);
			}
		}
	}

	private static async void LoadCourseReminder(DisplayItemModel model)
	{
		_ = model;
	}
}
