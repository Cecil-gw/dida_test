using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KotlinModels;
using TickTickHandler;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.KotlinUtils;

public static class TaskDllInitializer
{
	static TaskDllInitializer()
	{
		TickTickTask.CreateTaskCallback(CreateTaskFun);
		TickTickTask.GetTaskByIdCallback(GetTaskByIdFun);
		TickTickTask.UpdateTaskCallback(UpdateTaskFun);
		TickTickTask.UpdateTaskAndRemindersCallback(UpdateTaskAndRemindersFun);
		TickTickTask.CloneTaskCallback(CloneTaskFun);
		TickTickTask.GetTaskDefaultReminderParamsCallback(GetTaskDefaultReminderParamsFun);
		TickTickTask.UpdateCheckListItemsCallback(UpdateCheckListItemsFun);
		TickTickHandler.Utils.TestRemindValidCallback(TestRemindValidFun);
		TickTickHandler.Utils.TestTaskIsModifiedInEarlyMorningCallback(TestTaskIsModifiedInEarlyMorningFun);
		TickTickHandler.Utils.ToastRepeatCountRemainingCallback(ToastRepeatCountRemainingFun);
		TickTickTask.GetTasksByParentSidCallback(GetTasksByParentSidFun);
		TickTickTask.GetTaskInsertOrderCallback(GetTaskInsertOrderFun);
		TickTickTask.UpdateTaskParentCallback(UpdateTaskParentFun);
		TickTickTask.UnCompletedTaskLocationCallback(UnCompletedTaskLocationFun);
		TickTickTask.ResetTaskColumnToDefaultIdIfIsEmptyCallback(ResetTaskColumnToDefaultIdIfIsEmptyFun);
		TickTickTask.TryToScheduleReminderCallback(TryToScheduleReminderFun);
		TickTickTask.ResetPomoDataCallback(ResetPomoDataFun);
		TickTickTask.CorrectPomoAndStopwatchWhenTaskCompleteCallback(CorrectPomoAndStopwatchWhenTaskCompleteFun);
	}

	private static void CorrectPomoAndStopwatchWhenTaskCompleteFun(string id, long taskuid)
	{
	}

	private static void ResetPomoDataFun(TASK task)
	{
	}

	private static void TryToScheduleReminderFun(TASK task, int checklistitemreminderchanged)
	{
	}

	private static void ResetTaskColumnToDefaultIdIfIsEmptyFun(TASK task)
	{
	}

	private static void UnCompletedTaskLocationFun(TASK task)
	{
	}

	private static void UpdateTaskParentFun(TASK task, string newparentId, string oldparentId)
	{
		TaskDao.UpdateParent(task.id, newparentId);
	}

	private static long GetTaskInsertOrderFun(long projectuid, long taskuid, int insertbelowInt)
	{
		bool insertbelow = insertbelowInt != 0;
		long result = 0L;
		Task.WaitAll(Task.Run(async delegate
		{
			TaskModel taskModel = await TaskDao.GetTaskByUid(taskuid);
			if (taskModel != null)
			{
				result = ProjectSortOrderDao.GetNextTaskSortOrderInProject(taskModel.projectId, taskModel.sortOrder, taskModel.parentId, !insertbelow);
			}
		}));
		return result;
	}

	private static List<TASK> GetTasksByParentSidFun(List<string> ids, int withcloseInt)
	{
		List<TASK> tasks = new List<TASK>();
		foreach (string id in ids)
		{
			TaskBaseViewModel task = TaskCache.GetTaskById(id);
			if (task == null)
			{
				continue;
			}
			Task.WaitAll(Task.Run(async delegate
			{
				foreach (TaskModel child in await TaskService.GetAllSubTasksByIdAsync(id, task.ProjectId))
				{
					List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(child.id);
					List<TaskDetailItemModel> items = await TaskDetailItemDao.GetCheckItemsByTaskId(child.id);
					tasks.Add(TASKDao.NewTASK(child, items, reminders));
				}
			}));
		}
		return tasks;
	}

	private static void ToastRepeatCountRemainingFun(TASK task)
	{
	}

	private static void TestTaskIsModifiedInEarlyMorningFun(TTCalendar modify, TTCalendar start)
	{
	}

	private static void TestRemindValidFun(TASK task)
	{
	}

	public static void Init()
	{
	}

	private static async void UpdateCheckListItemsFun(TASK task, List<CheckItem> items)
	{
		if (items == null || items.Count <= 0)
		{
			return;
		}
		foreach (CheckItem item in items)
		{
			string id = item.id;
			System.DateTime value = ModelsDao.TTCalendarToDateTime(item.startDate);
			TaskDetailItemService.SaveModel(completeDate: ModelsDao.TTCalendarToDateTime(item.completedDate), itemId: id, startDate: value, status: item.isChecked);
		}
	}

