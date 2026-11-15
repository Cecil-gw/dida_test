using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Service.Display;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Dal;

public class TaskDao : BaseDao<TaskModel>
{
	private class OrderInDateModel
	{
		public string TaskId { get; set; }

		public long SortOrder { get; set; }

		public string Date { get; set; }
	}

	public class OrderModel
	{
		public string TaskId { get; set; }

		public long SortOrder { get; set; }
	}

	public class TitleModel
	{
		public string TaskId { get; set; }

		public string Kind { get; set; }

		public string Title { get; set; }
	}

	public static async Task<ObservableCollection<TaskModel>> GetTasksInTaskIds(List<string> taskIds)
	{
		if (taskIds == null)
		{
			return null;
		}
		List<string> ids = taskIds.Where((string id) => !string.IsNullOrEmpty(id)).Distinct().ToList();
		if (ids.Count == 0)
		{
			return new ObservableCollection<TaskModel>();
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<TaskModel> result = new List<TaskModel>();
		for (int start = 0; start < ids.Count; start += 200)
		{
			List<string> batch = ids.Skip(start).Take(200).ToList();
			result.AddRange(await (from v in App.Connection.Table<TaskModel>()
				where v.userId == userId && v.deleted == 0 && batch.Contains(v.id)
				select v).ToListAsync());
		}
		return new ObservableCollection<TaskModel>(result);
	}

	public static async Task<ObservableCollection<TaskModel>> GetEmptyColumnTaskInProject(string projectId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return new ObservableCollection<TaskModel>(await (from v in App.Connection.Table<TaskModel>()
			where v.userId == userId && v.projectId == projectId && v.columnId == "" && v.status != 0
			select v).ToListAsync());
	}

	public static async Task<ObservableCollection<TaskModel>> GetTasksInProjectIds(IEnumerable<string> projectIds)
	{
		ObservableCollection<TaskModel> result = new ObservableCollection<TaskModel>();
		foreach (string projectId in projectIds)
		{
			foreach (TaskModel item in await GetTasksInProjectAsync(projectId))
			{
				result.Add(item);
			}
		}
		return result;
	}

	public static async Task<TaskModel> GetFullTaskById(string id, bool checkTextCount = false)
	{
		TaskModel taskModel = await GetTaskById(id);
		if (taskModel != null)
		{
			taskModel.content = TaskUtils.NormalizeHorizontalRuleLineEndings(taskModel.content);
			if (checkTextCount)
			{
				string title = taskModel.title;
				if (title != null && title.Length > 204800)
				{
					UserActCollectUtils.SendCustomException("TitleOverLimit " + taskModel.title.Length);
					taskModel.title = taskModel.title.Substring(0, 204800);
				}
				string content = taskModel.content;
				if (content != null && content.Length > 204800)
				{
					UserActCollectUtils.SendCustomException("ContentOverLimit " + taskModel.content.Length);
					taskModel.content = taskModel.content.Substring(0, 204800);
				}
				string desc = taskModel.desc;
				if (desc != null && desc.Length > 204800)
				{
					UserActCollectUtils.SendCustomException("DescOverLimit " + taskModel.desc.Length);
					taskModel.desc = taskModel.desc.Substring(0, 204800);
				}
			}
			taskModel.exDate = ((!string.IsNullOrEmpty(taskModel.exDates)) ? ExDateSerilizer.ToArray(taskModel.exDates) : new string[0]);
			await AssembleFullTask(taskModel, checkTextCount);
		}
		return taskModel;
	}

	public static async Task<TaskModel> GetTaskById(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return null;
		}
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from v in App.Connection.Table<TaskModel>()
			where v.id.Equals(taskId) && v.userId == userId
			select v).FirstOrDefaultAsync();
	}

	public static async Task<TaskModel> GetThinTaskById(string taskId)
	{
		return await GetTaskById(taskId);
	}

	public static async Task<List<TaskModel>> GetTasksByIds(IEnumerable<string> taskIds)
	{
		if (taskIds == null || !taskIds.Any())
		{
			return new List<TaskModel>();
		}
		List<string> list = taskIds.ToList();
		List<TaskModel> result = new List<TaskModel>();
		foreach (string item in list)
		{
			TaskModel taskModel = await GetTaskById(item);
			if (taskModel != null)
			{
				result.Add(taskModel);
			}
		}
		return result;
	}

	public static async Task<List<TaskModel>> GetThinTasksInBatch(List<string> taskIds)
	{
		taskIds = taskIds.ToList();
		return await GetTasksByIds(taskIds);
	}

	public static async Task<Dictionary<string, TaskEtag>> GetTask2SidMap(List<string> ids)
	{
		string sql = "select _Id as _Id , id as TaskId, etag as Etag from TaskModel where id in ({0}) and userId = '" + LocalSettings.Settings.LoginUserId + "'";
		Dictionary<string, TaskEtag> result = new Dictionary<string, TaskEtag>();
		foreach (TaskEtag item in await SqliteUtils.BatchGetModelsAsync<TaskEtag>(sql, ids))
		{
			if (!string.IsNullOrEmpty(item.TaskId) && !result.ContainsKey(item.TaskId))
			{
				result[item.TaskId] = item;
			}
		}
		return result;
	}

	private static async Task<List<string>> GetTaskIds()
	{
		return TaskCache.GetAllTaskIds();
	}

	public static async Task BatchCreateTaskFromRemote(List<TaskModel> tasks, bool needHandleItem)
	{
		if (tasks == null)
		{
			UtilLog.Warn("BatchCreateTaskFromRemote tasks is null");
		}
		List<string> localTaskIds = await GetTaskIds();
		List<TaskModel> list = tasks?.Where((TaskModel task) => !localTaskIds.Contains(task.id)).ToList() ?? new List<TaskModel>();
		List<TaskReminderModel> reminderList = new List<TaskReminderModel>();
		List<TaskDetailItemModel> checklistItems = new List<TaskDetailItemModel>();
		List<AttachmentModel> attachmentList = new List<AttachmentModel>();
		List<PomodoroSummaryModel> pomos = new List<PomodoroSummaryModel>();
		List<LocationModel> locations = new List<LocationModel>();
		foreach (TaskModel item in list)
		{
			if (item == null)
			{
				UtilLog.Warn("BatchCreateTaskFromRemote remoteCreateTasks has null");
				continue;
			}
			item.userId = Utils.GetCurrentUserIdInt().ToString();
			HandleServerDate(item);
			if (item.reminders != null)
			{
				TaskReminderModel[] reminders = item.reminders;
				foreach (TaskReminderModel taskReminderModel in reminders)
				{
					if (taskReminderModel == null)
					{
						UtilLog.Warn("BatchCreateTaskFromRemote task " + item.id + " reminders has null");
						continue;
					}
					taskReminderModel.Taskid = item._Id;
					taskReminderModel.taskserverid = item.id;
				}
				reminderList.AddRange(item.reminders.Where((TaskReminderModel x) => x != null));
			}
			if (item.items != null)
			{
				TaskDetailItemModel[] items = item.items;
				foreach (TaskDetailItemModel taskDetailItemModel in items)
				{
					if (taskDetailItemModel == null)
					{
						UtilLog.Warn("BatchCreateTaskFromRemote task " + item.id + " items has null");
						continue;
					}
					taskDetailItemModel.TaskId = item._Id;
					taskDetailItemModel.TaskServerId = item.id;
					if (needHandleItem)
					{
						HandleItemServerDate(taskDetailItemModel, item.timeZone, item.Floating);
					}
				}
				checklistItems.AddRange(item.items.Where((TaskDetailItemModel x) => x != null));
			}
			if (item.Attachments != null)
			{
				AttachmentModel[] attachments = item.Attachments;
				foreach (AttachmentModel attachmentModel in attachments)
				{
					if (attachmentModel == null)
					{
						UtilLog.Warn("BatchCreateTaskFromRemote task " + item.id + " Attachments has null");
						continue;
					}
					attachmentModel.taskId = item.id;
					attachmentModel.sync_status = 2.ToString();
				}
				attachmentList.AddRange(item.Attachments.Where((AttachmentModel x) => x != null));
			}
			if (item.FocusSummaries != null)
			{
				PomodoroSummaryModel[] focusSummaries = item.FocusSummaries;
				foreach (PomodoroSummaryModel pomodoroSummaryModel in focusSummaries)
				{
					if (pomodoroSummaryModel == null)
					{
						UtilLog.Warn("BatchCreateTaskFromRemote task " + item.id + " FocusSummaries has null");
						continue;
					}
					pomodoroSummaryModel.id = Utils.GetGuid();
					pomodoroSummaryModel.taskId = item.id;
					LocalSettings.Settings.ObserveSyncedFocusSummary(pomodoroSummaryModel);
				}
				pomos.AddRange(item.FocusSummaries.Where((PomodoroSummaryModel x) => x != null));
			}
			if (item.tags != null)
			{
				item.tag = TagSerializer.ToJsonContent(item.tags.ToList());
			}
			if (item.childIds != null)
			{
				item.childrenString = JsonConvert.SerializeObject(item.childIds);
			}
			if (item.exDate != null)
			{
				item.exDates = ExDateSerilizer.ToString(item.exDate);
			}
			if (item.location?.loc != null)
			{
				item.hasLocation = 1;
				item.location.userId = Utils.GetCurrentUserIdInt().ToString();
				item.location.taskId = item.id;
				item.location.latitude = item.location.loc.latitude;
				item.location.longitude = item.location.loc.longitude;
				locations.Add(item.location);
			}
		}
		await BatchInsertTasks(list, fromRemote: true);
		if (attachmentList.Count > 0)
		{
			await App.Connection.InsertAllAsync(attachmentList);
			AttachmentCache.ResetDictItems();
		}
		if (reminderList.Count > 0)
		{
			await App.Connection.InsertAllAsync(reminderList);
		}
		if (checklistItems.Count > 0)
		{
			await TaskDetailItemDao.BatchInsertChecklists(checklistItems);
		}
		if (pomos.Count > 0)
		{
			await App.Connection.InsertAllAsync(pomos);
		}
		if (locations.Count > 0)
		{
			await App.Connection.InsertAllAsync(locations);
		}
	}

	private static void HandleItemServerDate(TaskDetailItemModel item, string timeZone, bool isFloating)
	{
		if (((item.isAllDay.HasValue && item.isAllDay.Value) || isFloating) && item.startDate.HasValue)
		{
			item.startDate = DateUtils.ConvertTimeZone(item.startDate.Value, timeZone);
		}
	}

	public static void HandleServerDate(TaskModel item)
	{
		if ((item.isAllDay.HasValue && item.isAllDay.Value) || item.Floating)
		{
			if (item.startDate.HasValue)
			{
				item.startDate = DateUtils.ConvertTimeZone(item.startDate.Value, item.timeZone);
			}
			if (item.dueDate.HasValue)
			{
				item.dueDate = DateUtils.ConvertTimeZone(item.dueDate.Value, item.timeZone);
			}
		}
	}

	public static async Task BatchInsertTasksAndItems(List<TaskModel> tasks)
	{
		if (tasks == null || !tasks.Any())
		{
			return;
		}
		List<TaskDetailItemModel> checkListItems = new List<TaskDetailItemModel>();
		List<AttachmentModel> attachments = new List<AttachmentModel>();
		List<TaskReminderModel> reminderList = new List<TaskReminderModel>();
		foreach (TaskModel task in tasks)
		{
			if (task.items != null)
			{
				TaskDetailItemModel[] items = task.items;
				for (int i = 0; i < items.Length; i++)
				{
					items[i].TaskServerId = task.id;
				}
				checkListItems.AddRange(task.items);
			}
			if (task.Attachments != null)
			{
				AttachmentModel[] attachments2 = task.Attachments;
				for (int i = 0; i < attachments2.Length; i++)
				{
					attachments2[i].taskId = task.id;
				}
				attachments.AddRange(task.Attachments);
			}
			if (task.reminders != null)
			{
				TaskReminderModel[] reminders = task.reminders;
				for (int i = 0; i < reminders.Length; i++)
				{
					reminders[i].taskserverid = task.id;
				}
				reminderList.AddRange(task.reminders);
			}
		}
		foreach (TaskModel task2 in tasks)
		{
			PrepareCorrectedTags(task2);
		}
		await App.Connection.InsertAllAsync(tasks);
		foreach (TaskModel task3 in tasks)
		{
			await SaveTagCorrectionStatus(task3, needSync: false);
		}
		List<TaskBaseViewModel> vms = TaskCache.InsertAll(tasks);
		if (attachments.Count > 0)
		{
			await App.Connection.InsertAllAsync(attachments);
		}
		if (reminderList.Count > 0)
		{
			await App.Connection.InsertAllAsync(reminderList);
		}
		if (checkListItems.Count > 0)
		{
			await TaskDetailItemDao.BatchInsertChecklists(checkListItems);
		}
		ProjectAndTaskIdsCache.OnTasksChanged(vms, CheckMatchedType.All);
		await SyncStatusDao.BatchAddCreateTaskStatus(tasks.Select((TaskModel t) => t.id));
	}

	public static async Task BatchInsertTasks(List<TaskModel> tasks, bool fromRemote = false)
	{
		if (tasks == null || !tasks.Any())
		{
			return;
		}
		foreach (TaskModel task in tasks)
		{
			PrepareCorrectedTags(task);
		}
		await App.Connection.InsertAllAsync(tasks);
		foreach (TaskModel task2 in tasks)
		{
			await SaveTagCorrectionStatus(task2, fromRemote);
		}
		ProjectAndTaskIdsCache.OnTasksChanged(TaskCache.InsertAll(tasks), CheckMatchedType.All);
	}

	public static async Task<TaskModel> InsertTask(TaskModel task)
	{
		if (task.startDate.HasValue)
		{
			task.startDate = task.startDate.Value;
		}
		if (task.sortOrder == 0L)
		{
			task.sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(task.projectId, task.parentId);
		}
		if (string.IsNullOrEmpty(task.userId))
		{
			task.userId = Utils.GetCurrentUserIdInt().ToString();
		}
		if (await GetTaskById(task.id) == null)
		{
			await InsertTaskToDb(task);
		}
		return task;
	}

	private static async Task InsertTaskToDb(TaskModel task)
	{
		PrepareCorrectedTags(task);
		if (await App.Connection.InsertAsync(task) > 0)
		{
			await SaveTagCorrectionStatus(task, needSync: false);
			TaskCache.Insert(task);
		}
	}

	public static async Task<List<SyncStatusModel>> GetNeedPostTasks(int type)
	{
		string sql = $"select * from SyncStatusModel where type = {type} and userId = '{LocalSettings.Settings.LoginUserId}'";
		return await App.Connection.QueryAsync<SyncStatusModel>(sql, Array.Empty<object>());
	}

	public static async Task<List<string>> GetDeletedIds()
	{
		return TaskCache.GetDeletedTaskIds();
	}

	public static async Task<List<SyncStatusModel>> GetNeedPostMovedOrRestoreTasks(int movedOrRestore)
	{
		return await GetNeedPostTasks(movedOrRestore);
	}

	public static async Task DeleteTaskToTrash(string taskId)
	{
		TaskModel taskModel = await GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (taskModel.deleted != 1)
			{
				taskModel.deleted = 1;
				taskModel.attendId = null;
				taskModel.modifiedTime = DateTime.Now;
			}
			await TaskService.UpdateTaskOnDeletedChanged(taskModel);
		}
	}

	public static async Task DeleteTaskForever(string taskId)
	{
		TaskModel taskModel = await GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (taskModel.deleted != 2)
			{
				taskModel.deleted = 2;
				taskModel.modifiedTime = DateTime.Now;
			}
			await TaskService.UpdateTaskOnDeletedChanged(taskModel);
		}
	}

	public static async Task BatchUpdateTasksFromRemote(TaskSyncBean taskSyncBean)
	{
		foreach (TaskModel item in (taskSyncBean?.Updated ?? new List<TaskModel>()).Concat(taskSyncBean?.Updating ?? new List<TaskModel>()))
		{
			if (item?.FocusSummaries != null)
			{
				PomodoroSummaryModel[] focusSummaries = item.FocusSummaries;
				foreach (PomodoroSummaryModel summary in focusSummaries)
				{
					LocalSettings.Settings.ObserveSyncedFocusSummary(summary);
				}
			}
		}
		if (taskSyncBean != null && taskSyncBean.Updated?.Count > 0)
		{
			List<string> changeParentIds = (await SyncStatusDao.GetEntityIdsByType(16)) ?? new List<string>();
			List<SyncStatusModel> moveStatus = await SyncStatusDao.GetSyncStatusByType(2);
			foreach (TaskModel task in taskSyncBean.Updated)
			{
				if (task == null)
				{
					continue;
				}
				TaskModel local = await GetThinTaskById(task.id);
				if (local != null)
				{
					if (changeParentIds.Contains(task.id))
					{
						task.parentId = local.parentId;
					}
					SyncStatusModel syncStatusModel = moveStatus?.FirstOrDefault((SyncStatusModel m) => m.EntityId == local.id);
					if (syncStatusModel != null)
					{
						if (syncStatusModel.MoveFromId != task.projectId)
						{
							syncStatusModel.MoveFromId = task.projectId;
							await BaseDao<SyncStatusModel>.UpdateAsync(syncStatusModel);
						}
						task.projectId = local.projectId;
					}
					if (!task.TagsNeedSync && IsTaskSame(local, task))
					{
						continue;
					}
				}
				await SaveModifiedTask(task);
			}
		}
		if (taskSyncBean == null || !(taskSyncBean.Updating?.Count > 0))
		{
			return;
		}
		foreach (TaskModel item2 in taskSyncBean.Updating)
		{
			await SaveMergedTask(item2);
		}
	}

	private static async Task SaveMergedTask(TaskModel task)
	{
		if (task != null)
		{
			if (task.tags != null)
			{
				ProjectModel projectById = CacheManager.GetProjectById(task.projectId);
				await TagDao.TryBatchAddTags(task.tags.ToList(), projectById?.IsShareList() ?? false);
			}
			await SaveTaskReminders(task);
			await SaveCheckItems(task);
			task.modifiedTime = DateTime.Now;
			if (task.childIds != null)
			{
				task.childrenString = JsonConvert.SerializeObject(task.childIds);
			}
			await TaskService.UpdateTask(task);
			if (await UserDao.GetUserSyncCheckPoint(LocalSettings.Settings.LoginUserId) != 0L)
			{
				await SyncStatusDao.AddSyncStatus(task.id, 0);
			}
		}
	}

	public static async Task SaveCheckItems(TaskModel task)
	{
		if (task.items != null)
		{
			await TaskDetailItemDao.DeleteCheckItemsByTaskId(task.id, task.items.Select((TaskDetailItemModel i) => i.id).ToList());
			TaskDetailItemModel[] items = task.items;
			foreach (TaskDetailItemModel obj in items)
			{
				obj.TaskId = task._Id;
				obj.TaskServerId = task.id;
				await TaskDetailItemDao.TrySaveChecklistItem(obj);
			}
		}
	}

	private static async Task SaveTaskReminders(TaskModel task)
	{
		if ((task.reminders != null && task.reminders.Length != 0 && task.startDate.HasValue) || (!task.startDate.HasValue && task.reminders == null))
		{
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
			if (task.reminders != null)
			{
				TaskReminderModel[] reminders = task.reminders;
				foreach (TaskReminderModel obj in reminders)
				{
					obj.Taskid = task._Id;
					obj.taskserverid = task.id;
					await TaskReminderDao.SaveReminders(obj);
				}
			}
		}
		else if (task.reminders != null && task.reminders.Length == 0)
		{
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
		}
	}

	private static bool IsTaskSame(TaskModel local, TaskModel server)
	{
		if (local.etag != server.etag)
		{
			return false;
		}
		if (local.status != server.status)
		{
			return false;
		}
		if (local.projectId != server.projectId)
		{
			return false;
		}
		if (local.parentId != server.parentId)
		{
			return false;
		}
		if (local.notionBlock != server.notionBlock)
		{
			return false;
		}
		if (local.annoyingAlert != server.annoyingAlert)
		{
			return false;
		}
		return true;
	}

	private static async Task SaveModifiedTask(TaskModel task)
	{
		if (task.tags != null)
		{
			task.tag = TagSerializer.ToJsonContent(task.tags.ToList());
			ProjectModel projectById = CacheManager.GetProjectById(task.projectId);
			await TagDao.TryBatchAddTags(task.tags.ToList(), projectById?.IsShareList() ?? false);
		}
		if (task.exDate != null)
		{
			task.exDates = ExDateSerilizer.ToString(task.exDate);
		}
		if (task.childIds != null)
		{
			task.childrenString = JsonConvert.SerializeObject(task.childIds);
		}
		try
		{
			HandleServerDate(task);
		}
		catch (Exception)
		{
		}
		await SaveTaskReminders(task);
		await SaveCheckItems(task);
		await SavePomodoroSummaries(task);
		await SaveTaskLocation(task);
		task.modifiedTime = DateTime.Now;
		await TaskService.UpdateTask(task);
	}

	private static async Task SaveTaskLocation(TaskModel task)
	{
		if (task.location?.loc != null)
		{
			task.hasLocation = 1;
			task.location.taskId = task.id;
			task.location.userId = Utils.GetCurrentUserIdInt().ToString();
			task.location.longitude = task.location.loc.longitude;
			task.location.latitude = task.location.loc.latitude;
			await LocationDao.InsertLocation(task.location);
		}
		else
		{
			task.hasLocation = 0;
		}
	}

	public static async Task SavePomodoroSummaries(TaskModel task)
	{
		if (task.FocusSummaries != null)
		{
			await PomoSummaryDao.DeletePomosByTaskId(task.id);
			PomodoroSummaryModel[] focusSummaries = task.FocusSummaries;
			foreach (PomodoroSummaryModel obj in focusSummaries)
			{
				obj.id = Utils.GetGuid();
				obj.taskId = task.id;
				obj.userId = LocalSettings.Settings.LoginUserId;
			}
			await PomoSummaryDao.SavePomoSummaries(task.FocusSummaries.ToList());
		}
	}

	private static string GetDateSql(IEnumerable<string> dates, string start = "c.startDate", string end = "c.dueDate")
	{
		string text = string.Empty;
		foreach (string date in dates)
		{
			switch (date)
			{
			case "today":
				text += AppendDateBetweenSql(DateTime.Now.Date, DateTime.Now.Date.AddDays(1.0), start, end);
				continue;
			case "tomorrow":
				text += AppendDateBetweenSql(DateTime.Now.Date.AddDays(1.0), DateTime.Now.Date.AddDays(2.0), start, end);
				continue;
			case "thisweek":
			{
				int nextWeekDayDiff = DateUtils.GetNextWeekDayDiff();
				text += AppendDateBetweenSql(DateTime.Now.Date.AddDays(nextWeekDayDiff - 7), DateTime.Now.Date.AddDays(nextWeekDayDiff), start, end);
				continue;
			}
			case "nextweek":
			{
				int nextWeekDayDiff2 = DateUtils.GetNextWeekDayDiff();
				text += AppendDateBetweenSql(DateTime.Now.Date.AddDays(nextWeekDayDiff2), DateTime.Now.Date.AddDays(nextWeekDayDiff2 + 7), start, end);
				continue;
			}
			case "thismonth":
			{
				DateTime start2 = DateTime.Now.Date.AddDays((DateTime.Now.Day - 1) * -1);
				DateTime end2 = start2.Date.AddMonths(1);
				text += AppendDateBetweenSql(start2, end2, start, end);
				continue;
			}
			case "nodue":
				text = text + "(" + start + " is null) or";
				continue;
			case "all":
				text += AppendDateBetweenSql(DateTime.Now.Date, DateTime.Now.Date.AddDays(180.0), start, end);
				continue;
			case "overdue":
				text += AppendOverdueSql();
				continue;
			}
			if (date.EndsWith("days"))
			{
				int.TryParse(date.Replace("days", string.Empty), out var result);
				text += AppendDateBetweenSql(DateTime.Now.Date, DateTime.Now.Date.AddDays(result), start, end);
			}
			else if (date.EndsWith("dayslater"))
			{
				int.TryParse(date.Replace("dayslater", string.Empty), out var result2);
				text += $"({start} is not null and {start} >= '{DateTime.Now.Date.AddDays(result2):yyyy-MM-dd 00:00:00}') or";
			}
		}
		if (text.EndsWith("or"))
		{
			text = text.Substring(0, text.Length - 2);
		}
		return " (" + text + ") ";
	}

	private static string AppendOverdueSql()
	{
		return string.Format(" ((c.startDate is not null and c.dueDate is null and c.startDate < '{0:yyyy-MM-dd 00:00:00}') or  (c.dueDate is not null and c.isAllday = 1 and c.dueDate < '{1:yyyy-MM-dd 00:00:00}' ) or (c.dueDate is not null and c.isAllday = 0 and c.dueDate < '{0:yyyy-MM-dd 00:00:00}' )) or", DateTime.Now.Date, DateTime.Now.Date.AddDays(1.0));
	}

	private static string AppendDateBetweenSql(DateTime start, DateTime end, string startColum = "c.startDate", string endColumn = "c.dueDate")
	{
		string text = start.ToString("yyyy-MM-dd 00:00:00", App.Ci);
		string text2 = end.AddDays(-1.0).ToString("yyyy-MM-dd 23:59:59", App.Ci);
		return string.Format(" ((({2} is not null and {2} >= '{0}' and {2} < '{1}')) or (({3} is not null and {3} > '{0}' and {3} < '{1}')) or ({2} is not null and {3} is not null and {2} < '{0}' and {3} > '{1}')) or", text, text2, startColum, endColumn);
	}

	private static string GetAssigneeSql(IEnumerable<string> assignees)
	{
		string text = string.Empty;
		string loginUserId = LocalSettings.Settings.LoginUserId;
		using (IEnumerator<string> enumerator = assignees.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
				case "me":
					text = text + " c.assignee == '" + loginUserId + "' or";
					break;
				case "other":
					text = text + " (c.c.assignee <> '" + loginUserId + "' and c.assignee <> '-1' and c.assignee <> '' and c.assignee is not null and d.userCount>1) or";
					break;
				case "noassignee":
					text += " c.assignee is null or c.assignee = '-1' or c.assignee = '' or";
					break;
				}
			}
		}
		if (text.EndsWith("or"))
		{
			text = text.Substring(0, text.Length - 2);
		}
		return " (" + text + ") ";
	}

	private static string GetProjectOrGroupSql(IReadOnlyList<string> groupIds, IReadOnlyList<string> projectIds)
	{
		if (projectIds.Count > 0 && groupIds.Count == 0)
		{
			return GetProjectSql(projectIds);
		}
		if (projectIds.Count == 0 && groupIds.Count > 0)
		{
			return GetGroupSql(groupIds);
		}
		return " (" + GetProjectSql(projectIds) + " or " + GetGroupSql(groupIds) + " ) ";
	}

	private static string GetGroupSql(IReadOnlyList<string> groupIds)
	{
		return GetItemsSql(groupIds, "d.groupId");
	}

	private static string GetProjectSql(IReadOnlyList<string> projectIds)
	{
		return GetItemsSql(projectIds, "c.projectId");
	}

	private static string GetPrioritySql(IReadOnlyList<int> priorities)
	{
		return GetItemsSql(priorities, "c.priority");
	}

	private static string GetItemsSql<T>(IReadOnlyList<T> items, string column)
	{
		string text = string.Empty;
		for (int i = 0; i < items.Count; i++)
		{
			T val = items[i];
			if (val is int)
			{
				string text2 = text;
				T val2 = val;
				text = text2 + val2;
			}
			else
			{
				string text3 = text;
				T val2 = val;
				text = text3 + "'" + val2?.ToString() + "'";
			}
			if (i < items.Count - 1)
			{
				text += ",";
			}
		}
		return " " + column + " in (" + text + ") ";
	}

	public static async Task<List<TaskModel>> GetUncompletedTasksInProject(string projectId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return new List<TaskModel>(await (from v in App.Connection.Table<TaskModel>()
			where v.userId == userId && v.deleted == 0 && v.status == 0 && v.projectId == projectId
			select v).ToListAsync());
	}

	public static async Task<List<TaskModel>> GetTasksInProjectAsync(string projectId)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		return (await App.Connection.QueryAsync<TaskModel>("Select * from TaskModel WHERE projectId = '" + projectId + "' and userId = '" + loginUserId + "'", Array.Empty<object>())) ?? new List<TaskModel>();
	}

	public static async Task<List<TaskModel>> GetTasksWithNotionBlock()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return (await (from task in App.Connection.Table<TaskModel>()
			where task.userId == userId && task.notionBlock != null && task.notionBlock != ""
			select task).ToListAsync()) ?? new List<TaskModel>();
	}

	public static async Task ClearNotionBlocks(List<string> taskIds)
	{
		if (taskIds == null || taskIds.Count == 0)
		{
			return;
		}
		string userId = LocalSettings.Settings.LoginUserId;
		for (int offset = 0; offset < taskIds.Count; offset += 100)
		{
			List<string> source = taskIds.Skip(offset).Take(100).ToList();
			string text = string.Join(",", source.Select((string id) => "?"));
			List<object> list = new List<object> { userId };
			list.AddRange(source.Cast<object>());
			await App.Connection.ExecuteAsync("UPDATE TaskModel SET notionBlock = '' WHERE userId = ? AND id IN (" + text + ")", list.ToArray());
		}
	}

	public static async Task<TaskModel> AssembleFullTask(TaskModel task, bool checkTextCount = false)
	{
		if (task == null)
		{
			return null;
		}
		TaskModel taskModel;
		if (task.startDate.HasValue)
		{
			taskModel = task;
			taskModel.reminders = (await TaskReminderDao.GetRemindersByTaskId(task.id)).ToArray();
			task.reminderCount = (task.reminders?.Length).Value;
			AdjustLegacyTrigger(task);
		}
		AdjustStartAndDueDate(task);
		if (task.kind == "CHECKLIST")
		{
			taskModel = task;
			taskModel.items = (await TaskDetailItemDao.GetCheckItemsByTaskId(task.id)).ToArray();
			if (task.items != null && task.items.Length != 0)
			{
				TaskDetailItemModel[] items = task.items;
				foreach (TaskDetailItemModel taskDetailItemModel in items)
				{
					taskDetailItemModel.serverStartDate = (taskDetailItemModel.startDate.HasValue ? taskDetailItemModel.startDate.Value.ToString(UtcDateTimeConverter.GetConverterValue(taskDetailItemModel.startDate.Value)) : "-1");
					if (checkTextCount)
					{
						string title = taskDetailItemModel.title;
						if (title != null && title.Length > 204800)
						{
							UserActCollectUtils.SendCustomException("CheckItemTextOverLimit " + taskDetailItemModel.title.Length);
							taskDetailItemModel.title = taskDetailItemModel.title.Substring(0, 204800);
						}
					}
				}
			}
		}
		else
		{
			task.items = null;
		}
		taskModel = task;
		taskModel.Attachments = (await AttachmentDao.GetTaskAttachments(task.id)).ToArray();
		task.attachmentCount = task.Attachments.Length;
		List<PomodoroSummaryModel> list = await PomoSummaryDao.GetPomosByTaskId(task.id);
		if (list != null && list.Count > 0)
		{
			PomodoroSummaryModel pomodoroSummaryModel = null;
			List<PomodoroSummaryModel> list2 = list.ToList();
			foreach (PomodoroSummaryModel item in list)
			{
				if (item.userId != LocalSettings.Settings.LoginUserId)
				{
					continue;
				}
				if (pomodoroSummaryModel == null)
				{
					pomodoroSummaryModel = item;
					continue;
				}
				pomodoroSummaryModel.count += item.count;
				pomodoroSummaryModel.PomoDuration += item.PomoDuration;
				pomodoroSummaryModel.estimatedPomo += item.estimatedPomo;
				pomodoroSummaryModel.EstimatedDuration += item.EstimatedDuration;
				pomodoroSummaryModel.StopwatchDuration += item.StopwatchDuration;
				List<object[]> focuses = item.focuses;
				if (focuses != null && focuses.Count > 0)
				{
					foreach (object[] focuse in item.focuses)
					{
						pomodoroSummaryModel.AddFocuses(focuse, checkExist: true);
					}
				}
				list2.Remove(item);
				App.Connection.DeleteAsync(item);
			}
			task.FocusSummaries = list2.ToArray();
		}
		if (!string.IsNullOrEmpty(task.tag))
		{
			task.tags = TagSerializer.ToTags(task.tag).ToArray();
		}
		if (!string.IsNullOrEmpty(task.exDates))
		{
			task.exDate = ExDateSerilizer.ToArray(task.exDates);
		}
		return task;
	}

	private static void AdjustLegacyTrigger(TaskModel task)
	{
		if (!task.isAllDay.HasValue || !task.isAllDay.Value || task.reminders == null || !task.reminders.Any())
		{
			return;
		}
		TaskReminderModel[] reminders = task.reminders;
		foreach (TaskReminderModel taskReminderModel in reminders)
		{
			if (!string.IsNullOrEmpty(taskReminderModel.trigger))
			{
				taskReminderModel.trigger = TriggerUtils.ConvertLegacyTrigger(taskReminderModel.trigger);
			}
		}
	}

	private static void AdjustStartAndDueDate(TaskModel task)
	{
		if (!task.startDate.HasValue && task.dueDate.HasValue)
		{
			task.startDate = task.dueDate;
			task.dueDate = null;
		}
		if (task.startDate.HasValue && task.startDate >= task.dueDate)
		{
			task.dueDate = null;
		}
	}

	public static async Task<List<TaskModel>> GetAllTask(Expression<Func<TaskModel, bool>> exp = null)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		AsyncTableQuery<TaskModel> asyncTableQuery = from t in App.Connection.Table<TaskModel>()
			where t.userId == userId
			select t;
		if (exp != null)
		{
			asyncTableQuery = asyncTableQuery.Where(exp);
		}
		return (await asyncTableQuery.ToListAsync()) ?? new List<TaskModel>();
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTasksInColumn(string projectId, string columnId, bool isDefaultColumn)
	{
		return await TaskCache.GetModelsInProjectColumn(projectId, columnId, isDefaultColumn);
	}

	public static async Task<List<TaskBaseViewModel>> GetDisplayTasksInDate(string dateStamp)
	{
		return await TaskDisplayService.GetDisplayTaskInDate(dateStamp);
	}

	public static async Task<List<TitleModel>> GetTaskTitleInProject()
	{
		return TaskCache.GetTaskTitleModels();
	}

	public static async Task BatchUpdateTaskOnTagDeleted(string tag)
	{
		List<TaskModel> list = await GetAllTask();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		List<TaskModel> list2 = list.Where((TaskModel task) => !string.IsNullOrEmpty(task.tag) && task.tag.Contains(tag)).ToList();
		if (list2.Count <= 0)
		{
			return;
		}
		foreach (TaskModel item in list2)
		{
			List<string> list3 = TagSerializer.ToTags(item.tag);
			list3.Remove(tag);
			item.tag = TagSerializer.ToJsonContent(list3);
			item.modifiedTime = DateTime.Now;
		}
		await BatchUpdateTasks(list2, checkUserId: false, CheckMatchedType.OnlyFilter);
	}

	public static async Task BatchUpdateAffectedTaskOnTagChanged(string original, string newLabel)
	{
		List<TaskModel> list = await GetAllTask();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		List<TaskModel> list2 = list.Where((TaskModel task) => !string.IsNullOrEmpty(task.tag) && task.tag.ToLower().Contains(AddTagDivider(original))).ToList();
		if (list2.Count <= 0)
		{
			return;
		}
		foreach (TaskModel item in list2)
		{
			string value = AddTagDivider(newLabel);
			if (!item.tag.ToLower().Contains(value))
			{
				item.tag = item.tag.ToLower().Replace(AddTagDivider(original), AddTagDivider(newLabel)).Replace("#", string.Empty)
					.Replace("＃", string.Empty)
					.ToLower();
				item.tags = TagSerializer.ToTags(item.tag).ToArray();
				item.modifiedTime = DateTime.Now;
			}
		}
		await BatchUpdateTasks(list2, checkUserId: false, CheckMatchedType.OnlyFilter);
	}

	private static string AddTagDivider(string tag)
	{
		return "\"" + tag.ToLower() + "\"";
	}

	public static async Task BatchUpdateTasks(List<TaskBaseViewModel> viewModels)
	{
		if (viewModels == null || viewModels.Count <= 0)
		{
			return;
		}
		foreach (TaskBaseViewModel viewModel in viewModels)
		{
			TaskModel taskModel = await viewModel.ToTask();
			if (taskModel != null)
			{
				await UpdateTask(taskModel);
			}
		}
	}

	public static async Task BatchUpdateTasks(List<TaskModel> tasks, bool checkUserId = true, CheckMatchedType checkMatched = CheckMatchedType.None)
	{
		if (tasks == null || tasks.Count <= 0)
		{
			return;
		}
		List<TaskBaseViewModel> vms = new List<TaskBaseViewModel>();
		foreach (TaskModel task in tasks)
		{
			if (checkUserId && string.IsNullOrEmpty(task.userId))
			{
				task.userId = Utils.GetCurrentUserIdInt().ToString();
			}
			PrepareCorrectedTags(task);
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.SetProperties(new TaskBaseViewModel(task));
			vms.Add(taskBaseViewModel);
		}
		await App.Connection.UpdateAllAsync(tasks);
		foreach (TaskModel task2 in tasks)
		{
			await SaveTagCorrectionStatus(task2);
		}
		ProjectAndTaskIdsCache.OnTasksChanged(vms, checkMatched);
	}

	private static void PrepareCorrectedTags(TaskModel task)
	{
		if (task.TagsNeedSync)
		{
			task.tag = TagSerializer.ToJsonContent(task.tags?.ToList());
		}
	}

	internal static async Task SaveTagCorrectionStatus(TaskModel task, bool needSync = true, Func<string, Task> addModifyStatus = null)
	{
		if (task.TagsNeedSync)
		{
			if (needSync)
			{
				await (addModifyStatus ?? new Func<string, Task>(SyncStatusDao.AddModifySyncStatus))(task.id);
			}
			task.TagsNeedSync = false;
		}
	}

	public static async Task UpdateTask(TaskModel task, CheckMatchedType checkMatched = CheckMatchedType.None)
	{
		if (task != null)
		{
			if (string.IsNullOrEmpty(task.userId))
			{
				task.userId = Utils.GetCurrentUserIdInt().ToString();
			}
			PrepareCorrectedTags(task);
			await App.Connection.UpdateAsync(task);
			await SaveTagCorrectionStatus(task);
			TaskCache.SafeGetTaskViewModel(task).SetProperties(new TaskBaseViewModel(task));
		}
	}

	public static async Task<bool> TryUpdateTaskContentIfUnchanged(string taskId, string expectedContent, string newContent)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return false;
		}
		return await App.Connection.ExecuteAsync("UPDATE TaskModel SET content = ? WHERE id = ? AND userId = ? AND deleted = ? AND IFNULL(content, '') = ?", newContent, taskId, LocalSettings.Settings.LoginUserId, 0, expectedContent ?? string.Empty) > 0;
	}

	public static async Task<List<string>> GetRepeatEventIds()
	{
		string text = Utils.GetCurrentUserIdInt().ToString();
		return await BaseDao<TaskModel>.GetEntityIds("select Id as Id from calendarEventModel where repeatFlag is not null and repeatFlag !='' and deleted = '0' and userId ='" + text + "'");
	}

	public static async Task HandleTimeZoneChanged(TimeZoneInfo newTz, TimeZoneInfo oldTz)
	{
		List<TaskModel> obj = await App.Connection.Table<TaskModel>().ToListAsync();
		List<TaskModel> list = new List<TaskModel>();
		HashSet<string> floatTask = new HashSet<string>();
		foreach (TaskModel item in obj)
		{
			if (item == null)
			{
				continue;
			}
			if (item.Floating)
			{
				floatTask.Add(item.id);
			}
			else if (item.isAllDay != true && item.startDate.HasValue)
			{
				item.startDate = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(item.startDate.Value, DateTimeKind.Unspecified), oldTz, newTz);
				if (item.dueDate.HasValue)
				{
					item.dueDate = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(item.dueDate.Value, DateTimeKind.Unspecified), oldTz, newTz);
				}
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			await App.Connection.UpdateAllAsync(list);
		}
		List<TaskDetailItemModel> obj2 = await App.Connection.Table<TaskDetailItemModel>().ToListAsync();
		List<TaskDetailItemModel> list2 = new List<TaskDetailItemModel>();
		foreach (TaskDetailItemModel item2 in obj2)
		{
			if (item2.isAllDay.HasValue && !item2.isAllDay.Value && !floatTask.Contains(item2.TaskServerId) && item2.startDate.HasValue)
			{
				item2.startDate = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(item2.startDate.Value, DateTimeKind.Unspecified), oldTz, newTz);
				list2.Add(item2);
			}
		}
		if (list2.Count > 0)
		{
			await App.Connection.UpdateAllAsync(list2);
		}
	}

	public static async Task HandleDirtyData()
	{
		List<TaskId> list = await App.Connection.QueryAsync<TaskId>("select userId as UserId, id as Id from taskModel group by userid, id having count(1) > 1", Array.Empty<object>());
		if (!list.Any())
		{
			return;
		}
		foreach (TaskId id in list)
		{
			List<TaskModel> taskModel = await (from v in App.Connection.Table<TaskModel>()
				where v.id.Equals(id.Id) && v.userId == id.UserId
				select v).ToListAsync();
			if (taskModel.Any() && taskModel.Count > 1)
			{
				for (int i = 1; i < taskModel.Count; i++)
				{
					await App.Connection.DeleteAsync(taskModel[i]);
				}
			}
		}
	}

	public static async Task<List<TaskBaseViewModel>> GetSummaryTask(SummaryFilterModel filter)
	{
		if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
		{
			return null;
		}
		return TaskViewModelHelper.GetTaskInSummary(filter);
	}

	public static async Task<List<TaskModel>> GetUnSyncCompletedTasks()
	{
		List<SyncStatusModel> list = await SyncStatusDao.GetSyncStatusByType(4);
		if (list != null && list.Any())
		{
			return (await GetThinTasksInBatch(list.Select((SyncStatusModel t) => t.EntityId).ToList())).Where((TaskModel task) => task.status != 0 && task.deleted == 0 && !string.IsNullOrEmpty(task.repeatTaskId) && task.repeatTaskId != task.id).ToList();
		}
		return null;
	}

	public static async Task DeleteTaskInDb(string taskId)
	{
		TaskModel taskModel = await GetTaskById(taskId);
		if (taskModel != null)
		{
			await App.Connection.DeleteAsync(taskModel);
			TaskCache.DeleteTask(taskId);
			TaskSyncedJsonDao.DeleteTaskSyncedJsonPhysical(taskId);
		}
	}

	public static async Task ClearTrashTaskInDb()
	{
		List<TaskModel> list = await (from v in App.Connection.Table<TaskModel>()
			where v.deleted == 1 || (v.deleted == 2 && v.userId == LocalSettings.Settings.LoginUserId)
			select v).ToListAsync();
		if (list == null)
		{
			return;
		}
		foreach (TaskModel task in list)
		{
			await App.Connection.DeleteAsync(task);
			TaskCache.DeleteTask(task.id);
			TaskSyncedJsonDao.DeleteTaskSyncedJsonPhysical(task.id);
		}
	}

	public static async Task<List<TaskModel>> GetAllSubTasksById(string taskId, string projectId)
	{
		return await GetTasksByIds(TaskCache.GetAllSubTasksById(taskId, projectId)?.Select((TaskBaseViewModel m) => m.Id).ToList());
	}

	public static async Task<List<TaskModel>> GetTaskAndChildrenInBatch(List<string> taskIds, bool checkExist = true)
	{
		return await GetTasksByIds((from t in TaskCache.GetTaskAndChildrenInBatch(taskIds, checkExist)
			select t.Id).ToList());
	}

	public static async Task RemoveTaskParentId(string taskId)
	{
		await UpdateParent(taskId, "");
	}

	public static async Task RemoveTaskParentIdInBatch(List<string> taskIds, bool disableNotify = false)
	{
		await BatchUpdateParent(taskIds, "", null, canUndo: true, disableNotify ? "none" : "auto");
	}

	public static Node<DisplayItemModel> GetDisplayItemNode(string taskId, string projectId, bool withCompleted, bool buildByChildIds = false)
	{
		int taskLevel;
		return GetDisplayItemNode(taskId, projectId, withCompleted, buildByChildIds, out taskLevel);
	}

	public static Node<DisplayItemModel> GetDisplayItemNode(string taskId, string projectId, bool withCompleted, bool buildByChildIds, out int taskLevel)
	{
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = (buildByChildIds ? TaskCache.GetTaskNodeDict((from t in TaskCache.GetTasksInProject(projectId)
			where withCompleted || t.Status == 0
			select t).ToList(), buildByChildIds: true) : TaskCache.GetTaskTreeInProject(projectId, withCompleted));
		taskLevel = 0;
		if (!string.IsNullOrEmpty(taskId) && dictionary.TryGetValue(taskId, out var value))
		{
			taskLevel = (withCompleted ? value.GetLevel(0) : GetTaskLevel(taskId, projectId, buildByChildIds));
			return value.ToDisplayItemNode(taskId);
		}
		return null;
	}

	public static async Task UpdateParent(string taskId, string parentId, bool needSync = true)
	{
		TaskModel task = await GetThinTaskById(taskId);
		if (task == null || task.parentId == parentId || (string.IsNullOrEmpty(task.parentId) && string.IsNullOrEmpty(parentId)))
		{
			return;
		}
		if (!needSync)
		{
			TaskCache.SafeGetTaskViewModel(task).ParentId = parentId;
			return;
		}
		UtilLog.Info("SetTaskParent t " + taskId + ",old " + task.parentId + ",new " + parentId);
		string oldParent = task.parentId;
		task.parentId = parentId;
		task.modifiedTime = DateTime.Now;
		TaskBaseViewModel taskById = TaskCache.GetTaskById(parentId);
		if (taskById != null && task.columnId != taskById.ColumnId)
		{
			task.columnId = taskById.ColumnId;
			await SyncStatusDao.AddModifySyncStatus(taskId);
		}
		await SyncStatusDao.AddSetParentSyncStatus(taskId, oldParent);
		await TaskService.UpdateTaskParent(task);
		if (!string.IsNullOrEmpty(parentId))
		{
			await AddOrRemoveTaskChildIds(parentId, new List<string> { task.id }, addChild: true);
		}
		if (!string.IsNullOrEmpty(oldParent))
		{
			await AddOrRemoveTaskChildIds(oldParent, new List<string> { task.id }, addChild: false);
		}
	}

	public static async Task AddOrRemoveTaskChildIds(string parentId, List<string> ids, bool addChild)
	{
		TaskModel taskModel = await GetThinTaskById(parentId);
		if (taskModel == null)
		{
			return;
		}
		List<string> list = (string.IsNullOrEmpty(taskModel?.childrenString) ? new List<string>() : (JsonConvert.DeserializeObject<List<string>>(taskModel.childrenString) ?? new List<string>()));
		foreach (string id in ids)
		{
			if (list.Contains(id))
			{
				if (!addChild)
				{
					list.Remove(id);
				}
			}
			else if (addChild)
			{
				list.Add(id);
			}
		}
		taskModel.childrenString = ((list.Count > 0) ? JsonConvert.SerializeObject(list) : null);
		await TaskService.UpdateTaskParent(taskModel);
	}

	public static int GetTaskLevel(string taskId, string projectId, bool buildByChildIds = false)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return 0;
		}
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = (buildByChildIds ? TaskCache.GetTaskNodeDict(TaskCache.GetTasksInProject(projectId).ToList(), buildByChildIds: true) : TaskCache.GetTaskTreeInProject(projectId));
		if (dictionary.ContainsKey(taskId))
		{
			return dictionary[taskId].GetLevel(0);
		}
		return 0;
	}

	public static async Task BatchUpdateParent(List<string> ids, string parentId, string undoId = null, bool canUndo = true, string toastType = "auto")
	{
		TaskModel parent = await GetThinTaskById(parentId);
		bool needToast = false;
		string taskName = "";
		int count = 0;
		bool needToastLink = true;
		bool undo = TaskDragUndoModel.CheckUndoIdExist(undoId);
		foreach (string taskId in ids)
		{
			TaskModel task = await GetThinTaskById(taskId);
			if (task == null || task.parentId == parentId || (string.IsNullOrEmpty(task.parentId) && string.IsNullOrEmpty(parentId)))
			{
				continue;
			}
			if (!task.isCompleted)
			{
				needToastLink = false;
			}
			bool projectChanged = false;
			string oldParent = task.parentId;
			if (parent != null && task.projectId != parent.projectId)
			{
				await TaskService.MoveProject(taskId, parent.projectId);
				projectChanged = true;
				task = await GetThinTaskById(taskId);
				needToast = true;
				taskName = task.title;
				count++;
			}
			task.parentId = parentId;
			string text = ((parent == null) ? "" : parent.columnId);
			if (task.columnId != text)
			{
				task.columnId = text;
				await SyncStatusDao.AddModifySyncStatus(taskId);
			}
			task.modifiedTime = DateTime.Now;
			if (canUndo && (projectChanged || undo))
			{
				TaskDragUndoModel.AddDragModel(undo ? undoId : taskId, taskId, oldParent, parentId, null, null, null, 0L);
			}
			else
			{
				await SyncStatusDao.AddSetParentSyncStatus(taskId, oldParent);
				if (!string.IsNullOrEmpty(oldParent))
				{
					await AddOrRemoveTaskChildIds(oldParent, new List<string> { task.id }, addChild: false);
				}
			}
			await TaskService.UpdateTaskParent(task);
		}
		if (needToast)
		{
			Utils.GetToastWindow()?.ToastMoveProjectControl(parent.projectId, (count > 1) ? string.Empty : taskName);
		}
		else if ((needToastLink && toastType == "auto") || toastType == "show")
		{
			Utils.GetToastWindow()?.TryToastString(null, Utils.GetString("task_linked"));
		}
		if (!string.IsNullOrEmpty(parentId))
		{
			await AddOrRemoveTaskChildIds(parentId, ids, addChild: true);
		}
	}

	public static List<string> GetTreeTopIds(List<string> taskIds, string projectId)
	{
		if (taskIds == null)
		{
			return new List<string>();
		}
		Dictionary<string, Node<TaskBaseViewModel>> taskNodes = (string.IsNullOrEmpty(projectId) ? TaskCache.GetAllTaskTree() : TaskCache.GetTaskTreeInProject(projectId));
		HashSet<string> taskIdSet = new HashSet<string>(taskIds);
		foreach (string taskId in taskIds)
		{
			if (taskNodes.ContainsKey(taskId) && ExistParentId(taskNodes[taskId], taskIdSet))
			{
				taskIdSet.Remove(taskId);
			}
		}
		return taskIds.Where((string id) => taskIdSet.Contains(id) && taskNodes.ContainsKey(id)).ToList();
		static bool ExistParentId(Node<TaskBaseViewModel> node, HashSet<string> set)
		{
			if (node.HasParent)
			{
				if (!set.Contains(node.ParentId))
				{
					return ExistParentId(node.Parent, set);
				}
				return true;
			}
			return false;
		}
	}

	public static async Task CheckChildrenLevel(string taskId, int newLevel)
	{
		TaskModel taskModel = await GetThinTaskById(taskId);
		if (taskModel != null)
		{
			Dictionary<string, Node<TaskBaseViewModel>> taskTreeInProject = TaskCache.GetTaskTreeInProject(taskModel.projectId);
			if (taskTreeInProject.ContainsKey(taskId))
			{
				await CheckTaskLevel(taskTreeInProject[taskId], newLevel, taskId);
			}
		}
	}

	private static async Task CheckTaskLevel(Node<TaskBaseViewModel> taskNode, int level, string undoId)
	{
		if (level >= 4)
		{
			List<Node<TaskBaseViewModel>> children = taskNode.Children;
			if (children != null && children.Count > 0)
			{
				List<TaskBaseViewModel> childTasks = taskNode.GetAllChildrenValue();
				long nextTaskSortOrderInProject = ProjectSortOrderDao.GetNextTaskSortOrderInProject(taskNode.Value.ProjectId, taskNode.Value.SortOrder, taskNode.Value.ParentId);
				for (int i = 0; i < childTasks.Count; i++)
				{
					childTasks[i].SortOrder = Math.Min(taskNode.Value.SortOrder, nextTaskSortOrderInProject) + Math.Abs(nextTaskSortOrderInProject - taskNode.Value.SortOrder) * (i + 1) / (childTasks.Count + 1);
				}
				await BatchUpdateTasks(childTasks);
				await BatchUpdateParent(childTasks.Select((TaskBaseViewModel t) => t.Id).ToList(), taskNode.Value.ParentId, undoId);
				return;
			}
		}
		List<Node<TaskBaseViewModel>> children2 = taskNode.Children;
		if (children2 == null || children2.Count <= 0)
		{
			return;
		}
		foreach (Node<TaskBaseViewModel> child in taskNode.Children)
		{
			await CheckTaskLevel(child, level + 1, undoId);
		}
	}

	public static async Task<bool> UncheckAllCheckItems(string taskId)
	{
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
		bool changed = false;
		if (list != null && list.Count > 0)
		{
			foreach (TaskDetailItemModel item in list)
			{
				if (item.status != 0)
				{
					item.status = 0;
					changed = true;
				}
			}
			await TaskDetailItemDao.BatchUpdateChecklists(list);
		}
		return changed;
	}

	public static async void UpdateTaskAttachmentCount(string taskId, int count)
	{
		TaskModel taskModel = await GetThinTaskById(taskId);
		if (taskModel != null)
		{
			taskModel.attachmentCount = count;
			UpdateTask(taskModel);
		}
	}

	public static async Task FoldOrOpenTasks(List<string> taskIds, bool isOpen)
	{
		List<TaskModel> list = await GetThinTasksInBatch(taskIds);
		if (list == null)
		{
			return;
		}
		foreach (TaskModel item in list)
		{
			if (item.isOpen != isOpen)
			{
				item.isOpen = isOpen;
			}
		}
		await BatchUpdateTasks(list);
	}

	public static async Task<List<TaskModel>> GetAssignTasksInProject(string projectId, string assignee)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<TaskModel>()
			where v.projectId.Equals(projectId) && v.userId == userId && v.assignee == assignee
			select v).ToListAsync();
	}

	public static async Task<List<TaskModel>> GetTasksInProjectOrColumn(string projectId, string columnId, bool first)
	{
		return await GetTasksByIds((await TaskCache.GetTasksInProjectOrColumn(projectId, columnId, first))?.Select((TaskBaseViewModel t) => t.Id).ToList());
	}

	public static async Task<TaskModel> GetTaskByUid(long uid)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from v in App.Connection.Table<TaskModel>()
			where (long)v._Id == uid && v.userId == userId
			select v).FirstOrDefaultAsync();
	}

	public static async Task<List<TaskModel>> GetParentsByTaskIdAsync(string taskId)
	{
		TaskModel task = await GetThinTaskById(taskId);
		if (task == null || string.IsNullOrEmpty(task.parentId))
		{
			return null;
		}
		HashSet<string> parentIds = new HashSet<string>();
		List<TaskModel> result = new List<TaskModel>();
		string text = task.parentId;
		while (!string.IsNullOrEmpty(text) && !parentIds.Contains(text))
		{
			TaskModel taskModel = await GetThinTaskById(text);
			if (taskModel != null && taskModel.projectId == task.projectId)
			{
				result.Add(taskModel);
				parentIds.Add(taskModel.id);
				text = taskModel.parentId;
			}
			else
			{
				text = null;
			}
		}
		return result;
	}

	public static async void Test()
	{
		List<TaskModel> tasks = await GetAllTask();
		await Task.Delay(1000);
		List<Task> list = new List<Task>();
		for (int i = 0; i < 10; i++)
		{
			list.Add(GetAllTask());
			list.Add(BaseDao<TaskModel>.UpdateAllAsync(tasks));
		}
		await Task.WhenAll(list);
	}

	public static async Task<List<TaskModel>> GetUtcTasks()
	{
		return await (from t in App.Connection.Table<TaskModel>()
			where t.timeZone == "Etc/UTC"
			select t).ToListAsync();
	}

	public static async Task BatchUpdateProjectIdsInDb(List<string> taskIds, string projectId)
	{
		while (taskIds.Count > 0)
		{
			List<string> source;
			if (taskIds.Count > 100)
			{
				source = taskIds.Take(100).ToList();
				taskIds.RemoveRange(0, 100);
			}
			else
			{
				source = taskIds.ToList();
				taskIds.Clear();
			}
			string seed = "";
			seed = source.Aggregate(seed, (string current, string id) => current + "'" + id + "',");
			seed = seed.Substring(0, seed.Length - 1);
			string query = "Update TaskModel Set projectId = ? where id in (" + seed + ")";
			await App.Connection.ExecuteAsync(query, projectId);
		}
	}
}
