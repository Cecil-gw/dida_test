using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KotlinModels;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.KotlinUtils;

public class TASKDao
{
	public static TASK NewTASK(TaskModel task, List<TaskDetailItemModel> items, List<TaskReminderModel> reminders, DateTime? repeatStart = null, DateTime? repeatDue = null, bool isClone = false)
	{
		TASK tASK = new TASK();
		ProjectModel projectById = ProjectAndTaskIdsCache.GetProjectById(task.projectId);
		tASK.uniqueId = task._Id;
		tASK.completedUserId = (long.TryParse(task.completedUserId, out var result) ? result : 0);
		tASK.id = (isClone ? Utils.GetGuid() : task.id);
		tASK.attendId = task.attendId;
		tASK.repeatFlag = task.repeatFlag;
		tASK.repeatFrom = task.repeatFrom;
		tASK.repeatTaskId = task.repeatTaskId;
		tASK.kind = task.kind;
		tASK.timeZone = task.timeZone;
		tASK.exDate = (string.IsNullOrEmpty(task.exDates) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(task.exDates));
		tASK.childIds = task.childIds;
		tASK.reminders = reminders?.Select((TaskReminderModel item) => ModelsDao.NewReminder(item, task.userId, task.userId))?.ToList() ?? new List<Reminder>();
		tASK.items = items?.Select((TaskDetailItemModel item) => ModelsDao.NewCheckItem(item, task.timeZone, task.id != item.TaskServerId))?.ToList() ?? new List<CheckItem>();
		tASK.taskStatus = task.status;
		tASK.isAllDay = ((task.isAllDay ?? true) ? 1 : 0);
		tASK.isFloating = ((task.isFloating == true) ? 1 : 0);
		tASK.progress = task.progress.GetValueOrDefault();
		tASK.startDate = ((!task.startDate.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.startDate.Value, task.timeZone, handleTimezone: true));
		tASK.dueDate = ((!task.dueDate.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.dueDate.Value, task.timeZone, handleTimezone: true));
		tASK.completedDate = ((!task.completedTime.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.completedTime.Value, task.timeZone, handleTimezone: true));
		tASK.repeatFirstDate = ((!task.repeatFirstDate.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.repeatFirstDate.Value, task.timeZone, handleTimezone: true));
		tASK.recurringStartDate = ((!repeatStart.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(repeatStart.Value, task.timeZone, handleTimezone: true));
		tASK.recurringDueDate = ((!repeatDue.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(repeatDue.Value, task.timeZone, handleTimezone: true));
		tASK.tempDueDate = new TTCalendar();
		tASK.tempStartDate = new TTCalendar();
		tASK.projectUid = projectById?._Id ?? 0;
		tASK.projectId = ((projectById == null) ? string.Empty : projectById.id);
		tASK.parentId = task.parentId;
		tASK.userId = task.userId;
		tASK.sortOrder = task.sortOrder;
		tASK.modifiedTime = ((!task.modifiedTime.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.modifiedTime.Value, TimeZoneData.LocalTimeZoneModel.TimeZoneName));
		tASK.snoozeRemindTime = ((!task.remindTime.HasValue) ? new TTCalendar() : ModelsDao.NewTTCalendar(task.remindTime.Value, TimeZoneData.LocalTimeZoneModel.TimeZoneName));
		return tASK;
	}

	public static async Task InsertModel(TASK temp, bool withReminder = false)
	{
		TaskModel originTask = await (from t in App.Connection.Table<TaskModel>()
			where (long)t._Id == temp.uniqueId
			select t).FirstOrDefaultAsync();
		if (originTask == null)
		{
			return;
		}
		TaskModel task = new TaskModel
		{
			title = originTask.title,
			content = originTask.content,
			desc = originTask.desc,
			columnId = originTask.columnId
		};
		try
		{
			task.id = temp.id;
			task.attendId = temp.attendId;
			task.repeatFlag = temp.repeatFlag;
			task.repeatFrom = temp.repeatFrom;
			task.repeatTaskId = temp.repeatTaskId;
			task.kind = temp.kind;
			task.timeZone = temp.timeZone;
			task.userId = temp.userId;
		}
		catch (Exception)
		{
		}
		if (string.IsNullOrEmpty(task.id))
		{
			return;
		}
		task.status = temp.taskStatus;
		task.sortOrder = temp.sortOrder;
		task.isAllDay = temp.isAllDay == 1;
		task.isFloating = temp.isFloating == 1;
		task.startDate = ModelsDao.TTCalendarToLocalDateTime(temp.startDate);
		task.dueDate = ModelsDao.TTCalendarToLocalDateTime(temp.dueDate);
		task.completedTime = ModelsDao.TTCalendarToLocalDateTime(temp.completedDate);
		task.modifiedTime = ModelsDao.TTCalendarToLocalDateTime(temp.modifiedTime);
		task.remindTime = ModelsDao.TTCalendarToLocalDateTime(temp.snoozeRemindTime);
		task.completedUserId = temp.completedUserId.ToString();
		task.projectId = temp.projectId;
		task.parentId = temp.parentId;
		task.priority = originTask.priority;
		task.tag = originTask.tag;
		task.annoyingAlert = (ModifyRepeatHandler.ShouldApplyHandlingAnnoyingAlert(task.id, isNewTask: true) ? ModifyRepeatHandler.HandlingAnnoyingAlert : originTask.annoyingAlert);
		List<string> list = temp.exDate.ToList();
		task.exDates = JsonConvert.SerializeObject(list);
		task.exDate = list.ToArray();
		List<string> children = temp.childIds.ToList();
		task.childrenString = JsonConvert.SerializeObject(children);
		await TaskDao.InsertTask(task);
		DateTime? dateTime = ModelsDao.TTCalendarToLocalDateTime(temp.recurringStartDate);
		int num;
		if (dateTime.HasValue)
		{
			DateTime date = dateTime.Value.Date;
			DateTime? obj = originTask.startDate?.Date;
			num = ((date == obj) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool modifyFirstRecurrence = (byte)num != 0;
		if (!modifyFirstRecurrence)
		{
			await Utils.CopyPomoSummaries(task, originTask.id, clear: true);
		}
		else
		{
			await Utils.CopyPomoSummaries(task, originTask.id);
			await PomoSummaryDao.CleanPomosByTaskId(originTask.id);
			TaskService.ClearMovedPomoSummaries(originTask);
			await TaskDetailItemService.UncheckDetailItemsByTaskId(originTask.id);
			await TaskService.UncheckAllSubTasks(originTask);
			await SyncStatusDao.AddModifySyncStatus(originTask.id);
		}
		await Utils.CopyAttachmentItem(task, originTask.id);
		await ModelsDao.InsertModels(temp.items, task.id, modifyFirstRecurrence);
		TaskChangeNotifier.NotifyTaskAdded(task);
		if (withReminder)
		{
			await ModelsDao.InsertModels(temp.reminders, task.id);
		}
		await SyncStatusDao.AddCreateSyncStatus(task.id);
		if (!string.IsNullOrEmpty(task.parentId))
		{
			await SyncStatusDao.AddSetParentSyncStatus(task.id, string.Empty);
			await TaskDao.AddOrRemoveTaskChildIds(task.parentId, new List<string> { task.id }, addChild: true);
		}
		if (children == null || children.Count <= 0)
		{
			return;
		}
		List<TaskBaseViewModel> allSubTasksById = TaskCache.GetAllSubTasksById(task.id, task.projectId);
		foreach (TaskBaseViewModel child in allSubTasksById)
		{
			await TaskService.UncheckTaskItem(child.Id);
			await SyncStatusDao.AddCreateSyncStatus(child.Id);
			await SyncStatusDao.AddSetParentSyncStatus(child.Id, string.Empty);
			await TaskDao.AddOrRemoveTaskChildIds(child.ParentId, new List<string> { child.Id }, addChild: true);
		}
	}

	public static async Task UpdateTask(TASK task, bool withReminder = false)
	{
		string taskId = task.id;
		TaskModel local = await TaskDao.GetTaskById(taskId);
		if (local != null)
		{
			try
			{
				local.attendId = task.attendId;
				local.repeatFlag = task.repeatFlag;
				local.repeatFrom = task.repeatFrom;
				local.repeatTaskId = task.repeatTaskId;
				local.kind = task.kind;
				local.timeZone = task.timeZone;
				local.projectId = task.projectId;
				local.parentId = task.parentId;
				local.userId = task.userId;
			}
			catch (Exception)
			{
			}
			local.status = task.taskStatus;
			local.isAllDay = task.isAllDay == 1;
			local.isFloating = task.isFloating == 1;
			local.startDate = ModelsDao.TTCalendarToLocalDateTime(task.startDate);
			local.dueDate = ModelsDao.TTCalendarToLocalDateTime(task.dueDate);
			local.completedTime = ModelsDao.TTCalendarToLocalDateTime(task.completedDate);
			local.completedUserId = task.completedUserId.ToString();
			local.modifiedTime = ModelsDao.TTCalendarToDateTime(task.modifiedTime);
			local.remindTime = ModelsDao.TTCalendarToDateTime(task.snoozeRemindTime);
			local.sortOrder = task.sortOrder;
			if (ModifyRepeatHandler.ShouldApplyHandlingAnnoyingAlert(local.id, isNewTask: false))
			{
				local.annoyingAlert = ModifyRepeatHandler.HandlingAnnoyingAlert;
			}
			List<string> list = task.exDate.ToList();
			local.exDates = ((list == null) ? string.Empty : JsonConvert.SerializeObject(list));
			local.exDate = list?.ToArray() ?? new string[0];
			List<string> list2 = task.childIds?.ToList();
			local.childrenString = ((list2 == null) ? null : JsonConvert.SerializeObject(list2));
			ModelsDao.SaveModels(task.items);
			if (withReminder)
			{
				await TaskReminderDao.DeleteRemindersByTaskId(taskId);
				await ModelsDao.InsertModels(task.reminders, local.id);
				ReminderCalculator.AssembleReminders();
			}
			await TaskService.UpdateTask(local);
			await SyncStatusDao.AddModifySyncStatus(local.id);
			TaskChangeNotifier.NotifyTaskDateChanged(local.id);
		}
	}
}
