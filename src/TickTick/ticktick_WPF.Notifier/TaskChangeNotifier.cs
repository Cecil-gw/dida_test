using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Notifier;

public class TaskChangeNotifier
{
	private readonly TasksChangeEventArgs _tasksChangeEventArgs = new TasksChangeEventArgs();

	public static readonly TaskChangeNotifier Notifier = new TaskChangeNotifier();

	public event EventHandler<TasksChangeEventArgs> TasksChanged;

	private TaskChangeNotifier()
	{
	}

	private async Task TryNotify(int delay = 10, bool needSync = true)
	{
		DelayActionHandlerCenter.TryDoAction("TasksChangeNotify", delegate
		{
			ThreadUtil.DetachedRunOnUiThread(NotifyTasksChanged);
		}, delay);
		if (needSync)
		{
			SyncManager.TryDelaySync();
		}
	}

	private void NotifyTasksChanged()
	{
		TasksChangeEventArgs eventArgs = GetEventArgs();
		SearchHelper.ClearTaskSearchModels();
		this.TasksChanged?.Invoke(Notifier, eventArgs);
		TickFocusManager.OnTasksChanged(eventArgs);
		if (ABTestManager.IsNewRemindCalculate())
		{
			TaskReminderCalculator.OnTasksChanged(eventArgs);
			CheckItemReminderCalculator.OnTasksChanged(eventArgs);
		}
	}

	private TasksChangeEventArgs GetEventArgs()
	{
		TasksChangeEventArgs result = _tasksChangeEventArgs.Copy();
		_tasksChangeEventArgs.Clear();
		return result;
	}

	public static void NotifyTaskChanged(TasksChangeEventArgs args)
	{
		Notifier._tasksChangeEventArgs.Merge(args);
		Notifier.TryNotify();
	}

	public static void NotifyBatchDeleteUndo(List<string> taskIds)
	{
		Notifier._tasksChangeEventArgs.UndoDeletedIds.AddRange(taskIds, checkNull: true);
		Notifier.TryNotify(10, needSync: false);
	}

	public static void NotifyTaskBatchDeletedChanged(List<string> taskIds, bool needSync = true)
	{
		Notifier._tasksChangeEventArgs.DeletedChangedIds.AddRange(taskIds, checkNull: true);
		Notifier.TryNotify(10, needSync);
	}

	public static void NotifyTaskBatchAdded(List<string> taskIds)
	{
		Notifier._tasksChangeEventArgs.AddIds.AddRange(taskIds, checkNull: true);
		Notifier.TryNotify();
	}