	private static TaskDefaultReminderParams GetTaskDefaultReminderParamsFun()
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		List<string> defaultReminderDueTime = ((!string.IsNullOrEmpty(defaultSafely.TimeReminders)) ? defaultSafely.TimeReminders.Split(',').ToList() : new List<string>());
		List<string> defaultReminderAllDay = ((!string.IsNullOrEmpty(defaultSafely.AllDayReminders)) ? defaultSafely.AllDayReminders.Split(',').ToList() : new List<string>());
		return new TaskDefaultReminderParams
		{
			defaultReminderDueTime = defaultReminderDueTime,
			defaultReminderAllDay = defaultReminderAllDay
		};
	}

	private static TASK CloneTaskFun(TASK task, int withChildInt)
	{
		bool flag = withChildInt != 0;
		string taskId = task.id;
		TASK clone = null;
		Task.WaitAll(Task.Run(async delegate
		{
			List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(taskId);
			TaskModel localTask = await TaskDao.GetTaskById(taskId);
			List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
			foreach (TaskDetailItemModel item in list)
			{
				item.id = Utils.GetGuid();
			}
			clone = TASKDao.NewTASK(localTask, list, reminders, null, null, isClone: true);
		}));
		clone.recurringStartDate = task.recurringStartDate;
		clone.recurringDueDate = task.recurringDueDate;
		string newId = clone.id;
		if (flag)
		{
			Thread thread = new Thread((ThreadStart)async delegate
			{
				List<string> childIds = (from t in await TaskService.CopySubTasks(taskId, newId)
					where t.parentId == newId
					select t.id).ToList();
				clone.childIds = childIds;
			});
			thread.Start();
			int num = 0;
			while (thread.IsAlive && num < 50)
			{
				num++;
				Thread.Sleep(20);
			}
		}
		return clone;
	}

	[Obsolete]
	private static TASK CreateTaskFun(TASK task)
	{
		DataChangedNotifier.NotifyNewTaskWhenModifyRepeat(task.id, ModifyRepeatHandler.HandlingTaskId);
		TASKDao.InsertModel(task).RunSynchronously();
		TaskModel result = TaskDao.GetThinTaskById(task.id).Result;
		task.uniqueId = result._Id;
		return task;
	}

	public static async void UpdateTaskAndRemindersFun(TASK task)
	{
		string taskId = task.id;
		int i = 1;
		while (!TaskCache.ExistTask(taskId) && i <= 5)
		{
			await Task.Delay(i * 20);
		}
		if (TaskCache.ExistTask(taskId))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && !task.repeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
				if (taskById != null)
				{
					System.DateTime value = ModelsDao.TTCalendarToDateTime(task.startDate);
					System.DateTime? dateTime = ModifyRepeatHandler.HandleToTime?.StartDate;
					System.DateTime? startDate = taskById.StartDate;
					if (value == startDate && dateTime != startDate)
					{
						if (!string.IsNullOrEmpty(taskById.RepeatFlag) && taskById.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
						{
							task.repeatFlag += ";TT_SKIP=OFFICIAL_WORKDAY";
						}
					}
					else if (value == dateTime && !string.IsNullOrEmpty(ModifyRepeatHandler.HandleToTime?.RepeatFlag) && ModifyRepeatHandler.HandleToTime.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
					{
						task.repeatFlag = ModifyRepeatHandler.HandleToTime.RepeatFlag;
					}
				}
			}
			await TASKDao.UpdateTask(task, withReminder: true);
		}
		else
		{
			DataChangedNotifier.NotifyNewTaskWhenModifyRepeat(task.id, ModifyRepeatHandler.HandlingTaskId);
			await TASKDao.InsertModel(task, withReminder: true);
		}
		await Task.Delay(500);
	}

	private static async void UpdateTaskFun(TASK task)
	{
		string id = task.id;
		if (TaskCache.ExistTask(id))
		{
			if (!string.IsNullOrEmpty(task.repeatFlag) && !task.repeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(id);
				if (taskById != null)
				{
					System.DateTime value = ModelsDao.TTCalendarToDateTime(task.startDate);
					System.DateTime? dateTime = ModifyRepeatHandler.HandleToTime?.StartDate;
					System.DateTime? startDate = taskById.StartDate;
					if (value == startDate && dateTime != startDate)
					{
						if (!string.IsNullOrEmpty(taskById.RepeatFlag) && taskById.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
						{
							task.repeatFlag += ";TT_SKIP=OFFICIAL_WORKDAY";
						}
					}
					else if (value == dateTime && !string.IsNullOrEmpty(ModifyRepeatHandler.HandleToTime?.RepeatFlag) && ModifyRepeatHandler.HandleToTime.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
					{
						task.repeatFlag = ModifyRepeatHandler.HandleToTime.RepeatFlag;
					}
				}
			}
			await TASKDao.UpdateTask(task);
		}
		else
		{
			DataChangedNotifier.NotifyNewTaskWhenModifyRepeat(task.id, ModifyRepeatHandler.HandlingTaskId);
			await TASKDao.InsertModel(task);
		}
		await Task.Delay(500);
	}

	private static TASK GetTaskByIdFun(string taskId)
	{
		TASK result = new TASK();
		Task.WaitAll(Task.Run(async delegate
		{
			TaskModel task = await TaskDao.GetTaskById(taskId);
			if (task != null)
			{
				result = TASKDao.NewTASK(reminders: await TaskReminderDao.GetRemindersByTaskId(task.id), task: task, items: await TaskDetailItemDao.GetCheckItemsByTaskId(task.id));
			}
		}));
		return result;
	}
}
