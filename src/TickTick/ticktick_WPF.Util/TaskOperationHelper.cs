using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public static class TaskOperationHelper
{
	public static async Task<OperationExtra> GetTaskAccessInfo(string taskId, bool canShowAddSubtask = true)
	{
		OperationExtra extra = new OperationExtra();
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			if (!string.IsNullOrEmpty(task.tag))
			{
				task.tags = TagSerializer.ToTags(task.tag).ToArray();
			}
			if (!string.IsNullOrEmpty(task.exDates))
			{
				task.exDate = ExDateSerilizer.ToArray(task.exDates);
			}
			extra.TaskId = taskId;
			extra.Priority = task.priority;
			extra.ProjectId = task.projectId;
			extra.ColumnId = task.columnId;
			extra.Assignee = task.assignee;
			extra.TaskType = ((task.kind == "NOTE") ? TaskType.Note : TaskType.Task);
			if (extra.TaskType == TaskType.Task)
			{
				extra.IsAbandoned = task.status == -1;
			}
			List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(task.id);
			extra.TimeModel = new TimeData
			{
				StartDate = task.startDate,
				DueDate = task.dueDate,
				IsAllDay = (!task.startDate.HasValue || (task.isAllDay ?? true)),
				Reminders = list?.ToList(),
				RepeatFrom = task.repeatFrom,
				RepeatFlag = task.repeatFlag,
				AnnoyingAlert = task.annoyingAlert,
				HasTime = task.startDate.HasValue,
				ExDates = task.exDate?.ToList(),
				TimeZone = new TimeZoneViewModel(task.Floating, task.timeZone)
			};
			if (!task.startDate.HasValue)
			{
				TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
				DateTime? startDate = defaultSafely.GetDefaultDateTime();
				if (!startDate.HasValue)
				{
					startDate = DateTime.Today;
					task.isAllDay = true;
					if (defaultSafely.DateMode == 1)
					{
						task.dueDate = startDate.Value.AddMinutes(defaultSafely.Duration);
						task.isAllDay = Constants.DurationValue.IsAllDayValue(defaultSafely.Duration);
					}
					task.reminders = ((task.isAllDay.HasValue && task.isAllDay.Value) ? TimeData.GetDefaultAllDayReminders().ToArray() : TimeData.GetDefaultTimeReminders().ToArray());
				}
				task.startDate = startDate;
			}
			extra.ShowCopy = true;
			extra.ShowCopyLink = true;
			extra.ShowCreateSubTask = canShowAddSubtask && !string.IsNullOrEmpty(taskId) && TaskDao.GetTaskLevel(taskId, task.projectId) < 4;
			extra.ShowLinkParent = TaskCache.ExistSubTasks() && task.kind != "NOTE";
			extra.Tags = new TagSelectData
			{
				OmniSelectTags = (task.tags?.ToList() ?? new List<string>())
			};
			extra.ShowPomo = LocalSettings.Settings.EnableFocus && task.status == 0;
			extra.ShowSkip = !TaskService.IsNonRepeatTask(task);
			extra.ShowDate = string.IsNullOrEmpty(task.attendId) || AgendaHelper.CanAccessAgenda(task);
			bool flag = AgendaHelper.CanAccessAgenda(task);
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId);
			extra.ShowAssignTo = projectModel != null && projectModel.IsShareList() && (string.IsNullOrEmpty(task.attendId) || flag);
			extra.CanSwitch = task.kind == "NOTE" || (task.status == 0 && !TaskCache.IsParentTask(task.id) && !TaskCache.IsChildTask(task.parentId, task.projectId) && string.IsNullOrEmpty(task.attendId));
			extra.ShowSwitch = task.status == 0 && string.IsNullOrEmpty(task.attendId);
			extra.FailedSwitchTips = Utils.GetString("CannotConvertRelatedTaskToNote");
			extra.CompleteTime = ((task.status == 2) ? task.completedTime : ((DateTime?)null));
		}
		return extra;
	}

	public static bool CheckIfAgendaAllowClearDate(string attendId, DependencyObject parent)
	{
		if (!string.IsNullOrEmpty(attendId))
		{
			bool? flag = new CustomerDialog(Utils.GetString("ClearDate"), Utils.GetString("AgendaClearDateHint"), Utils.GetString("PublicClear"), Utils.GetString("Cancel"))
			{
				Owner = Window.GetWindow(parent)
			}.ShowDialog();
			if (flag.HasValue)
			{
				return flag.Value;
			}
			return false;
		}
		return true;
	}

	public static async Task<bool> CheckIfTaskAllowClearDate(string taskId, DependencyObject parent)
	{
		return CheckIfAgendaAllowClearDate((await TaskDao.GetThinTaskById(taskId))?.attendId, parent);
	}

	public static async Task<bool> CheckIfAllowDeleteTask(string taskId, DependencyObject parent)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (!string.IsNullOrEmpty(taskModel.attendId))
		{
			return await CheckIfAllowDeleteAgenda(taskModel, parent);
		}
		return true;
	}

	public static async Task<bool> CheckIfAllowDeleteAgenda(TaskModel task, DependencyObject parent)
	{
		if (task != null && !string.IsNullOrEmpty(task.attendId))
		{
			if (AgendaHelper.CanAccessAgenda(task))
			{
				bool? flag = new CustomerDialog(Utils.GetString("DeleteAgenda"), Utils.GetString("DeleteAgendaHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
				{
					Owner = ((parent == null) ? null : Window.GetWindow(parent))
				}.ShowDialog();
				if (!flag.HasValue || !flag.Value)
				{
					return false;
				}
			}
			else
			{
				bool? flag2 = new CustomerDialog(Utils.GetString("DeleteAgenda"), Utils.GetString("DeleteAttendAgendaHint"), Utils.GetString("Delete"), Utils.GetString("Cancel"))
				{
					Owner = ((parent == null) ? null : Window.GetWindow(parent))
				}.ShowDialog();
				if (!flag2.HasValue || !flag2.Value)
				{
					return false;
				}
			}
		}
		return true;
	}
}