	public static void NotifyDeleteUndo(string taskId)
	{
		Notifier._tasksChangeEventArgs.UndoDeletedIds.Add(taskId);
		Notifier.TryNotify(50, needSync: false);
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById == null || !(taskById.Kind == "CHECKLIST") || taskById.CheckItems?.Value == null)
		{
			return;
		}
		foreach (TaskBaseViewModel item in taskById.CheckItems.Value)
		{
			OnCheckItemChanged(item.Id);
		}
	}

	public static async void NotifyTaskDeleted(string taskId)
	{
		if (taskId == null)
		{
			return;
		}
		Notifier._tasksChangeEventArgs.DeletedChangedIds.Add(taskId);
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById != null && taskById.Kind == "CHECKLIST" && taskById.CheckItems?.Value != null)
		{
			foreach (TaskBaseViewModel item in taskById.CheckItems.Value)
			{
				OnCheckItemChanged(item.Id);
			}
		}
		Notifier.TryNotify(50, needSync: false);
	}

	public static async void NotifyTaskAdded(TaskModel task)
	{
		if (task != null)
		{
			Notifier._tasksChangeEventArgs.AddIds.Add(task.id);
			Notifier.TryNotify(100);
		}
	}

	public static async void NotifyTaskSortChanged(IEnumerable<string> taskIds)
	{
		Notifier._tasksChangeEventArgs.SortOrderChangedIds.AddRange(taskIds, checkNull: true);
		Notifier.TryNotify();
	}

	public static async void NotifyTaskStatusChanged(TaskCloseExtra extra)
	{
		TaskModel originalTask = extra.OriginalTask;
		if (originalTask == null || originalTask.status != 0 || extra.RepeatTask == null)
		{
			Notifier._tasksChangeEventArgs.StatusChangedIds.Add(extra.OriginalTask?.id);
		}
		else if (!string.IsNullOrEmpty(extra.OriginalTask?.id))
		{
			Notifier._tasksChangeEventArgs.DateChangedIds.Add(extra.OriginalTask?.id);
		}
		if (!string.IsNullOrEmpty(extra.RepeatTask?.id))
		{
			Notifier._tasksChangeEventArgs.AddIds.Add(extra.RepeatTask?.id);
		}
		Notifier.TryNotify(400);
	}

	public static async void NotifyTaskDateChanged(string taskId, object sender = null)
	{
		if (taskId != null)
		{
			Notifier._tasksChangeEventArgs.DateChangedIds.Add(taskId);
			Notifier.TryNotify(100);
		}
	}

	public static async void NotifyTaskCopied(List<string> taskIds, bool waitNotify = false)
	{
		if (waitNotify)
		{
			await Task.Delay(350);
		}
		Notifier._tasksChangeEventArgs.AddIds.AddRange(taskIds);
		Notifier.TryNotify(100);
	}

	public static void NotifyTaskProjectChanged(TaskModel task)
	{
		Notifier._tasksChangeEventArgs.ProjectChangedIds.Add(task.id);
		Notifier.TryNotify(100);
	}

	public static void NotifyTaskTextChanged(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			Notifier._tasksChangeEventArgs.TaskTextChangedIds.Add(taskId);
			Notifier.TryNotify(100, needSync: false);
		}
	}

	public static async void NotifyBatchDateChanged(List<string> taskIds, object sender = null, bool sync = true)
	{
		Notifier._tasksChangeEventArgs.DateChangedIds.AddRange(taskIds);
		Notifier.TryNotify(100, sync);
	}

	public static void NotifyTaskPriorityChanged(string id, int priority, List<string> ids = null, object sender = null)
	{
		ids = (string.IsNullOrEmpty(id) ? ids : new List<string> { id });
		Notifier._tasksChangeEventArgs.PriorityChangedIds.AddRange(ids);
		Notifier.TryNotify(100);
	}

	public static void NotifyTaskTagsChanged(TagExtra tags)
	{
		Notifier._tasksChangeEventArgs.TagChangedIds.AddRange(tags?.GetIds());
		Notifier.TryNotify(100);
	}

	public static void NotifyDropToItem(string taskId)
	{
		Notifier._tasksChangeEventArgs.DeletedChangedIds.Add(taskId);
		Notifier.TryNotify(10, needSync: false);
	}

	public static void NotifyTasksMerged(List<string> taskIds)
	{
		Notifier._tasksChangeEventArgs.BatchChangedIds.AddRange(taskIds);
		Notifier.TryNotify(10, needSync: false);
	}

	public static void NotifyTaskOpenChanged(string taskId)
	{
		Notifier._tasksChangeEventArgs.TasksOpenChangedIds.Add(taskId);
		Notifier.TryNotify(10, needSync: false);
	}

	public static void NotifyTaskPomoAdded(List<string> taskIds)
	{
		if (taskIds != null && taskIds.Count > 0)
		{
			Notifier._tasksChangeEventArgs.PomoChangedIds.AddRange(taskIds);
			Notifier.TryNotify(10, needSync: false);
		}
	}

	public static void NotifyTaskKindChanged(List<string> ids)
	{
		Notifier._tasksChangeEventArgs.KindChangedIds.AddRange(ids);
		Notifier.TryNotify();
	}

	public static void NotifyTasksCopied(List<string> ids)
	{
		Notifier._tasksChangeEventArgs.BatchChangedIds.AddRange(ids);
		Notifier.TryNotify();
	}

	public static void NotifyTaskAssigneeChanged(List<string> ids, string assignee)
	{
		if (ids != null)
		{
			Notifier._tasksChangeEventArgs.AssignChangedIds.AddRange(ids);
			Notifier.TryNotify(50);
		}
	}

	public static void NotifyAttachmentChanged(string taskId, object sender)
	{
		Notifier._tasksChangeEventArgs.AttachmentChangedIds.Add(taskId);
		Notifier.TryNotify(50, needSync: false);
	}

	public static void NotifyTaskNeedScrollToBottom(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			Notifier._tasksChangeEventArgs.ScrollToBottomIds.Add(taskId);
			Notifier.TryNotify(50, needSync: false);
		}
	}

	public static void NotifyTaskBatchChanged(List<string> ids)
	{
		Notifier._tasksChangeEventArgs.BatchChangedIds.AddRange(ids);
		Notifier.TryNotify(50, needSync: false);
	}

	public static async void OnServerTaskChanged(List<string> ids)
	{
		if (ids != null && ids.Count > 0)
		{
			await Task.Delay(1000);
			Notifier._tasksChangeEventArgs.BatchChangedIds.AddRange(ids);
			Notifier.TryNotify(50, needSync: false);
		}
	}

	public static void NotifyBatchTaskStarredChanged(TaskPinExtra ext)
	{
		Notifier._tasksChangeEventArgs.PinChangedIds.AddRange(ext.Ids);
		Notifier.TryNotify(50);
	}

	public static void OnCheckItemChanged(string itemId, bool needSync = false)
	{
		Notifier._tasksChangeEventArgs.CheckItemChangedIds.Add(itemId);
		Notifier.TryNotify(50, needSync);
	}

	public static void OnCheckItemsChanged(List<string> itemIds, bool needSync = false)
	{
		Notifier._tasksChangeEventArgs.CheckItemChangedIds.AddRange(itemIds);
		Notifier.TryNotify(50, needSync);
	}

	public static void NotifyTaskParentChanged(List<string> parentChangedIds)
	{
		Notifier._tasksChangeEventArgs.SortOrderChangedIds.AddRange(parentChangedIds);
		Notifier.TryNotify();
	}
}
