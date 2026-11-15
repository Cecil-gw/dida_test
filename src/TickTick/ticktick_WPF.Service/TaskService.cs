using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ical.Net;
using Ical.Net.DataTypes;
using KotlinModels;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service.SortOrder;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DateParser;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Kanban;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.QuickAdd;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.Time;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Service;

public static class TaskService
{
	private class MatrixProjectMoveCandidate
	{
		public string TaskId { get; set; }

		public ProjectModel Project { get; set; }

		public string ColumnId { get; set; }
	}

	private class MatrixProjectMoveSnapshot
	{
		public List<TaskModel> Tasks { get; set; } = new List<TaskModel>();

		public List<SyncStatusModel> SyncStatuses { get; set; } = new List<SyncStatusModel>();

		public List<CommentModel> Comments { get; set; } = new List<CommentModel>();
	}

	private sealed class AttachmentRecoveryLockEntry
	{
		public readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

		public int Users;
	}

	private static readonly object AttachmentRecoveryLocksGate = new object();

	private static readonly Dictionary<string, AttachmentRecoveryLockEntry> AttachmentRecoveryLocks = new Dictionary<string, AttachmentRecoveryLockEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly BlockingList<string> TaskPullingProjectIds = new BlockingList<string>();

	private static BlockingSet<string> _checkItemPulledIds = new BlockingSet<string>();

	private static HashSet<string> _pulledAttachmentTaskId = new HashSet<string>();

	public static async Task BatchAbandonTasks(List<string> selectedTaskIds)
	{
		await Task.WhenAll(from task in (await GetSelectedTasks(selectedTaskIds)).Where((TaskModel task) => task.kind != "NOTE" && task.status == 0).ToList()
			select SetTaskStatus(task, -1));
	}

	public static async Task<bool> BatchCompleteTasks(List<string> selectedTaskIds, bool ignoreRepeat = false, bool justComplete = false)
	{
		List<TaskModel> list = (await GetSelectedTasks(selectedTaskIds)).Where((TaskModel t) => t.kind != "NOTE").ToList();
		int uncompletedCount = list.Count((TaskModel task) => task.status == 0);
		int num = list.Count((TaskModel task) => task.status != 0);
		bool changed;
		bool flag;
		if (list.Count == uncompletedCount || (list.Count == num && !justComplete))
		{
			int newStatus = ((uncompletedCount > num) ? 2 : 0);
			changed = list.Count > 0;
			await Task.WhenAll(list.Select((TaskModel task) => SetTaskStatus(task, newStatus, playSound: false, ignoreRepeat)));
			flag = newStatus == 2 && uncompletedCount > 0;
		}
		else
		{
			changed = list.Count > 0;
			await Task.WhenAll(from task in list
				where task.status == 0
				select SetTaskStatus(task, 2, playSound: false, ignoreRepeat));
			flag = uncompletedCount > 0;
		}
		if (flag)
		{
			Utils.PlayCompletionSound();
		}
		return changed;
	}

	public static async Task BatchAddTag(List<string> taskIds, string tag)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		TagExtra tagExt = new TagExtra();
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			List<string> list2 = new List<string>();
			if (!string.IsNullOrEmpty(taskModel.tag))
			{
				list2 = TagSerializer.ToTags(taskModel.tag);
			}
			if (!list2.Contains(tag))
			{
				list2.Add(tag);
			}
			taskModel.tag = TagSerializer.ToJsonContent(list2);
			taskModel.tags = list2.ToArray();
			taskModel.modifiedTime = DateTime.Now;
			tagExt.AddTaskTagsPair(taskModel.id, list2);
		}
		await BatchSaveTask(list, CheckMatchedType.CheckTag);
		TaskChangeNotifier.NotifyTaskTagsChanged(tagExt);
	}

	public static async Task BatchSetTags(List<string> taskIds, TagSelectData tags)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		TagExtra tagExt = new TagExtra();
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			if (!string.IsNullOrEmpty(taskModel.tag))
			{
				taskModel.tags = TagSerializer.ToTags(taskModel.tag).ToArray();
			}
			List<string> list2 = new List<string>();
			list2.AddRange(tags.OmniSelectTags);
			if (taskModel.tags != null && taskModel.tags.Length != 0)
			{
				List<string> source = taskModel.tags.ToList();
				list2.AddRange(source.Where((string tag) => !tags.OmniSelectTags.Contains(tag) && tags.PartSelectTags.Contains(tag)));
			}
			taskModel.tag = TagSerializer.ToJsonContent(list2);
			taskModel.tags = list2.ToArray();
			taskModel.modifiedTime = DateTime.Now;
			tagExt.AddTaskTagsPair(taskModel.id, list2);
		}
		await BatchSaveTask(list, CheckMatchedType.CheckTag);
		TaskChangeNotifier.NotifyTaskTagsChanged(tagExt);
	}

	public static async Task BatchSetAssignee(List<string> taskIds, string assignee)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			taskModel.assignee = assignee;
			taskModel.modifiedTime = DateTime.Now;
		}
		await BatchSaveTask(list, CheckMatchedType.CheckAssign);
		TaskChangeNotifier.NotifyTaskAssigneeChanged(taskIds, assignee);
	}

	public static async Task<List<TaskModel>> BatchAddTasks(List<string> titles, string projectId, TimeData time = null, int priority = 0, List<string> tags = null, int status = 0, string assigneeId = "-1", string columnId = "", string parentId = null, bool isNote = false, DateTime? defaultDate = null, bool isPin = false, bool? addTop = null, long? targetSortOrder = null, string content = null)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		string timezone = Utils.GetLocalTimeZone();
		List<TaskModel> models = new List<TaskModel>();
		List<string> tagLabels = new List<string>();
		List<TaskReminderModel> reminders = new List<TaskReminderModel>();
		bool addInTop = false;
		if (addTop.HasValue)
		{
			addInTop = addTop.Value;
		}
		else
		{
			TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
			if (defaultSafely != null && defaultSafely.AddTo == 0)
			{
				addInTop = true;
			}
		}
		if (titles != null && titles.Any())
		{
			List<ProjectModel> projects = CacheManager.GetProjects()?.Where((ProjectModel p) => p.IsEnable()).ToList();
			Dictionary<string, int> priorityDict = new Dictionary<string, int>
			{
				{
					Utils.GetString("PriorityHigh"),
					5
				},
				{
					Utils.GetString("PriorityMedium"),
					3
				},
				{
					Utils.GetString("PriorityLow"),
					1
				},
				{
					Utils.GetString("PriorityNull"),
					0
				}
			};
			for (int i = 0; i < titles.Count; i++)
			{
				string text = projectId;
				long num = ((!addInTop) ? (268435456L * (long)(i + 1)) : ((long)(titles.Count + 1 - i) * 268435456L * -1));
				string title = titles[i]?.Replace("\n", " ").Replace("\r", " ").TrimEnd();
				if (string.IsNullOrEmpty(title))
				{
					continue;
				}
				if (projects != null && projects.Any())
				{
					List<string> tokens = projects.Select((ProjectModel p) => p.name).ToList();
					string existName = TaskTitleUtils.GetLastMatchName(ref title, tokens, "^", "~");
					if (existName != null)
					{
						text = projects.FirstOrDefault((ProjectModel p) => p.name == existName)?.id;
					}
				}
				if (string.IsNullOrEmpty(text))
				{
					text = TaskDefaultDao.GetDefaultSafely().ProjectId;
				}
				long num2 = targetSortOrder ?? ProjectSortOrderDao.GetNewTaskSortOrderInProject(text, parentId, addInTop);
				List<AvatarViewModel> projectAvatarsFromCache = AvatarHelper.GetProjectAvatarsFromCache(text);
				string text2 = (isNote ? null : TaskTitleUtils.GetLastMatchName(ref title, priorityDict.Keys.ToList(), "!", "！"));
				int priority2 = priority;
				if (text2 != null)
				{
					priority2 = priorityDict[text2];
				}
				string assignee = assigneeId;
				if (projectAvatarsFromCache != null && projectAvatarsFromCache.Any())
				{
					List<string> tokens2 = projectAvatarsFromCache.Select((AvatarViewModel u) => u.Name).ToList();
					string existName2 = TaskTitleUtils.GetLastMatchName(ref title, tokens2, "@", "@");
					if (existName2 != null)
					{
						AvatarViewModel avatarViewModel = projectAvatarsFromCache.FirstOrDefault((AvatarViewModel u) => u.Name == existName2);
						if (avatarViewModel != null)
						{
							assignee = avatarViewModel.UserId;
						}
					}
				}
				string text3 = title;
				List<string> list = ((tags == null) ? new List<string>() : tags.ToList());
				List<string> list2 = title.Split(' ').ToList();
				if (list2.Count > 0)
				{
					List<string> list3 = new List<string>();
					foreach (string item in list2)
					{
						if (item.Length > 1 && item.Length <= 65 && (item.StartsWith("#") || item.StartsWith("＃")))
						{
							string text4 = item.Substring(1);
							if (NameUtils.IsValidName(text4))
							{
								list.Add(text4);
							}
						}
						else
						{
							list3.Add(item);
						}
					}
					title = list3.Join(" ");
					if (!LocalSettings.Settings.KeepTagsInText)
					{
						text3 = title;
					}
				}
				List<string> timeText = new List<string>();
				TimeData timeData = time;
				if (LocalSettings.Settings.DateParsing)
				{
					IPaserDueDate paserDueDate = DateParser.Parse(title, null);
					if (paserDueDate != null)
					{
						timeText = paserDueDate.GetRecognizeStrings();
						TimeData timeData2 = paserDueDate.ToTimeData(!string.IsNullOrEmpty(title) && title.Contains(Utils.GetString("reminder")));
						if (timeData2 != null && !Utils.IsEmptyDate(timeData2.StartDate))
						{
							timeData = timeData2;
							if (time != null)
							{
								timeData.TimeZone = time.TimeZone;
							}
						}
					}
				}
				tagLabels.AddRange(list);
				TaskModel model = new TaskModel
				{
					id = Utils.GetGuid(),
					projectId = text,
					title = text3.Trim(),
					content = ((i != 0) ? string.Empty : content?.Trim()),
					userId = userId,
					timeZone = timezone,
					sortOrder = num2 + num,
					tag = ((list != null && list.Count > 0) ? TagSerializer.ToJsonContent(list) : null),
					tags = list?.ToArray(),
					priority = priority2,
					kind = (isNote ? "NOTE" : "TEXT"),
					status = status,
					columnId = columnId,
					parentId = parentId,
					createdTime = DateTime.Now,
					creator = LocalSettings.Settings.LoginUserId
				};
				TaskModel taskModel = model;
				taskModel.columnId = await ResolveAddOrMoveColumnId(text, model.columnId);
				if (!string.Equals(assignee, "-1"))
				{
					model.assignee = assignee;
				}
				if (status != 0)
				{
					model.completedTime = DateTime.Now;
				}
				if (timeData != null)
				{
					model.startDate = timeData.StartDate;
					model.dueDate = (isNote ? ((DateTime?)null) : timeData.DueDate);
					model.isAllDay = timeData.IsAllDay;
					model.repeatFrom = timeData.RepeatFrom;
					model.repeatFlag = timeData.RepeatFlag;
					model.isFloating = timeData.TimeZone?.IsFloat ?? false;
					model.timeZone = timeData.TimeZone?.TimeZoneName ?? TimeZoneData.LocalTimeZoneModel.TimeZoneName;
					if (timeData.Reminders != null && timeData.Reminders.Any())
					{
						reminders.AddRange(timeData.Reminders.Select((TaskReminderModel reminder) => new TaskReminderModel
						{
							id = Utils.GetGuid(),
							taskserverid = model.id,
							trigger = reminder.trigger
						}));
					}
					if (LocalSettings.Settings.RemoveTimeText && timeText.Any())
					{
						foreach (string item2 in timeText)
						{
							model.title = model.title.Replace(item2.TrimStart(), string.Empty);
						}
					}
				}
				models.Add(model);
			}
			if (isPin)
			{
				for (int num3 = models.Count - 1; num3 >= 0; num3--)
				{
					models[num3].pinnedTimeStamp = Utils.GetNowTimeStamp() - num3;
				}
			}
			await TagService.CheckTagsExist(tagLabels);
			await TaskDao.BatchInsertTasks(models);
			if (reminders.Any())
			{
				await TaskReminderDao.BatchAddTaskReminders(reminders);
			}
			List<string> ids = models.Select((TaskModel taskModel2) => taskModel2.id).ToList();
			await SyncStatusDao.BatchAddCreateTaskStatus(ids);
			if (!string.IsNullOrEmpty(parentId))
			{
				await TaskDao.AddOrRemoveTaskChildIds(parentId, ids, addChild: true);
				await SyncStatusDao.BatchAddTaskParentStatus(ids);
			}
			return models;
		}
		return null;
	}

	public static async Task BatchClearDate(List<string> taskIds)
	{
		List<TaskModel> tasks = await GetSelectedTasks(taskIds);
		for (int i = 0; i < tasks.Count; i++)
		{
			TaskModel task = tasks[i];
			task.isAllDay = true;
			task.startDate = null;
			task.dueDate = null;
			task.reminders = null;
			task.repeatFrom = string.Empty;
			task.repeatFlag = string.Empty;
			task.annoyingAlert = -1;
			task.remindTime = null;
			task.modifiedTime = DateTime.Now;
			task.isFloating = false;
			task.timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
			await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
		}
		TaskChangeNotifier.NotifyBatchDateChanged(taskIds);
		await BatchSaveTask(tasks, CheckMatchedType.CheckSmart);
	}

	public static async Task<bool> BatchDeleteTaskByIds(List<string> selectedTaskIds, bool showUndo = true, int status = 1, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null)
	{
		List<TaskModel> tasks = ((status != 1) ? (await GetSelectedTasks(selectedTaskIds)).Where((TaskModel t) => t.CheckEnable(defaultValue: true)).ToList() : (await TaskDao.GetTaskAndChildrenInBatch(selectedTaskIds)));
		return await BatchDeleteTasks(tasks, showUndo, status, null, deleteRecurrenceUndo);
	}

	public static bool ConfirmBatchDeleteTasks(int taskCount, int status = 1)
	{
		if (taskCount < 50 || status != 1)
		{
			return true;
		}
		return new CustomerDialog(Utils.GetString("DeleteTask"), string.Format(Utils.GetString("BatchDeleteConfirm"), taskCount.ToString()), Utils.GetString("Delete"), Utils.GetString("Cancel")).ShowDialog() == true;
	}

	public static async Task<bool> BatchDeleteTasks(List<TaskModel> tasks, bool showUndo = true, int status = 1, Grid undoGrid = null, List<TaskDeleteRecurrenceUndoEntity> deleteRecurrenceUndo = null, bool showConfirm = true)
	{
		if (tasks.Count == 0)
		{
			return true;
		}
		if (showConfirm && !ConfirmBatchDeleteTasks(tasks.Count, status))
		{
			return false;
		}
		List<string> ids = new List<string>();
		List<string> deleteForever = new List<string>();
		for (int i = 0; i < tasks.Count; i++)
		{
			TaskModel taskModel = tasks[i];
			if (IsEmptyTask(taskModel))
			{
				deleteForever.Add(taskModel.id);
				taskModel.deleted = 2;
			}
			else
			{
				if (taskModel.deleted == 2 && status == 1)
				{
					continue;
				}
				ids.Add(taskModel.id);
				taskModel.deleted = status;
			}
			taskModel.modifiedTime = DateTime.Now;
		}
		await TaskDao.BatchUpdateTasks(tasks, checkUserId: true, CheckMatchedType.All);
		if (status == 1)
		{
			if (!showUndo)
			{
				TaskChangeNotifier.NotifyTaskBatchDeletedChanged(ids);
				TaskChangeNotifier.NotifyTaskBatchDeletedChanged(deleteForever);
				await SyncStatusDao.BatchAddDeleteSyncStatus(ids);
				await SyncStatusDao.BatchAddDeleteForeverSyncStatus(deleteForever);
				SyncManager.TryDelaySync();
			}
			else
			{
				UndoToast undoToast = new UndoToast();
				if (ids.Count + deleteForever.Count == 1)
				{
					TaskModel taskModel2 = tasks.FirstOrDefault();
					if (taskModel2 != null)
					{
						undoToast.InitTaskUndo(taskModel2.id, taskModel2.title, taskModel2.deleted == 2, deleteRecurrenceUndo);
					}
				}
				else
				{
					undoToast.InitBatchTaskUndo(ids, deleteForever, deleteRecurrenceUndo);
				}
				if (undoGrid == null)
				{
					Utils.GetToastWindow()?.Toast(undoToast);
				}
				else
				{
					WindowToastHelper.ShowAndHideToast(undoGrid, undoToast);
				}
			}
			UtilLog.Info("TaskBatchService.BatchDeleteTasks : " + ids.Join(";"));
		}
		else
		{
			UtilLog.Info("TaskBatchService.BatchDeleteTasksForever : " + ids.Join(";"));
			await SyncStatusDao.BatchAddDeleteForeverSyncStatus(ids);
			await SyncStatusDao.BatchAddDeleteForeverSyncStatus(deleteForever);
			TaskChangeNotifier.NotifyTaskBatchDeletedChanged(ids);
			SyncManager.TryDelaySync();
		}
		return true;
	}

	public static async Task BatchDeleteTaskInColumn(string projectId, string columnId, string primaryColumnId)
	{
		await BatchDeleteTasks(await TaskDao.GetTasksInProjectOrColumn(projectId, columnId, primaryColumnId == columnId), showUndo: false);
	}

	public static async Task BatchMoveTaskInColumn(string projectId, string columnId, string primaryColumnId)
	{
		await BatchDeleteTasks(await TaskDao.GetTasksInProjectOrColumn(projectId, columnId, primaryColumnId == columnId), showUndo: false);
	}

	public static async Task BatchDeleteTaskInCustomSection(string projectId, string columnId, string primaryColumnId, bool filterStatus = true, bool ignoreDefaultColumnFallback = false)
	{
		bool first = !ignoreDefaultColumnFallback && primaryColumnId == columnId;
		List<TaskModel> list = await TaskDao.GetTasksInProjectOrColumn(projectId, columnId, first);
		List<TaskModel> notDeletedTasks = list.Where((TaskModel t) => t.pinnedTimeStamp > 0).ToList();
		if (filterStatus)
		{
			notDeletedTasks.AddRange(list.Where((TaskModel x) => x.status != 0));
		}
		HashSet<string> notDeletedTasksIds = new HashSet<string>(notDeletedTasks.Select((TaskModel t) => t.id));
		List<TaskModel> deletedTasks = new List<TaskModel>();
		foreach (TaskModel task in list)
		{
			if (notDeletedTasksIds.Contains(task.id))
			{
				if (!string.IsNullOrEmpty(task.parentId) && !notDeletedTasksIds.Contains(task.parentId))
				{
					task.parentId = string.Empty;
					await TaskDao.UpdateParent(task.id, string.Empty);
					UtilLog.Info("SetTaskParent empty " + task.id + " DeleteSection");
				}
				task.columnId = string.Empty;
			}
			else
			{
				deletedTasks.Add(task);
			}
		}
		await TaskDao.BatchUpdateTasks(notDeletedTasks.ToList(), checkUserId: true, CheckMatchedType.CheckSmart);
		await BatchDeleteTasks(deletedTasks, showUndo: false);
	}

	public static async Task BatchClearTrash(List<string> trashIds)
	{
		List<TaskModel> list = await GetSelectedTasks(trashIds);
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			taskModel.deleted = 2;
			taskModel.modifiedTime = DateTime.Now;
		}
		await TaskDao.BatchUpdateTasks(list);
		await SyncStatusDao.AddClearTrashSyncStatus();
		SyncManager.TryDelaySync();
	}

	public static async Task BatchMoveParentTaskProject(List<string> taskIds, ProjectModel project, bool keepAssignee = false, string columnId = null)
	{
		List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(taskIds);
		if (taskAndChildrenInBatch != null && taskAndChildrenInBatch.Count > 0)
		{
			await BatchMoveProject(taskAndChildrenInBatch.Select((TaskBaseViewModel t) => t.Id).ToList(), new MoveProjectArgs(null, project, keepAssignee)
			{
				Notify = false,
				ColumnId = columnId
			});
		}
	}

	public static async Task BatchRestoreProject(List<string> taskIds)
	{
		List<string> ids = new List<string>(taskIds);
		ids = TaskCache.GetAllTaskAndSubTaskIdsByIds(ids);
		List<TaskModel> tasks = await GetSelectedTasks(ids);
		IEnumerable<string> enumerable = tasks.Select((TaskModel t) => t.projectId).Distinct();
		Dictionary<string, ProjectModel> prjs = new Dictionary<string, ProjectModel>();
		foreach (string item in enumerable)
		{
			Dictionary<string, ProjectModel> dictionary = prjs;
			string key = item;
			dictionary[key] = await ProjectDao.GetProjectById(item);
		}
		string inbox = Utils.GetInboxId();
		foreach (TaskModel task in tasks)
		{
			task.modifiedTime = DateTime.Now;
			task.deleted = 0;
			task.assignee = "-1";
			string projectId = task.projectId;
			if (prjs.ContainsKey(task.projectId) && prjs[task.projectId] == null)
			{
				task.projectId = inbox;
			}
			await SyncStatusDao.AddMoveOrRestoreProjectStatus(task.id, projectId, isRestore: true);
			CommentDao.ChangeCommentProjectId(task, task.projectId);
		}
		await TaskDao.BatchUpdateTasks(tasks, checkUserId: true, CheckMatchedType.CheckProject);
		TaskChangeNotifier.NotifyTaskBatchDeletedChanged(ids);
		SyncManager.TryDelaySync();
		if (ids.Count == 1)
		{
			Utils.GetToastWindow()?.ToastMoveProjectControl(prjs.Values.FirstOrDefault()?.id, null, MoveToastType.Restore);
		}
		else
		{
			Utils.Toast(Utils.GetString("TaskHasBeenRestoreToOrigin"));
		}
	}

	public static async Task CheckColumnEmptyTask(string projectId)
	{
		List<TaskModel> tasks = (await TaskDao.GetTasksInProjectAsync(projectId))?.Where((TaskModel t) => string.IsNullOrEmpty(t.columnId)).ToList();
		if (tasks == null || !tasks.Any())
		{
			return;
		}
		string defaultColumnId = await ColumnDao.GetProjectDefaultColumnId(projectId);
		UtilLog.Info("ColumnEmptyTasksCount projectId " + projectId + " count " + tasks.Count);
		string lastDefaultColumnId = TaskCache.GetLastDefaultColumnId(projectId);
		if (string.IsNullOrEmpty(lastDefaultColumnId))
		{
			if (!string.IsNullOrEmpty(defaultColumnId))
			{
				TaskCache.SetLastDefaultColumnId(projectId, defaultColumnId);
			}
		}
		else
		{
			if (lastDefaultColumnId == defaultColumnId || string.IsNullOrEmpty(lastDefaultColumnId))
			{
				return;
			}
			foreach (TaskModel task in tasks)
			{
				task.columnId = lastDefaultColumnId;
				task.modifiedTime = DateTime.Now;
				await UpdateTaskColumnId(task);
				await SyncStatusDao.AddModifySyncStatus(task.id);
			}
			if (!string.IsNullOrEmpty(defaultColumnId))
			{
				TaskCache.SetLastDefaultColumnId(projectId, defaultColumnId);
			}
		}
	}

	public static async Task<bool> BatchMoveProject(List<string> taskIds, MoveProjectArgs args, bool checkLimit = true)
	{
		List<string> ids = ExpandProjectMoveTaskIds(taskIds);
		MoveProjectArgs moveProjectArgs = args;
		moveProjectArgs.ColumnId = await ResolveAddOrMoveColumnId(args.Project.id, args.ColumnId);
		List<TaskModel> tasks = await GetSelectedTasks(ids);
		if (tasks.Count == 0)
		{
			return false;
		}
		int num = tasks.Count((TaskModel taskModel) => taskModel.projectId != args.Project.id && taskModel.status == 0);
		if (checkLimit && num > 0 && await ProChecker.CheckTaskLimit(args.Project.id, num))
		{
			return false;
		}
		long newSortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(args.Project.id);
		TaskDefaultDao.GetDefaultSafely();
		tasks.Sort((TaskModel a, TaskModel b) => a.sortOrder.CompareTo(b.sortOrder));
		bool changed = false;
		long step = 268435456L / (long)(2 * tasks.Count);
		for (int i = 0; i < tasks.Count; i++)
		{
			TaskModel task = tasks[i];
			bool flag = task.projectId == args.Project.id;
			bool flag2 = task.columnId == args.ColumnId || string.IsNullOrEmpty(args.ColumnId);
			if (flag && flag2)
			{
				continue;
			}
			changed = true;
			string projectId = task.projectId;
			task.projectId = args.Project.id;
			task.columnId = args.ColumnId;
			task.modifiedTime = DateTime.Now;
			task.deleted = 0;
			string assignee = task.assignee;
			long sortOrder = task.sortOrder;
			if (!args.KeepAssignee)
			{
				task.assignee = "-1";
			}
			task.sortOrder = newSortOrder - (tasks.Count - i) * step;
			if (!flag)
			{
				if (string.IsNullOrEmpty(args.UndoId))
				{
					await SyncStatusDao.AddMoveOrRestoreProjectStatus(task.id, projectId);
					await SyncStatusDao.AddModifySyncStatus(task.id);
				}
				else
				{
					TaskDragUndoModel.AddDragModel(args.UndoId, task.id, null, null, projectId, args.Project.id, assignee, sortOrder);
				}
				CommentDao.ChangeCommentProjectId(task, args.Project.id);
				await UpdateTaskOnProjectChanged(task, CheckMatchedType.None);
			}
			else
			{
				await UpdateTaskColumnId(task);
			}
		}
		if (changed)
		{
			ProjectAndTaskIdsCache.OnTasksChanged(TaskCache.GetTasksByIds(ids), CheckMatchedType.CheckProject);
			if (args.Notify)
			{
				TaskChangeNotifier.NotifyTaskBatchChanged(ids);
				SyncManager.TryDelaySync();
			}
		}
		return true;
	}

	private static List<string> ExpandProjectMoveTaskIds(IEnumerable<string> taskIds)
	{
		if (taskIds == null)
		{
			return new List<string>();
		}
		List<string> list = taskIds.Where((string id) => !string.IsNullOrEmpty(id)).Distinct().ToList();
		if (list.Count == 0)
		{
			return list;
		}
		Dictionary<string, List<TaskBaseViewModel>> dictionary = (from task in (from task in TaskCache.GetAllTask()
				where task != null && task.Deleted == 0
				select task).ToList()
			where !string.IsNullOrEmpty(task.ParentId)
			group task by task.ParentId).ToDictionary((IGrouping<string, TaskBaseViewModel> group) => group.Key, (IGrouping<string, TaskBaseViewModel> group) => group.ToList());
		HashSet<string> hashSet = new HashSet<string>(list);
		Queue<string> queue = new Queue<string>(list);
		while (queue.Count > 0)
		{
			string key = queue.Dequeue();
			if (!dictionary.TryGetValue(key, out var value))
			{
				continue;
			}
			foreach (TaskBaseViewModel item in value)
			{
				if (!string.IsNullOrEmpty(item.Id) && hashSet.Add(item.Id))
				{
					queue.Enqueue(item.Id);
				}
			}
		}
		return hashSet.ToList();
	}

	private static async Task BatchSaveTask(List<TaskModel> tasks, CheckMatchedType checkType = CheckMatchedType.None)
	{
		await TaskDao.BatchUpdateTasks(tasks, checkUserId: true, checkType);
		await SyncStatusDao.BatchAddModifySyncStatus(tasks.Select((TaskModel t) => t.id));
	}

	private static string GetMatrixEffectiveAssignee(NormalFilterViewModel filter)
	{
		return filter?.Assignees?.FirstOrDefault();
	}

	private static bool CheckMatrixAssigneeRule(NormalFilterViewModel nextFilter, IToastShowWindow toastShowWindow)
	{
		if (nextFilter == null || nextFilter.Assignees?.Any((string assignee) => !string.IsNullOrEmpty(assignee) && assignee != "noassignee" && assignee != "me" && assignee != LocalSettings.Settings.LoginUserId) != true)
		{
			return true;
		}
		toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
		return false;
	}

	private static async Task<MatrixProjectMoveSnapshot> SnapshotMatrixProjectMoveTasks(List<MatrixProjectMoveCandidate> projectMoves)
	{
		Dictionary<string, TaskModel> snapshots = new Dictionary<string, TaskModel>();
		foreach (MatrixProjectMoveCandidate item in projectMoves ?? new List<MatrixProjectMoveCandidate>())
		{
			TaskModel taskModel = await TaskDao.GetTaskById(item.TaskId);
			if (taskModel == null)
			{
				return null;
			}
			snapshots[taskModel.id] = taskModel.Clone();
			foreach (TaskModel item2 in (await GetAllSubTasksByIdAsync(taskModel.id, taskModel.projectId)) ?? new List<TaskModel>())
			{
				snapshots[item2.id] = item2.Clone();
			}
		}
		List<string> affectedIds = snapshots.Keys.ToList();
		MatrixProjectMoveSnapshot result = new MatrixProjectMoveSnapshot
		{
			Tasks = snapshots.Values.ToList()
		};
		if (!affectedIds.Any())
		{
			return result;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		result.SyncStatuses = (await (from status in App.Connection.Table<SyncStatusModel>()
			where affectedIds.Contains(status.EntityId) && status.UserId == userId
			select status).ToListAsync()).Select(CloneSyncStatus).ToList();
		foreach (string item3 in affectedIds)
		{
			List<CommentModel> list = await CommentDao.GetCommentsByTaskSidWithDeleted(item3);
			result.Comments.AddRange((list ?? new List<CommentModel>()).Select(CloneCommentProjectSnapshot));
		}
		return result;
	}

	private static async Task RollbackMatrixProjectMovesAsync(MatrixProjectMoveSnapshot snapshot, List<string> notifyIds)
	{
		if (snapshot == null || snapshot.Tasks.Count == 0)
		{
			return;
		}
		try
		{
			List<string> affectedIds = snapshot.Tasks.Select((TaskModel t) => t.id).Distinct().ToList();
			await TaskDao.BatchUpdateTasks(snapshot.Tasks.Select((TaskModel t) => t.Clone()).ToList(), checkUserId: true, CheckMatchedType.All);
			await RollbackMatrixMoveCommentsAsync(snapshot.Comments, affectedIds);
			await RollbackMatrixMoveSyncStatusesAsync(snapshot.SyncStatuses, affectedIds);
			TaskChangeNotifier.NotifyTaskBatchChanged(notifyIds);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("RollbackMatrixProjectMovesAsync " + ex.Message);
		}
	}

	private static async Task RollbackMatrixMoveSyncStatusesAsync(List<SyncStatusModel> snapshots, List<string> affectedIds)
	{
		if (affectedIds == null || affectedIds.Count == 0)
		{
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (SyncStatusModel item in await (from status in App.Connection.Table<SyncStatusModel>()
			where affectedIds.Contains(status.EntityId) && status.UserId == userId
			select status).ToListAsync())
		{
			await App.Connection.DeleteAsync(item);
		}
		List<SyncStatusModel> list = (snapshots ?? new List<SyncStatusModel>()).Select(CloneSyncStatus).ToList();
		if (list.Any())
		{
			await App.Connection.InsertAllAsync(list);
		}
	}

	private static async Task RollbackMatrixMoveCommentsAsync(List<CommentModel> snapshots, List<string> affectedIds)
	{
		if (affectedIds == null || affectedIds.Count == 0 || snapshots == null || snapshots.Count == 0)
		{
			return;
		}
		Dictionary<int, string> snapshotMap = (from comment in snapshots
			group comment by comment._Id).ToDictionary((IGrouping<int, CommentModel> group) => group.Key, (IGrouping<int, CommentModel> group) => group.First().projectSid);
		List<CommentModel> changed = new List<CommentModel>();
		foreach (string affectedId in affectedIds)
		{
			foreach (CommentModel item in (await CommentDao.GetCommentsByTaskSidWithDeleted(affectedId)) ?? new List<CommentModel>())
			{
				if (snapshotMap.TryGetValue(item._Id, out var value) && item.projectSid != value)
				{
					item.projectSid = value;
					changed.Add(item);
				}
			}
		}
		if (changed.Any())
		{
			await App.Connection.UpdateAllAsync(changed);
		}
	}

	private static SyncStatusModel CloneSyncStatus(SyncStatusModel status)
	{
		return new SyncStatusModel
		{
			_Id = status._Id,
			UserId = status.UserId,
			EntityId = status.EntityId,
			MoveFromId = status.MoveFromId,
			OldParentId = status.OldParentId,
			Type = status.Type,
			ModifyPoint = status.ModifyPoint
		};
	}

	private static CommentModel CloneCommentProjectSnapshot(CommentModel comment)
	{
		return new CommentModel
		{
			_Id = comment._Id,
			id = comment.id,
			taskSid = comment.taskSid,
			projectSid = comment.projectSid
		};
	}

	private static async Task<List<MatrixProjectMoveCandidate>> PrepareMatrixProjectMoves(List<TaskBaseViewModel> tasksViewModels, HashSet<string> topIds, Dictionary<string, ProjectModel> targetProjects, IToastShowWindow toastShowWindow)
	{
		List<MatrixProjectMoveCandidate> moves = new List<MatrixProjectMoveCandidate>();
		Dictionary<string, int> targetCounts = new Dictionary<string, int>();
		foreach (TaskBaseViewModel vm in tasksViewModels)
		{
			if (topIds.Contains(vm.Id) && targetProjects.TryGetValue(vm.Id, out var targetProject) && targetProject != null && !(vm.ProjectId == targetProject.id))
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(vm.Id);
				if (taskModel == null)
				{
					toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
					return null;
				}
				int num = ((await GetAllSubTasksByIdAsync(vm.Id, taskModel.projectId))?.Count ?? 0) + 1;
				if (!targetCounts.ContainsKey(targetProject.id))
				{
					targetCounts[targetProject.id] = 0;
				}
				targetCounts[targetProject.id] += num;
				List<MatrixProjectMoveCandidate> list = moves;
				MatrixProjectMoveCandidate matrixProjectMoveCandidate = new MatrixProjectMoveCandidate
				{
					TaskId = vm.Id,
					Project = targetProject
				};
				MatrixProjectMoveCandidate matrixProjectMoveCandidate2 = matrixProjectMoveCandidate;
				matrixProjectMoveCandidate2.ColumnId = await ResolveAddOrMoveColumnId(targetProject.id);
				list.Add(matrixProjectMoveCandidate);
				targetProject = null;
			}
		}
		foreach (KeyValuePair<string, int> item in targetCounts)
		{
			if (await ProChecker.CheckTaskLimit(item.Key, item.Value))
			{
				return null;
			}
		}
		return moves;
	}

	private static ProjectModel ResolveMatrixDropProject(TaskBaseViewModel task, NormalFilterViewModel nextFilter, FilterTaskDefault nextDefault)
	{
		ProjectModel projectById = CacheManager.GetProjectById(task.ProjectId);
		string matrixEffectiveAssignee = GetMatrixEffectiveAssignee(nextFilter);
		string text = ((matrixEffectiveAssignee == "me") ? LocalSettings.Settings.LoginUserId : matrixEffectiveAssignee);
		ProjectModel projectModel = projectById;
		if (HasProjectScope(nextFilter) && !ProjectMatchesScope(projectById, nextFilter))
		{
			projectModel = nextDefault.ProjectModel;
		}
		if (!string.IsNullOrEmpty(text) && text != "noassignee" && !ProjectSupportsAssignee(projectModel, text))
		{
			projectModel = GetFirstAssignableProject(nextFilter, text, projectById);
		}
		return projectModel;
	}

	private static bool ValidateMatrixAssigneeProject(string assignee, ProjectModel targetProject, NormalFilterViewModel nextFilter)
	{
		if (targetProject == null)
		{
			return false;
		}
		string matrixEffectiveAssignee = GetMatrixEffectiveAssignee(nextFilter);
		if (matrixEffectiveAssignee == "me")
		{
			return ProjectSupportsAssignee(targetProject, LocalSettings.Settings.LoginUserId);
		}
		if (matrixEffectiveAssignee == "noassignee")
		{
			return true;
		}
		if (!string.IsNullOrEmpty(matrixEffectiveAssignee))
		{
			return ProjectSupportsAssignee(targetProject, matrixEffectiveAssignee);
		}
		if (IsUnassigned(assignee) || assignee == LocalSettings.Settings.LoginUserId)
		{
			return true;
		}
		return ProjectSupportsAssignee(targetProject, assignee);
	}

	private static void ApplyMatrixAssignee(TaskModel task, NormalFilterViewModel nextFilter)
	{
		string matrixEffectiveAssignee = GetMatrixEffectiveAssignee(nextFilter);
		if (!string.IsNullOrEmpty(matrixEffectiveAssignee))
		{
			switch (matrixEffectiveAssignee)
			{
			case "noassignee":
				task.assignee = "-1";
				break;
			case "me":
				task.assignee = LocalSettings.Settings.LoginUserId;
				break;
			default:
				task.assignee = matrixEffectiveAssignee;
				break;
			case "other":
				break;
			case "anyone":
				break;
			}
		}
	}

	private static bool HasProjectScope(NormalFilterViewModel filter)
	{
		List<string> projects = filter.Projects;
		if (projects == null || !projects.Any())
		{
			return filter.Groups?.Any() ?? false;
		}
		return true;
	}

	private static bool ProjectMatchesScope(ProjectModel project, NormalFilterViewModel filter)
	{
		if (project == null || !HasProjectScope(filter))
		{
			return true;
		}
		List<string> projects = filter.Projects;
		if (projects == null || !projects.Any(CalendarUtils.IsAllListId))
		{
			List<string> projects2 = filter.Projects;
			if (projects2 == null || !projects2.Contains(project.id))
			{
				return filter.Groups?.Contains(project.groupId) ?? false;
			}
		}
		return true;
	}

	private static ProjectModel GetFirstAssignableProject(NormalFilterViewModel filter, string assignee, ProjectModel currentProject)
	{
		if (ProjectMatchesScope(currentProject, filter) && ProjectSupportsAssignee(currentProject, assignee))
		{
			return currentProject;
		}
		return GetMatrixProjectScope(filter).FirstOrDefault((ProjectModel project) => ProjectSupportsAssignee(project, assignee));
	}

	private static List<ProjectModel> GetMatrixProjectScope(NormalFilterViewModel filter)
	{
		List<ProjectModel> list = (from project in CacheManager.GetProjects()
			where project.IsEnable()
			orderby project.sortOrder
			select project).ToList();
		if (HasProjectScope(filter))
		{
			List<string> projects = filter.Projects;
			if (projects == null || !projects.Any(CalendarUtils.IsAllListId))
			{
				List<ProjectModel> list2 = new List<ProjectModel>();
				if (filter.Projects != null)
				{
					list2.AddRange(list.Where((ProjectModel project) => filter.Projects.Contains(project.id)));
				}
				if (filter.Groups != null)
				{
					list2.AddRange(list.Where((ProjectModel project) => filter.Groups.Contains(project.groupId)));
				}
				return (from project in list2
					group project by project.id into @group
					select @group.First() into project
					orderby project.sortOrder
					select project).ToList();
			}
		}
		return list;
	}

	private static bool ProjectSupportsAssignee(ProjectModel project, string assignee)
	{
		if (project == null || IsUnassigned(assignee))
		{
			return true;
		}
		if (project.IsShareList())
		{
			return AvatarHelper.GetUserByIdAndProjectId(assignee, project.id) != null;
		}
		return false;
	}

	private static bool IsUnassigned(string assignee)
	{
		if (!string.IsNullOrEmpty(assignee) && !(assignee == "-1"))
		{
			return assignee == "noassignee";
		}
		return true;
	}

	internal static async Task<bool> BatchUpdateTaskQuadrantProperties(List<string> taskIds, string previewRule, string nextRule, IToastShowWindow toastShowWindow)
	{
		List<TaskBaseViewModel> tasksViewModels = TaskCache.GetTasksByIds(taskIds);
		NormalFilterViewModel nextFilter = ticktick_WPF.Util.Filter.Parser.ToNormalModel(nextRule);
		if (!CheckMatrixAssigneeRule(nextFilter, toastShowWindow))
		{
			return false;
		}
		List<string> matchedTaskIds = (from t in GetTaskMatchedNormalFilter(nextFilter, tasksViewModels, inAll: true)
			select t.Id).ToList();
		tasksViewModels.RemoveAll((TaskBaseViewModel t) => matchedTaskIds.Contains(t.Id));
		if (tasksViewModels.Count == 0 && matchedTaskIds.Count > 0)
		{
			if (!string.IsNullOrEmpty(previewRule))
			{
				toastShowWindow?.TryToastString(null, Utils.GetString("TaskExisted"));
			}
			return false;
		}
		NormalFilterViewModel previewFilter = ticktick_WPF.Util.Filter.Parser.ToNormalModel(previewRule);
		FilterTaskDefault filterTaskDefault = new FilterTaskDefault();
		FilterViewModel.GetDefaultInNormal(previewFilter, filterTaskDefault);
		FilterTaskDefault nextDefault = new FilterTaskDefault();
		FilterViewModel.GetDefaultInNormal(nextFilter, nextDefault);
		if (nextFilter.Priorities.Any() && ((nextFilter.TaskTypes.Count == 1 && nextFilter.TaskTypes[0].ToLower() == "note") || (tasksViewModels.Any((TaskBaseViewModel t) => t.IsNote) && nextFilter.TaskTypes.Count != 1)))
		{
			toastShowWindow?.TryToastString(null, Utils.GetString("NoteMoveFailed"));
			return false;
		}
		if (nextFilter.TaskTypes.Count == 1 && nextFilter.TaskTypes[0].ToLower() == "note" && tasksViewModels.Any((TaskBaseViewModel t) => t.Status != 0 || TaskCache.IsParentTask(t.Id) || TaskCache.IsChildTask(t.ParentId, t.ProjectId)))
		{
			toastShowWindow?.TryToastString(null, Utils.GetString("CannotConvertMultiLevelToNote"));
			return false;
		}
		List<string> treeTopIds = TaskDao.GetTreeTopIds(taskIds, null);
		Dictionary<string, ProjectModel> dictionary = new Dictionary<string, ProjectModel>();
		foreach (TaskBaseViewModel item in tasksViewModels)
		{
			ProjectModel projectModel = ResolveMatrixDropProject(item, nextFilter, nextDefault);
			if (projectModel == null)
			{
				toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
				return false;
			}
			if (!ValidateMatrixAssigneeProject(item.GetAssignee(), projectModel, nextFilter))
			{
				toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
				return false;
			}
			dictionary[item.Id] = projectModel;
		}
		List<MatrixProjectMoveCandidate> projectMoves = await PrepareMatrixProjectMoves(tasksViewModels, new HashSet<string>(treeTopIds), dictionary, toastShowWindow);
		if (projectMoves == null)
		{
			return false;
		}
		MatrixProjectMoveSnapshot moveSnapshots = await SnapshotMatrixProjectMoveTasks(projectMoves);
		if (moveSnapshots == null)
		{
			toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
			return false;
		}
		try
		{
			foreach (MatrixProjectMoveCandidate item2 in projectMoves)
			{
				if (!(await TryMoveProject(new MoveProjectArgs(item2.TaskId, item2.Project, keepAssignee: true)
				{
					ColumnId = item2.ColumnId
				}, checkLimit: false)))
				{
					await RollbackMatrixProjectMovesAsync(moveSnapshots, taskIds);
					toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
					return false;
				}
			}
			List<TaskModel> tasks = new List<TaskModel>();
			string toastSwitchKind = string.Empty;
			foreach (TaskBaseViewModel vm in tasksViewModels)
			{
				TaskModel taskModel = await TaskDao.GetTaskById(vm.Id);
				if (taskModel == null)
				{
					await RollbackMatrixProjectMovesAsync(moveSnapshots, taskIds);
					toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
					return false;
				}
				tasks.Add(taskModel);
				if (nextFilter.DueDates.Any((string dateRule) => dateRule != "recurring"))
				{
					List<FilterDatePair> filterDatePairs = FilterUtils.GetFilterDatePairs(nextFilter.DueDates);
					if (!TaskUtils.CheckDate(vm, filterDatePairs))
					{
						TimeData timeData = new TimeData();
						if (nextDefault.DefaultDate.HasValue)
						{
							timeData = taskModel.GetTimeData();
							ChangeStartDate(ref timeData, nextDefault.DefaultDate.Value);
						}
						taskModel.startDate = timeData.StartDate;
						taskModel.dueDate = timeData.DueDate;
						taskModel.repeatFlag = timeData.RepeatFlag;
						taskModel.repeatFrom = timeData.RepeatFrom;
						taskModel.isAllDay = timeData.IsAllDay;
					}
				}
				if (nextFilter.Priorities.Any() && !nextFilter.Priorities.Contains(taskModel.priority))
				{
					taskModel.priority = nextDefault.Priority ?? TaskDefaultDao.GetDefaultSafely().Priority;
				}
				if (nextFilter.TaskTypes.Count == 1)
				{
					string text = ((nextFilter.TaskTypes[0].ToLower() == "note") ? "NOTE" : "TEXT");
					if (taskModel.kind == "NOTE" != (text == "NOTE"))
					{
						taskModel.kind = text;
						if (string.IsNullOrEmpty(toastSwitchKind))
						{
							toastSwitchKind = Utils.GetString((text == "NOTE") ? "ConvertedtoNote" : "ConvertedtoTask");
						}
					}
				}
				if (nextFilter.Tags.Any())
				{
					List<string> list = TagSerializer.ToTags(taskModel.tag);
					if (previewFilter.Tags.Any())
					{
						if (previewFilter.Tags.Contains("*withtags"))
						{
							list.Clear();
						}
						else
						{
							foreach (string tag in previewFilter.Tags)
							{
								list.RemoveAll((string t) => string.Equals(t, tag, StringComparison.CurrentCultureIgnoreCase));
							}
						}
					}
					if (!nextFilter.Tags.Contains("*withtags") && nextFilter.Tags.Contains("!tag"))
					{
						list.Clear();
					}
					foreach (string defaultTag in nextDefault.DefaultTags)
					{
						string tagName = defaultTag;
						if (tagName.StartsWith("#"))
						{
							tagName = tagName.Substring(1);
						}
						if (!list.Any((string t) => string.Equals(t, tagName, StringComparison.CurrentCultureIgnoreCase)))
						{
							list.Add(tagName);
						}
					}
					taskModel.tag = TagSerializer.ToJsonContent(list);
				}
				ApplyMatrixAssignee(taskModel, nextFilter);
			}
			if (!string.IsNullOrEmpty(toastSwitchKind))
			{
				toastShowWindow?.TryToastString(null, toastSwitchKind);
			}
			List<string> changedIds = tasks.Select((TaskModel t) => t.id).ToList();
			await TaskDao.BatchUpdateTasks(tasks, checkUserId: true, CheckMatchedType.All);
			await SyncStatusDao.BatchAddModifySyncStatus(changedIds);
			TaskChangeNotifier.NotifyTaskBatchChanged(changedIds);
			SyncManager.TryDelaySync();
			return true;
		}
		catch (Exception ex)
		{
			await RollbackMatrixProjectMovesAsync(moveSnapshots, taskIds);
			UtilLog.Warn("BatchUpdateTaskQuadrantProperties " + ex.Message);
			toastShowWindow?.TryToastString(null, Utils.GetString("OptionNotSupport"));
			return false;
		}
	}

	public static async Task BatchSkipRecurrence(List<string> taskIds)
	{
		if (taskIds != null && taskIds.Any())
		{
			foreach (string taskId in taskIds)
			{
				await SkipCurrentRecurrence(taskId, notify: false);
			}
		}
		TaskChangeNotifier.NotifyBatchDateChanged(taskIds);
	}

	public static async Task BatchSetStartDate(List<string> selectedTaskIds, DateTime date)
	{
		List<TaskModel> taskModels = new List<TaskModel>();
		for (int i = 0; i < selectedTaskIds.Count; i++)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(selectedTaskIds[i]);
			bool noDateTask = taskModel == null || !taskModel.startDate.HasValue;
			if (taskModel != null)
			{
				taskModel = await ModifyTaskDate(taskModel, date);
				taskModels.Add(taskModel);
				if (noDateTask && taskModel.isAllDay.HasValue && taskModel.isAllDay.Value)
				{
					taskModel.reminders = TimeData.GetDefaultAllDayReminders().ToArray();
					taskModel.exDates = string.Empty;
					await SaveTaskReminders(taskModel);
				}
			}
		}
		await BatchSaveTask(taskModels, CheckMatchedType.CheckSmart);
		TaskChangeNotifier.NotifyBatchDateChanged(selectedTaskIds);
		ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(new TimeData
		{
			StartDate = date
		});
	}

	public static async Task UndoBatchDeletedTasks(List<string> taskIds)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		if (list != null && list.Any())
		{
			list.ForEach(delegate(TaskModel task)
			{
				task.deleted = 0;
				task.modifiedTime = DateTime.Now;
			});
			UtilLog.Info("TaskBatchService.UndoBatchDeletedTasks : " + taskIds.Count);
			await TaskDao.BatchUpdateTasks(list, checkUserId: true, CheckMatchedType.All);
			await SyncStatusDao.BatchRemoveDeleteSyncStatus(taskIds);
			TaskChangeNotifier.NotifyBatchDeleteUndo(taskIds);
		}
	}

	public static async Task<bool> BatchSetPriority(List<string> taskIds, int priority)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		if (list.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			if (taskModel.kind != "NOTE")
			{
				taskModel.priority = priority;
				taskModel.modifiedTime = DateTime.Now;
			}
		}
		await BatchSaveTask(list, CheckMatchedType.OnlyFilter);
		TaskChangeNotifier.NotifyTaskPriorityChanged(null, priority, taskIds);
		return true;
	}

	public static async Task<int> BatchSetCheckItemToToday(List<TaskDetailItemModel> items)
	{
		if (items == null || items.Count <= 0)
		{
			return 0;
		}
		List<string> taskList = new List<string>();
		DateTime today = DateTime.Today;
		List<TaskDetailItemModel> list = new List<TaskDetailItemModel>();
		foreach (TaskDetailItemModel item in items)
		{
			if (item != null)
			{
				TaskDetailItemModel taskDetailItemModel = item.Copy();
				if (taskDetailItemModel.startDate.HasValue)
				{
					taskDetailItemModel.startDate = new DateTime(today.Year, today.Month, today.Day, taskDetailItemModel.startDate.Value.Hour, taskDetailItemModel.startDate.Value.Minute, taskDetailItemModel.startDate.Value.Second);
					list.Add(taskDetailItemModel);
					taskList.Add(taskDetailItemModel.TaskServerId);
				}
			}
		}
		if (list.Any())
		{
			await TaskDetailItemDao.BatchUpdateChecklists(list);
		}
		if (taskList.Any())
		{
			await SyncStatusDao.BatchAddModifySyncStatus(taskList.Distinct());
		}
		return items.Count;
	}

	public static async Task BatchSetColumn(List<string> taskIds, string columnId)
	{
		List<TaskBaseViewModel> taskAndChildrenInBatch = TaskCache.GetTaskAndChildrenInBatch(taskIds);
		foreach (TaskBaseViewModel item in taskAndChildrenInBatch)
		{
			TaskModel taskModel = await TaskDao.GetTaskById(item.Id);
			if (taskModel != null)
			{
				taskModel.columnId = columnId;
				taskModel.modifiedTime = DateTime.Now;
				await UpdateTaskColumnId(taskModel);
			}
		}
	}

	public static async Task BatchSetCompleteDate(List<string> taskIds, DateTime date)
	{
		List<TaskModel> list = await GetSelectedTasks(taskIds);
		for (int i = 0; i < list.Count; i++)
		{
			TaskModel taskModel = list[i];
			if (taskModel.status != 0)
			{
				taskModel.completedTime = date;
				taskModel.modifiedTime = DateTime.Now;
			}
		}
		await BatchSaveTask(list);
	}

	public static async Task BatchStarTaskOrNote(List<string> ids, string project, bool pin)
	{
		foreach (TaskModel item in await TaskDao.GetThinTasksInBatch(ids))
		{
			if (item.CheckEnable())
			{
				await TogglesStarred(item.id, project ?? item.projectId, pin, null, isInDetail: false, notify: false);
			}
		}
		TaskChangeNotifier.NotifyBatchTaskStarredChanged(new TaskPinExtra
		{
			Ids = ids,
			IsPin = pin
		});
	}

	public static async Task<bool> BatchSwitchTaskOrNote(List<string> ids, bool toast = true)
	{
		if (ids == null || !ids.Any())
		{
			return false;
		}
		List<TaskModel> list = await TaskDao.GetThinTasksInBatch(ids);
		if (list == null || !list.Any())
		{
			return false;
		}
		list.Any((TaskModel t) => t.kind == "NOTE");
		foreach (TaskModel task in list)
		{
			if (task.kind == "NOTE")
			{
				task.kind = "TEXT";
			}
			else
			{
				if (task.kind == "CHECKLIST")
				{
					List<string> checklist = (from s in await TaskDetailItemDao.GetCheckItemsByTaskId(task.id)
						orderby s.sortOrder
						select s.title).ToList();
					task.content = ChecklistUtils.Items2Text(task.desc, checklist);
					TaskModel taskModel = task;
					taskModel.content = await AttachmentDao.AddAttachmentStrings(task.id, task.content);
					task.desc = "";
				}
				task.dueDate = null;
				task.kind = "NOTE";
				task.status = 0;
				task.completedTime = null;
				task.priority = 0;
				task.progress = 0;
				task.repeatFlag = null;
				task.repeatFrom = null;
			}
			task.modifiedTime = DateTime.Now;
			await UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
		if (toast)
		{
			Utils.Toast(Utils.GetString("Converted"));
		}
		TaskChangeNotifier.NotifyTaskKindChanged(ids);
		return true;
	}

	public static async Task<bool> BatchSetDate(List<string> taskIds, DateTime? date)
	{
		if (taskIds != null && taskIds.Count > 0)
		{
			if (!date.HasValue)
			{
				await BatchClearDate(taskIds);
			}
			else
			{
				await BatchSetStartDate(taskIds, date.Value);
			}
			return true;
		}
		return false;
	}

	public static async Task<bool> MoveColumnAsync(string columnId, string fromProjectId, string toProjectId, string toColumnId = null, bool isSection = false, bool alwaysIgnorePin = false)
	{
		ProjectModel toProject = CacheManager.GetProjectById(toProjectId);
		if (toProject == null)
		{
			return false;
		}
		List<ColumnModel> columns = await ColumnDao.CheckProjectColumns(toProjectId);
		string text = await ColumnDao.GetProjectDefaultColumnId(fromProjectId);
		List<TaskBaseViewModel> tasks = await TaskDao.GetDisplayTasksInColumn(fromProjectId, columnId, columnId == text);
		if (columns?.Count >= LimitCache.GetLimitByKey(Constants.LimitKind.KanbanNumber))
		{
			new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("ExceedMaxSectionCountMessage"), MessageBoxButton.OK, Utils.GetToastWindow() as Window).ShowDialog();
			return false;
		}
		if (await ProChecker.CheckTaskLimit(toProjectId, tasks.Count))
		{
			return false;
		}
		if (fromProjectId != toProjectId)
		{
			ColumnModel columnModel = await ColumnDao.GetColumnById(columnId);
			if (columnModel != null)
			{
				columnModel.projectId = toProjectId;
				columnModel.sortOrder = (columns?.Max((ColumnModel c) => c.sortOrder)).GetValueOrDefault() + 268435456;
				await App.Connection.UpdateAsync(columnModel);
			}
		}
		tasks = TaskCache.GetTaskAndChildrenInBatch(tasks.Select((TaskBaseViewModel t) => t.Id).ToList(), checkExist: true, fromProjectId);
		if (isSection || alwaysIgnorePin)
		{
			IEnumerable<TaskBaseViewModel> enumerable = tasks.Where((TaskBaseViewModel x) => true);
			if (isSection)
			{
				enumerable = enumerable.Where((TaskBaseViewModel t) => t.IsPinned || t.Status != 0);
			}
			else if (alwaysIgnorePin)
			{
				enumerable = enumerable.Where((TaskBaseViewModel t) => t.IsPinned);
			}
			IEnumerable<string> excludeTaskIds = enumerable.Select((TaskBaseViewModel x) => x.Id).Distinct();
			foreach (TaskBaseViewModel task in enumerable)
			{
				if (!string.IsNullOrEmpty(task.ParentId) && !excludeTaskIds.Contains(task.ParentId))
				{
					task.ParentId = string.Empty;
					await TaskDao.UpdateParent(task.Id, string.Empty);
					UtilLog.Info("SetTaskParent empty " + task.Id + " MoveSection");
				}
				task.ColumnId = string.Empty;
			}
			await BatchSetColumn(excludeTaskIds.ToList(), string.Empty);
			tasks.RemoveAll((TaskBaseViewModel x) => excludeTaskIds.Contains(x.Id));
		}
		List<string> ids = tasks.Select((TaskBaseViewModel t) => t.Id).ToList();
		MoveProjectArgs moveProjectArgs = new MoveProjectArgs(toProject);
		if (toColumnId != null)
		{
			moveProjectArgs.ColumnId = toColumnId;
		}
		else
		{
			moveProjectArgs.ColumnId = columnId;
		}
		await BatchMoveProject(ids, moveProjectArgs);
		await BatchSetColumn((from t in tasks
			where t.ColumnId != columnId && t.ColumnId != toColumnId
			select t.Id).ToList(), columnId);
		await SyncStatusDao.AddColumnMoveProjectStatus(columnId, fromProjectId);
		DataChangedNotifier.NotifyColumnChanged(toProjectId);
		DataChangedNotifier.NotifyColumnChanged(fromProjectId);
		TaskChangeNotifier.NotifyTaskBatchChanged(ids);
		return true;
	}

	public static async Task DelayOrAdvancedTasks(List<string> taskIds, long l)
	{
		foreach (string taskId in taskIds)
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
			if (taskById != null)
			{
				TimeDataModel delayTimeData = GetDelayTimeData(taskById, l);
				if (delayTimeData != null)
				{
					await SetDateTime(delayTimeData);
				}
			}
		}
	}

	public static TimeDataModel GetDelayTimeData(TaskBaseViewModel task, long mins, bool useCompleteTime = false)
	{
		bool flag = Math.Abs(mins) >= 1440;
		if ((!useCompleteTime && ((!flag && (task.IsAllDay ?? true)) || !task.StartDate.HasValue)) || (useCompleteTime && !task.StartDate.HasValue && task.Status != 0 && task.CompletedTime.HasValue))
		{
			return null;
		}
		TimeDataModel timeDataModel = new TimeDataModel
		{
			TaskId = task.Id,
			RepeatFlag = task.RepeatFlag,
			RepeatFrom = task.RepeatFrom,
			IsAllDay = task.IsAllDay,
			StartDate = task.StartDate,
			DueDate = task.DueDate
		};
		if (!task.StartDate.HasValue && useCompleteTime && task.CompletedTime.HasValue)
		{
			timeDataModel.IsAllDay = false;
			timeDataModel.StartDate = task.CompletedTime.Value.AddMinutes(mins);
			timeDataModel.HandleReminderMode = ReminderMode.SetDefault;
			return timeDataModel;
		}
		if (!timeDataModel.StartDate.HasValue)
		{
			return null;
		}
		timeDataModel.StartDate = timeDataModel.StartDate.Value.AddMinutes(mins);
		if (timeDataModel.DueDate.HasValue)
		{
			timeDataModel.DueDate = timeDataModel.DueDate.Value.AddMinutes(mins);
		}
		return timeDataModel;
	}

	public static bool IsTaskMatchedFilter(FilterModel normal, TaskBaseViewModel task, bool inAll = false)
	{
		return GetTasksMatchedFilter(normal, new List<TaskBaseViewModel> { task }, inCal: false, inAll)?.Any() ?? false;
	}

	public static List<TaskBaseViewModel> GetTaskMatchedNormalFilter(NormalFilterViewModel normal, List<TaskBaseViewModel> tasks, bool inAll = false, bool inCal = false)
	{
		if (normal != null)
		{
			List<FilterDatePair> list = null;
			if (normal.DueDates != null && normal.DueDates.Count > 0)
			{
				list = FilterUtils.GetFilterDatePairs(normal.DueDates);
			}
			TaskType taskType = ((normal.TaskTypes.Count != 1) ? TaskType.TaskAndNote : ((normal.TaskTypes[0] != "task") ? TaskType.Note : TaskType.Task));
			List<string> keywords = normal.Keywords;
			string keyword = ((keywords != null && keywords.Count > 0) ? normal.Keywords[0] : null);
			List<TaskBaseViewModel> list2 = TaskViewModelHelper.GetTasks(tasks.Where((TaskBaseViewModel task) => string.IsNullOrEmpty(task.CalendarId)).ToList(), normal.Groups, normal.Projects, normal.Tags, list, showComplete: true, inTodayOrWeek: false, inAll, normal.Assignees, normal.Priorities, LogicType.Or, withChild: false, inCal, orTag: false, taskType, keyword).ToList();
			list2.AddRange(GetEventsMatchedNormalFilter(normal, tasks, list, inCal));
			return list2;
		}
		return new List<TaskBaseViewModel>();
	}

	private static List<TaskBaseViewModel> GetEventsMatchedNormalFilter(NormalFilterViewModel normal, List<TaskBaseViewModel> tasks, List<FilterDatePair> pairs, bool inCal)
	{
		List<TaskBaseViewModel> list = tasks.Where((TaskBaseViewModel task) => !string.IsNullOrEmpty(task.CalendarId)).ToList();
		if (list.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		if (normal.Assignees.Any() || normal.Tags.Any() || normal.Priorities.Any() || normal.Status.Any() || normal.TaskTypes.Any() || (normal.Projects.Count > 0 && normal.Projects.All((string t) => t != "Calendar5959a2259161d16d23a4f272")))
		{
			return new List<TaskBaseViewModel>();
		}
		return list.Where(delegate(TaskBaseViewModel task)
		{
			if (!IsEventMatchedCalendarFilter(task, normal))
			{
				return false;
			}
			if (!TaskUtils.CheckDate(task, pairs, inTodayOrWeek: false, LogicType.Or, inCal, showOutDateNote: true))
			{
				return false;
			}
			string text = normal.Keywords?.FirstOrDefault()?.Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return true;
			}
			string text2 = (task.Title + "\n" + task.Content + "\n" + task.Desc).ToLower();
			return (from key in text.Split(' ')
				where !string.IsNullOrEmpty(key)
				select key).ToList().All(text2.Contains);
		}).ToList();
	}

	private static bool IsEventMatchedCalendarFilter(TaskBaseViewModel task, NormalFilterViewModel normal)
	{
		if (normal.Calendars.Contains("#allSubscribe") || (!normal.CalendarAccounts.Any() && !normal.Calendars.Any() && (normal.Projects.Count == 0 || normal.Projects.Contains("Calendar5959a2259161d16d23a4f272"))))
		{
			return true;
		}
		HashSet<string> hashSet = new HashSet<string>(normal.Calendars.Where((string id) => id != "#allSubscribe"));
		if (normal.CalendarAccounts.Any())
		{
			HashSet<string> accountIds = new HashSet<string>(normal.CalendarAccounts);
			foreach (string item in from calendar in CacheManager.GetBindCalendars()
				where CalendarUtils.ContainsAccountId(accountIds, calendar.AccountId)
				select calendar.Id)
			{
				hashSet.Add(item);
			}
		}
		if (hashSet.Count > 0)
		{
			return CalendarUtils.ContainsCalendarId(hashSet, task.CalendarId);
		}
		return false;
	}

	public static List<TaskBaseViewModel> GetTasksMatchedFilter(FilterModel filter, List<TaskBaseViewModel> tasks, bool inCal = false, bool inAll = false)
	{
		if (filter == null)
		{
			return new List<TaskBaseViewModel>();
		}
		if (ticktick_WPF.Util.Filter.Parser.GetFilterRuleType(filter.rule) == 0)
		{
			return GetTaskMatchedNormalFilter(ticktick_WPF.Util.Filter.Parser.ToNormalModel(filter.rule), tasks, inAll, inCal);
		}
		AdvancedFilterViewModel advancedFilterViewModel = ticktick_WPF.Util.Filter.Parser.ToAdvanceModel(filter.rule);
		if (!IsAdvanceModelValid(advancedFilterViewModel.CardList))
		{
			return new List<TaskBaseViewModel>();
		}
		return GetTaskMatchedAdvancedFilter(advancedFilterViewModel, tasks, inCal);
	}

	public static List<TaskBaseViewModel> GetCheckItemsMatchedFilter(FilterModel filter, List<TaskBaseViewModel> items)
	{
		if (filter == null)
		{
			return new List<TaskBaseViewModel>();
		}
		if (ticktick_WPF.Util.Filter.Parser.GetFilterRuleType(filter.rule) == 0)
		{
			return GetItemMatchedNormalFilter(ticktick_WPF.Util.Filter.Parser.ToNormalModel(filter.rule), items);
		}
		AdvancedFilterViewModel advancedFilterViewModel = ticktick_WPF.Util.Filter.Parser.ToAdvanceModel(filter.rule);
		if (!IsAdvanceModelValid(advancedFilterViewModel.CardList))
		{
			return new List<TaskBaseViewModel>();
		}
		return GetItemsInAdvancedFilter(advancedFilterViewModel, items);
	}

	public static List<TaskBaseViewModel> GetItemMatchedNormalFilter(NormalFilterViewModel normal, List<TaskBaseViewModel> items)
	{
		if (normal != null)
		{
			List<FilterDatePair> startEndPairs = null;
			if (normal.DueDates != null && normal.DueDates.Count > 0)
			{
				startEndPairs = FilterUtils.GetFilterDatePairs(normal.DueDates);
			}
			return TaskViewModelHelper.GetItems(items, normal.Groups, normal.Projects, normal.Tags, startEndPairs, showComplete: true, inTodayOrWeek: false, checkInAll: false, normal.Assignees, normal.Priorities).ToList();
		}
		return new List<TaskBaseViewModel>();
	}

	private static List<TaskBaseViewModel> GetItemsInAdvancedFilter(AdvancedFilterViewModel advanced, List<TaskBaseViewModel> items)
	{
		List<CardViewModel> list = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
		List<CardViewModel> list2 = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.LogicAnd || card.Type == CardType.LogicOr).ToList();
		DateCardViewModel dateCardViewModel = (DateCardViewModel)list.FirstOrDefault((CardViewModel c) => c is DateCardViewModel);
		bool num = dateCardViewModel?.Values != null && (dateCardViewModel.Values.Any((string v) => v != "nodue") || dateCardViewModel.LogicType == LogicType.Not);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (num)
		{
			switch (list.IndexOf(dateCardViewModel))
			{
			case 0:
				flag = true;
				switch (list.Count)
				{
				case 2:
					flag2 = list2[0].Type == CardType.LogicAnd;
					break;
				case 3:
					flag2 = list2[0].Type == CardType.LogicAnd;
					flag3 = list2[1].Type == CardType.LogicAnd;
					break;
				}
				break;
			case 1:
				flag2 = true;
				switch (list.Count)
				{
				case 2:
					flag = list2[0].Type == CardType.LogicAnd;
					break;
				case 3:
					flag = list2[0].Type == CardType.LogicAnd;
					flag3 = list2[1].Type == CardType.LogicAnd;
					break;
				}
				break;
			case 2:
				flag3 = true;
				if (list.Count == 3)
				{
					flag2 = list2[1].Type == CardType.LogicAnd;
					flag = flag2;
				}
				break;
			}
		}
		switch (list.Count)
		{
		case 1:
			if (!flag)
			{
				return new List<TaskBaseViewModel>();
			}
			return GetSingleFilterMatchedItems(list[0], items);
		case 2:
		{
			List<TaskBaseViewModel> first2 = (flag ? GetSingleFilterMatchedItems(list[0], items) : new List<TaskBaseViewModel>());
			List<TaskBaseViewModel> second3 = (flag2 ? GetSingleFilterMatchedItems(list[1], items) : new List<TaskBaseViewModel>());
			if (list2.Count == 1)
			{
				switch (list2[0].Type)
				{
				case CardType.LogicAnd:
					return GetInterSetOfModels(first2, second3);
				case CardType.LogicOr:
					return GetUnionOfModels(first2, second3);
				}
			}
			break;
		}
		case 3:
		{
			List<TaskBaseViewModel> first = (flag ? GetSingleFilterMatchedItems(list[0], items) : new List<TaskBaseViewModel>());
			List<TaskBaseViewModel> second = (flag2 ? GetSingleFilterMatchedItems(list[1], items) : new List<TaskBaseViewModel>());
			List<TaskBaseViewModel> second2 = (flag3 ? GetSingleFilterMatchedItems(list[2], items) : new List<TaskBaseViewModel>());
			if (list2.Count == 2)
			{
				CardType type = list2[0].Type;
				CardType type2 = list2[1].Type;
				List<TaskBaseViewModel> list3 = null;
				switch (type)
				{
				case CardType.LogicAnd:
					list3 = GetInterSetOfModels(first, second);
					break;
				case CardType.LogicOr:
					list3 = GetUnionOfModels(first, second);
					break;
				}
				if (list3 == null)
				{
					list3 = new List<TaskBaseViewModel>();
				}
				switch (type2)
				{
				case CardType.LogicAnd:
					return GetInterSetOfModels(list3, second2);
				case CardType.LogicOr:
					return GetUnionOfModels(list3, second2);
				}
			}
			break;
		}
		}
		return new List<TaskBaseViewModel>();
	}

	private static List<TaskBaseViewModel> GetSingleFilterMatchedItems(CardViewModel model, List<TaskBaseViewModel> items)
	{
		List<TaskBaseViewModel> result = new List<TaskBaseViewModel>();
		if (model != null)
		{
			if (!(model is PriorityCardViewModel priorityCardViewModel))
			{
				if (!(model is DateCardViewModel dateCardViewModel))
				{
					if (!(model is AssigneeCardViewModel assigneeCardViewModel))
					{
						if (!(model is ProjectOrGroupCardViewModel projectOrGroupCardViewModel))
						{
							if (model is TagCardViewModel tagCardViewModel)
							{
								result = TaskViewModelHelper.GetItems(items, null, null, tagCardViewModel.Values, null, showComplete: true, inTodayOrWeek: false, checkInAll: false, null, null, tagCardViewModel.LogicType).ToList();
							}
						}
						else
						{
							result = TaskViewModelHelper.GetItems(items, projectOrGroupCardViewModel.GroupIds, projectOrGroupCardViewModel.Values, null, null, showComplete: true, inTodayOrWeek: false, checkInAll: false, null, null, projectOrGroupCardViewModel.LogicType).ToList();
						}
					}
					else
					{
						result = TaskViewModelHelper.GetItems(items, null, null, null, null, showComplete: true, inTodayOrWeek: false, checkInAll: false, assigneeCardViewModel.Values, null, assigneeCardViewModel.LogicType).ToList();
					}
				}
				else
				{
					List<FilterDatePair> filterDatePairs = FilterUtils.GetFilterDatePairs(dateCardViewModel.Values);
					result = TaskViewModelHelper.GetItems(items, null, null, null, filterDatePairs, showComplete: true, inTodayOrWeek: false, checkInAll: false, null, null, dateCardViewModel.LogicType).ToList();
				}
			}
			else
			{
				result = TaskViewModelHelper.GetItems(items, null, null, null, null, showComplete: true, inTodayOrWeek: false, checkInAll: false, null, priorityCardViewModel.Values, priorityCardViewModel.LogicType).ToList();
			}
		}
		return result;
	}

	private static List<TaskBaseViewModel> GetTaskMatchedAdvancedFilter(AdvancedFilterViewModel advanced, List<TaskBaseViewModel> tasks, bool inCal = false)
	{
		List<CardViewModel> list = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
		List<CardViewModel> list2 = advanced.CardList.Where((CardViewModel card) => card.Type == CardType.LogicAnd || card.Type == CardType.LogicOr).ToList();
		switch (list.Count)
		{
		case 1:
			return GetSingleFilterMatchedTasks(list[0], tasks, inCal);
		case 2:
		{
			List<TaskBaseViewModel> singleFilterMatchedTasks4 = GetSingleFilterMatchedTasks(list[0], tasks, inCal);
			List<TaskBaseViewModel> singleFilterMatchedTasks5 = GetSingleFilterMatchedTasks(list[1], tasks, inCal);
			if (list2.Count == 1)
			{
				switch (list2[0].Type)
				{
				case CardType.LogicAnd:
					return GetInterSetOfModels(singleFilterMatchedTasks4, singleFilterMatchedTasks5);
				case CardType.LogicOr:
					return GetUnionOfModels(singleFilterMatchedTasks4, singleFilterMatchedTasks5);
				}
			}
			break;
		}
		case 3:
		{
			List<TaskBaseViewModel> singleFilterMatchedTasks = GetSingleFilterMatchedTasks(list[0], tasks, inCal);
			List<TaskBaseViewModel> singleFilterMatchedTasks2 = GetSingleFilterMatchedTasks(list[1], tasks, inCal);
			List<TaskBaseViewModel> singleFilterMatchedTasks3 = GetSingleFilterMatchedTasks(list[2], tasks, inCal);
			if (list2.Count == 2)
			{
				CardType type = list2[0].Type;
				CardType type2 = list2[1].Type;
				List<TaskBaseViewModel> list3 = null;
				switch (type)
				{
				case CardType.LogicAnd:
					list3 = GetInterSetOfModels(singleFilterMatchedTasks, singleFilterMatchedTasks2);
					break;
				case CardType.LogicOr:
					list3 = GetUnionOfModels(singleFilterMatchedTasks, singleFilterMatchedTasks2);
					break;
				}
				if (list3 == null)
				{
					list3 = new List<TaskBaseViewModel>();
				}
				switch (type2)
				{
				case CardType.LogicAnd:
					return GetInterSetOfModels(list3, singleFilterMatchedTasks3);
				case CardType.LogicOr:
					return GetUnionOfModels(list3, singleFilterMatchedTasks3);
				}
			}
			break;
		}
		}
		return new List<TaskBaseViewModel>();
	}

	private static List<TaskBaseViewModel> GetSingleFilterMatchedTasks(CardViewModel model, List<TaskBaseViewModel> tasks, bool inCal = false)
	{
		List<TaskBaseViewModel> result = new List<TaskBaseViewModel>();
		if (model != null)
		{
			if (!(model is PriorityCardViewModel priorityCardViewModel))
			{
				if (!(model is DateCardViewModel dateCardViewModel))
				{
					if (!(model is AssigneeCardViewModel assigneeCardViewModel))
					{
						if (!(model is ProjectOrGroupCardViewModel projectOrGroupCardViewModel))
						{
							if (!(model is TagCardViewModel tagCardViewModel))
							{
								if (!(model is TaskTypeViewModel { Values: var values }))
								{
									if (model is KeywordsViewModel { Keyword: var keyword })
									{
										result = TaskViewModelHelper.GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, null, null, LogicType.Or, withChild: false, inCal: false, orTag: false, TaskType.TaskAndNote, keyword).ToList();
									}
								}
								else
								{
									TaskType taskType = ((values == null || values.Count != 1) ? TaskType.TaskAndNote : ((values[0] != "task") ? TaskType.Note : TaskType.Task));
									result = TaskViewModelHelper.GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, null, null, LogicType.Or, withChild: false, inCal: false, orTag: false, taskType).ToList();
								}
							}
							else
							{
								result = TaskViewModelHelper.GetTasks(tasks, null, null, tagCardViewModel.Values, null, showComplete: true, inTodayOrWeek: false, inAll: false, null, null, tagCardViewModel.LogicType, withChild: false, inCal).ToList();
							}
						}
						else
						{
							result = TaskViewModelHelper.GetTasks(tasks, projectOrGroupCardViewModel.GroupIds, projectOrGroupCardViewModel.Values, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, null, null, projectOrGroupCardViewModel.LogicType, withChild: false, inCal).ToList();
						}
					}
					else
					{
						result = TaskViewModelHelper.GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, assigneeCardViewModel.Values, null, assigneeCardViewModel.LogicType, withChild: false, inCal).ToList();
					}
				}
				else
				{
					List<FilterDatePair> filterDatePairs = FilterUtils.GetFilterDatePairs(dateCardViewModel.Values);
					result = TaskViewModelHelper.GetTasks(tasks, null, null, null, filterDatePairs, showComplete: true, inTodayOrWeek: false, inAll: false, null, null, dateCardViewModel.LogicType, withChild: false, inCal).ToList();
				}
			}
			else
			{
				result = TaskViewModelHelper.GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, null, priorityCardViewModel.Values, priorityCardViewModel.LogicType, withChild: false, inCal).ToList();
			}
		}
		return result;
	}

	public static async Task<TaskCloseExtra> SetTaskStatus(string taskId, int closeStatus, bool needUndo = false, IToastShowWindow undoWindow = null, bool inDetail = false, bool playSound = true)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (!string.IsNullOrEmpty(taskModel.exDates))
			{
				taskModel.exDate = ExDateSerilizer.ToArray(taskModel.exDates);
			}
			if (needUndo && inDetail && taskModel.status != 0 && closeStatus == 0 && CloseUndoHandler.TryUndoInDetail(taskModel.id))
			{
				return null;
			}
			return await SetTaskStatus(taskModel, closeStatus, playSound, ignoreRepeat: false, needUndo, undoWindow, completeByItem: false, !inDetail);
		}
		return null;
	}

	public static async Task<TaskCloseExtra> SetTaskStatus(TaskModel originalTask, int closeStatus, bool playSound = true, bool ignoreRepeat = false, bool needUndo = false, IToastShowWindow undoWindow = null, bool completeByItem = false, bool toastUndo = true)
	{
		if (closeStatus == 2)
		{
			LocalSettings.Settings.ExtraSettings.LastCompleteTime = ticktick_WPF.Util.DateUtils.GetDateNum(DateTime.Today);
		}
		if (playSound && originalTask.status == 0 && closeStatus != -1)
		{
			Utils.PlayCompletionSound();
		}
		UtilLog.Info($"TaskService.SetTaskStatus {originalTask.id},{Utils.GetSafeDateLogText(originalTask.startDate)}, {originalTask.status} to {closeStatus}, repeatFlag {originalTask.repeatFlag}, ignoreRepeat {ignoreRepeat}");
		TaskCloseExtra result = new TaskCloseExtra();
		if (Utils.IsEmptyDate(originalTask.startDate))
		{
			originalTask.startDate = null;
		}
		if (closeStatus != 0 && originalTask.remindTime.HasValue)
		{
			originalTask.remindTime = null;
			await ReminderDelayDao.DeleteByIdAsync(originalTask.id, "task");
		}
		ProjectModel project = await ProjectDao.GetProjectById(originalTask.projectId);
		if (project != null && originalTask.status != closeStatus && project.viewMode == "kanban" && string.IsNullOrEmpty(originalTask.columnId))
		{
			originalTask.columnId = await ColumnDao.GetProjectDefaultColumnId(originalTask.projectId);
		}
		needUndo = needUndo && originalTask.status == 0;
		if (needUndo)
		{
			if (completeByItem)
			{
				CloseUndoHandler.AddUndoOriginTask(originalTask.id, originalTask.Clone());
			}
			else
			{
				CloseUndoHandler.AddUndoModel(originalTask.id, originalTask.Clone());
			}
		}
		bool isNonRepeat = IsNonRepeatTask(originalTask);
		if (isNonRepeat || ignoreRepeat)
		{
			if (originalTask.status != closeStatus)
			{
				if (originalTask.status == 0)
				{
					await TickFocusManager.NotifyPomoTaskChanged(originalTask.id, null);
				}
				originalTask.status = closeStatus;
				originalTask.modifiedTime = DateTime.Now;
				if (originalTask.status != 0)
				{
					originalTask.completedTime = DateTime.Now;
					originalTask.completedUserId = Utils.GetCurrentUserStr();
					await TryCloseSubTask(originalTask.id, originalTask.projectId, needUndo, closeStatus);
				}
				else
				{
					originalTask.completedTime = null;
					originalTask.completedUserId = null;
					await TryUnCompleteParentTask(originalTask.id);
				}
			}
		}
		else
		{
			TaskModel newTask = new TaskModel(originalTask)
			{
				id = Utils.GetGuid(),
				status = closeStatus,
				repeatFlag = string.Empty,
				createdTime = DateTime.Now,
				completedTime = DateTime.Now,
				repeatTaskId = originalTask.id,
				sortOrder = ProjectSortOrderDao.GetNextTaskSortOrderInProject(originalTask.projectId, originalTask.sortOrder, originalTask.parentId),
				completedUserId = Utils.GetCurrentUserStr()
			};
			newTask.AddPinnedSecond();
			double diffInMinutes = 0.0;
			if (originalTask.startDate.HasValue && originalTask.dueDate.HasValue)
			{
				diffInMinutes = (originalTask.dueDate.Value - originalTask.startDate.Value).TotalMinutes;
			}
			int addForgotCycle = 1;
			originalTask.startDate = RepeatUtils.GetNextRepeatDate(originalTask, ref addForgotCycle);
			TaskModel taskModel = await TaskDao.GetThinTaskById(originalTask.id);
			originalTask.repeatFlag = RRuleUtils.GetNextCount(originalTask.repeatFlag, handleEbbingCount: true);
			if (originalTask.dueDate.HasValue && originalTask.startDate.HasValue)
			{
				originalTask.dueDate = originalTask.startDate.Value.AddMinutes(diffInMinutes);
			}
			originalTask.modifiedTime = DateTime.Now;
			if (taskModel != null && taskModel.startDate == originalTask.startDate && originalTask.repeatFrom != "1")
			{
				if (originalTask.status == 0)
				{
					await TickFocusManager.NotifyPomoTaskChanged(originalTask.id, null);
				}
				originalTask.status = closeStatus;
				originalTask.modifiedTime = DateTime.Now;
				if (originalTask.status != 0)
				{
					originalTask.completedTime = DateTime.Now;
					originalTask.completedUserId = Utils.GetCurrentUserStr();
					await TryCloseSubTask(originalTask.id, originalTask.projectId, needUndo, closeStatus);
				}
				else
				{
					originalTask.completedTime = null;
					originalTask.completedUserId = null;
					await TryUnCompleteParentTask(originalTask.id);
				}
			}
			else
			{
				originalTask.remindTime = null;
				originalTask.status = 0;
				originalTask.progress = null;
				TaskModel repeatTask = await TaskDao.InsertTask(newTask);
				await ReminderDelayDao.DeleteByIdAsync(originalTask.id, "task");
				if (needUndo)
				{
					CloseUndoHandler.AddUndoNewTasks(originalTask.id, new List<TaskModel> { repeatTask });
				}
				if (!needUndo)
				{
					SyncStatusDao.AddSyncStatus(newTask.id, 4);
				}
				if (!string.IsNullOrEmpty(repeatTask.parentId))
				{
					await TaskDao.AddOrRemoveTaskChildIds(repeatTask.parentId, new List<string> { repeatTask.id }, addChild: true);
					await SyncStatusDao.AddSetParentSyncStatus(repeatTask.id, "");
				}
				await TryCloseAndCopySubTask(originalTask, newTask, needUndo, closeStatus);
				await ProjectCopyManager.CopyChecklistItems(originalTask.id, repeatTask.id);
				DataChangedNotifier.NotifyNewTaskWhenModifyRepeat(newTask.id, originalTask.id);
				double deltaInDays = 0.0;
				if (originalTask.startDate.HasValue && repeatTask.startDate.HasValue)
				{
					deltaInDays = (originalTask.startDate.Value - repeatTask.startDate.Value).TotalDays;
				}
				await UpdateTaskCheckItemsWithDeltaDate(originalTask.id, deltaInDays);
				if (originalTask.reminders == null)
				{
					List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(originalTask.id);
					if (originalTask.isAllDay.HasValue && originalTask.isAllDay.Value && list.Any())
					{
						foreach (TaskReminderModel item in list)
						{
							item.trigger = TriggerUtils.ConvertLegacyTrigger(item.trigger);
						}
					}
					originalTask.reminders = list.ToArray();
				}
				TaskModel taskModel2 = repeatTask;
				taskModel2.reminders = await Utils.CopyReminderItem(repeatTask, originalTask.reminders);
				taskModel2 = repeatTask;
				taskModel2.Attachments = await Utils.CopyAttachmentItem(repeatTask, originalTask.id);
				taskModel2 = repeatTask;
				taskModel2.FocusSummaries = await Utils.CopyPomoSummaries(repeatTask, originalTask.id);
				UtilLog.Info("TaskService.CleanPomosByTaskId: " + originalTask.id);
				await PomoSummaryDao.CleanPomosByTaskId(originalTask.id);
				ClearMovedPomoSummaries(originalTask);
				await PomoDao.UpdatePomoTaskId(originalTask.id, newTask.id);
				await UpdateTask(repeatTask);
				if (Utils.IsEmptyDate(originalTask.startDate))
				{
					originalTask.deleted = 1;
					await TaskDao.DeleteTaskToTrash(originalTask.id);
					await SyncStatusDao.AddTrashTaskStatus(originalTask.id);
				}
				result.RepeatTask = repeatTask;
			}
			await TickFocusManager.NotifyPomoTaskChanged(originalTask.id, newTask.id);
		}
		result.OriginalTask = originalTask;
		if (originalTask.kind == "CHECKLIST")
		{
			TaskModel newTask = originalTask;
			newTask.progress = await CalculateProgress(originalTask.id);
		}
		if (project != null)
		{
			result.OriginalTask.Color = project.color;
			if (result.RepeatTask != null)
			{
				result.RepeatTask.Color = project.color;
			}
		}
		await UpdateTask(originalTask);
		if (!needUndo)
		{
			await SyncStatusDao.AddSyncStatus(originalTask.id, 0);
		}
		if (needUndo)
		{
			if (toastUndo)
			{
				CloseUndoHandler.TryToast(undoWindow ?? App.Window, closeStatus);
			}
			else
			{
				CloseUndoHandler.TryStartTimer();
			}
		}
		TaskChangeNotifier.NotifyTaskStatusChanged(result);
		UtilLog.Info($"SetTaskStatus : isRepeat {!isNonRepeat}");
		return result;
	}

	public static async Task TryCloseAndCopySubTask(TaskModel originalTask, TaskModel newTask, bool needUndo, int closeStatus)
	{
		List<TaskModel> subTasks = await GetAllSubTasksByIdAsync(originalTask.id, originalTask.projectId);
		List<TaskModel> list = new List<TaskModel>();
		List<TaskModel> updateTasks = new List<TaskModel>();
		AssemblyRepeatTaskOnComplete(subTasks, list, updateTasks);
		list.RemoveAll((TaskModel t) => t.id == originalTask.id);
		Dictionary<string, string> dict = new Dictionary<string, string> { [originalTask.id] = newTask.id };
		List<TaskModel> copyTasks = new List<TaskModel>();
		foreach (TaskModel task in list)
		{
			TaskModel taskModel = new TaskModel(task)
			{
				id = Utils.GetGuid()
			};
			if (taskModel.status == 0)
			{
				taskModel.status = closeStatus;
				taskModel.completedTime = DateTime.Now;
				taskModel.completedUserId = Utils.GetCurrentUserIdInt().ToString();
			}
			taskModel.AddPinnedSecond();
			copyTasks.Add(taskModel);
			dict[task.id] = taskModel.id;
			await CopyExtraData(task, taskModel);
			if (task.status != 0 || task.progress != 0 || task.kind == "CHECKLIST")
			{
				if (needUndo)
				{
					CloseUndoHandler.AddUndoOriginTask(originalTask.id, task.Clone());
				}
				task.status = 0;
				task.progress = null;
				task.completedTime = null;
				task.modifiedTime = DateTime.Now;
				await UpdateTaskOnStatusChanged(task);
				if (task.kind == "CHECKLIST")
				{
					await UpdateTaskCheckItemsWithDeltaDate(task.id, 0.0);
				}
				if (!needUndo)
				{
					await SyncStatusDao.AddSyncStatus(task.id, 0);
				}
			}
		}
		foreach (TaskModel item in copyTasks)
		{
			if (!string.IsNullOrEmpty(item.parentId) && dict.ContainsKey(item.parentId))
			{
				string parentId = dict[item.parentId];
				item.parentId = parentId;
				TaskModel taskModel2 = await TaskDao.InsertTask(item);
				if (needUndo)
				{
					await SyncStatusDao.AddSyncStatus(taskModel2.id, 4);
				}
			}
		}
		foreach (TaskModel task in copyTasks)
		{
			if (!string.IsNullOrEmpty(task.parentId))
			{
				await SyncStatusDao.AddSetParentSyncStatus(task.id, "");
				await TaskDao.AddOrRemoveTaskChildIds(task.parentId, new List<string> { task.id }, addChild: true);
			}
		}
		if (needUndo)
		{
			CloseUndoHandler.AddUndoNewTasks(originalTask.id, copyTasks);
		}
		foreach (TaskModel item2 in updateTasks)
		{
			if (!string.IsNullOrEmpty(item2.parentId) && dict.ContainsKey(item2.parentId))
			{
				CloseUndoHandler.AddUndoOriginTask(originalTask.id, item2.Clone());
				string parentId2 = dict[item2.parentId];
				await TaskDao.UpdateParent(item2.id, parentId2);
			}
		}
	}

	private static void AssemblyRepeatTaskOnComplete(List<TaskModel> subTasks, List<TaskModel> needCopyTasks, List<TaskModel> updateTasks)
	{
		if (subTasks == null)
		{
			return;
		}
		List<TaskModel> list = subTasks.Where((TaskModel t) => t.status != 0 && !string.IsNullOrEmpty(t.repeatTaskId) && t.id != t.repeatTaskId && subTasks.Any((TaskModel ta) => ta.id == t.repeatTaskId)).ToList();
		HashSet<TaskModel> hashSet = new HashSet<TaskModel>(list);
		subTasks.RemoveAll(hashSet.Contains);
		foreach (TaskModel item in list)
		{
			List<TaskModel> subTasks2 = GetSubTasks(item.id, subTasks);
			if (subTasks2 == null || !subTasks2.Any())
			{
				continue;
			}
			foreach (TaskModel item2 in subTasks2)
			{
				subTasks.Remove(item2);
				hashSet.Add(item2);
			}
		}
		subTasks.RemoveAll(hashSet.Contains);
		needCopyTasks.AddRange(subTasks);
		updateTasks.AddRange(hashSet);
	}

	private static async Task CopyExtraData(TaskModel task, TaskModel copy)
	{
		await ProjectCopyManager.CopyChecklistItems(task.id, copy.id);
		List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(task.id);
		if (task.isAllDay.HasValue && task.isAllDay.Value && list.Any())
		{
			foreach (TaskReminderModel item in list)
			{
				item.trigger = TriggerUtils.ConvertLegacyTrigger(item.trigger);
			}
		}
		TaskModel taskModel;
		if (list != null)
		{
			task.reminders = list.ToArray();
			taskModel = copy;
			taskModel.reminders = await Utils.CopyReminderItem(copy, task.reminders);
		}
		taskModel = copy;
		taskModel.Attachments = await Utils.CopyAttachmentItem(copy, task.id);
		taskModel = copy;
		taskModel.FocusSummaries = await Utils.CopyPomoSummaries(copy, task.id);
		await PomoSummaryDao.CleanPomosByTaskId(task.id);
		ClearMovedPomoSummaries(task);
	}

	internal static void ClearMovedPomoSummaries(TaskModel task)
	{
		if (task != null)
		{
			task.FocusSummaries = null;
		}
	}

	private static async Task TryUnCompleteParentTask(string taskId)
	{
		List<TaskModel> list = await TaskDao.GetParentsByTaskIdAsync(taskId);
		if (list == null)
		{
			return;
		}
		foreach (TaskModel task in list)
		{
			task.status = 0;
			task.completedTime = null;
			task.completedUserId = null;
			await UpdateTaskOnStatusChanged(task);
			await SyncStatusDao.AddSyncStatus(task.id, 0);
		}
	}

	private static async Task TryCloseSubTask(string taskId, string projectId, bool needUndo, int closeStatus)
	{
		List<TaskModel> subTasks = await GetAllSubTasksByIdAsync(taskId, projectId);
		if (subTasks == null)
		{
			return;
		}
		subTasks = subTasks.Where((TaskModel s) => s.status == 0).ToList();
		foreach (TaskModel task in subTasks)
		{
			if (needUndo)
			{
				CloseUndoHandler.AddUndoOriginTask(taskId, task.Clone());
			}
			await TickFocusManager.NotifyPomoTaskChanged(task.id, null);
			task.status = closeStatus;
			task.completedTime = DateTime.Now;
			task.completedUserId = Utils.GetCurrentUserStr();
		}
		await TaskDao.BatchUpdateTasks(subTasks, checkUserId: true, CheckMatchedType.All);
		foreach (TaskModel item in subTasks)
		{
			if (!needUndo)
			{
				await SyncStatusDao.AddSyncStatus(item.id, 0);
			}
		}
	}

	private static async Task UpdateTaskCheckItemsWithDeltaDate(string originalTaskId, double deltaInDays)
	{
		List<TaskDetailItemModel> checklistItems = await TaskDetailItemDao.GetCheckItemsByTaskId(originalTaskId);
		if (checklistItems.Count <= 0)
		{
			return;
		}
		foreach (TaskDetailItemModel item in checklistItems)
		{
			item.status = 0;
			if (item.startDate.HasValue)
			{
				item.startDate = item.startDate.Value.AddDays(deltaInDays);
			}
		}
		await TaskDetailItemDao.BatchUpdateChecklists(checklistItems.ToList());
		TaskChangeNotifier.OnCheckItemsChanged((from item in checklistItems
			where !string.IsNullOrEmpty(item.id)
			select item.id).ToList());
	}

	public static async Task<TaskModel> DeleteCheckItem(string itemId)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null)
		{
			await TaskDetailItemDao.DeleteById(itemId);
			ItemChangeNotifier.NotifyItemDeleted(item);
			CheckItemCompleteExtra checkItemCompleteExtra = await SyncTaskChange(item);
			if (checkItemCompleteExtra != null)
			{
				return checkItemCompleteExtra.OriginalTask;
			}
		}
		return null;
	}

	public static async Task ReminderCompleteCheckItem(string itemId)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null && item.status == 0)
		{
			item.status = 1;
			item.completedTime = DateTime.Now;
			await TaskDetailItemDao.SaveChecklistItem(item);
			await SyncTaskChange(item, handleTask: true);
			ItemChangeNotifier.NotifyItemDateChanged(item);
			if (await TaskDao.GetThinTaskById(item.TaskServerId) != null)
			{
				SyncManager.TryDelaySync();
			}
		}
	}

	public static async Task CompleteCheckItem(string itemId, bool handleTask = true, bool completeOnly = false, bool needUndo = false, IToastShowWindow window = null)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null && (!completeOnly || item.status != 1))
		{
			if (needUndo)
			{
				CloseUndoHandler.AddUndoCheckItem(item.TaskServerId, item);
			}
			item.status = ((item.status != 1) ? 1 : 0);
			item.completedTime = ((item.status == 1) ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			await TaskDetailItemDao.SaveChecklistItem(item);
			await SyncTaskChange(item, handleTask, needUndo, window);
			ItemChangeNotifier.NotifyItemStatusChanged(item.id);
		}
	}

	public static async Task<CheckItemCompleteExtra> SyncTaskChange(TaskDetailItemModel item, bool handleTask = false, bool needUndo = false, IToastShowWindow window = null)
	{
		TaskModel task = await TaskDao.GetThinTaskById(item.TaskServerId);
		if (task == null)
		{
			return null;
		}
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(task.id);
		string originKind = task.kind;
		if (list.Count == 0)
		{
			task.kind = "TEXT";
			task.progress = 0;
			task.content = ChecklistUtils.Items2Text(task.desc, new List<string>());
			task.modifiedTime = DateTime.Now;
			await UpdateTask(task);
			await SyncStatusDao.AddSyncStatus(item.TaskServerId, 0);
		}
		else
		{
			task.kind = "CHECKLIST";
			int progress = CalculateProgress(list);
			if (originKind != task.kind)
			{
				await UpdateTaskOnKindChanged(task);
			}
			if (progress < 100 && progress >= 0)
			{
				await SyncProgress(task, progress);
			}
			else if (progress == 100)
			{
				if (handleTask && TaskCache.CanTaskCompletedByCheckItem(task.id))
				{
					UtilLog.Info("CompleteTask id " + task.id + " from:CheckItem");
					TaskCloseExtra taskCloseExtra = await SetTaskStatus(task, 2, playSound: false, ignoreRepeat: false, needUndo, window, completeByItem: true);
					return new CheckItemCompleteExtra
					{
						ItemId = item.id,
						OriginalTask = taskCloseExtra.OriginalTask,
						RepeatTask = taskCloseExtra.RepeatTask
					};
				}
				await SyncProgress(task, progress);
			}
		}
		if (originKind != task.kind)
		{
			TaskChangeNotifier.NotifyTaskKindChanged(new List<string> { task.id });
		}
		return new CheckItemCompleteExtra
		{
			ItemId = item.id,
			OriginalTask = task
		};
	}

	public static async Task SyncProgress(TaskModel task, int progress)
	{
		task.progress = progress;
		task.modifiedTime = DateTime.Now;
		await UpdateTaskOnProgressChanged(task);
		await SyncStatusDao.AddSyncStatus(task.id, 0);
	}

	public static async Task<int> CalculateProgress(string taskId)
	{
		return CalculateProgress(await TaskDetailItemDao.GetCheckItemsByTaskId(taskId));
	}

	public static int CalculateProgress(IReadOnlyCollection<TaskDetailItemModel> checkItems)
	{
		if (checkItems.Any())
		{
			int count = checkItems.Count;
			int num = checkItems.Count((TaskDetailItemModel item) => item.status == 0);
			return (int)Math.Round((float)(count - num) * 1f / ((float)count * 1f) * 100f, 0, MidpointRounding.AwayFromZero);
		}
		return 0;
	}

	public static async Task<List<TaskModel>> CopySubTasks(string oldTaskId, string newTaskId)
	{
		TaskBaseViewModel taskById = TaskCache.GetTaskById(oldTaskId);
		List<TaskModel> children = await GetAllSubTasksByIdAsync(taskById.Id, taskById.ProjectId);
		List<TaskModel> copyChildren = new List<TaskModel>();
		Dictionary<string, string> dict = new Dictionary<string, string> { [oldTaskId] = newTaskId };
		foreach (TaskModel child in children)
		{
			if (string.IsNullOrEmpty(child.repeatTaskId) || !(child.repeatTaskId != child.id) || !children.Any((TaskModel c) => c.id == child.repeatTaskId))
			{
				child._Id = 0;
				string oldId = child.id;
				child.id = Utils.GetGuid();
				child.repeatTaskId = string.Empty;
				dict[oldId] = child.id;
				child.AddPinnedSecond();
				List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(oldId);
				if (list != null && list.Any())
				{
					await Utils.CopyReminderItem(child, list.ToArray());
				}
				await Utils.CopyAttachmentItem(child, oldId);
				await ProjectCopyManager.CopyChecklistItems(oldId, child.id, keepStatus: false);
				copyChildren.Add(child);
			}
		}
		foreach (TaskModel item in copyChildren)
		{
			if (!string.IsNullOrEmpty(item.parentId) && dict.ContainsKey(item.parentId))
			{
				item.parentId = dict[item.parentId];
			}
		}
		await TaskDao.BatchInsertTasks(copyChildren);
		return copyChildren;
	}

	public static async Task UncheckAllSubTasks(TaskModel task)
	{
		List<TaskModel> list = await GetAllSubTasksByIdAsync(task.id, task.projectId);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskModel child in list)
		{
			bool flag = await TaskDao.UncheckAllCheckItems(child.id);
			if (child.status != 0)
			{
				child.status = 0;
				child.completedTime = null;
				flag = true;
			}
			if (child.progress != 0)
			{
				child.progress = 0;
				flag = true;
			}
			if (child.remindTime.HasValue)
			{
				child.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(child.id, "task");
				flag = true;
			}
			if (flag)
			{
				await UpdateTaskOnStatusChanged(child);
				SyncStatusDao.AddModifySyncStatus(child.id);
			}
		}
	}

	public static bool IsAllRepeatTask(List<string> taskIds)
	{
		if (taskIds == null || taskIds.Count == 0)
		{
			return false;
		}
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(taskIds);
		if (tasksByIds != null && tasksByIds.Any())
		{
			return tasksByIds.All((TaskBaseViewModel t) => t.Status == 0 && !IsNonRepeatTask(t));
		}
		return true;
	}

	public static bool IsNonRepeatTask(TaskBaseViewModel vm)
	{
		if (vm == null || vm.Status != 0)
		{
			return true;
		}
		List<string> exDates = ((vm.ExDates == null) ? new List<string>() : ExDateSerilizer.ToArray(vm.ExDates)?.ToList());
		return RepeatUtils.IsNonRepeatTask(vm.StartDate, vm.IsAllDay, vm.RepeatFlag, vm.RepeatFrom, vm.TimeZoneName, vm.IsFloating, exDates);
	}

	public static bool IsNonRepeatTask(TaskModel taskModel)
	{
		if (taskModel == null || taskModel.status != 0)
		{
			return true;
		}
		List<string> exDates = ((taskModel.exDate == null) ? new List<string>() : taskModel.exDate.ToList());
		return RepeatUtils.IsNonRepeatTask(taskModel.startDate, taskModel.isAllDay, taskModel.repeatFlag, taskModel.repeatFrom, taskModel.timeZone, taskModel.Floating, exDates);
	}

	public static SyncBean HandleTaskBeforeSave(SyncBean syncBean)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		if (syncBean?.syncTaskBean != null)
		{
			SyncTaskBean syncTaskBean = syncBean.syncTaskBean;
			if (syncTaskBean.Add.Count > 0)
			{
				foreach (TaskModel item in syncTaskBean.Add)
				{
					ClearDueDate(item);
					item.userId = userId;
				}
			}
			if (syncTaskBean.Update.Count > 0)
			{
				foreach (TaskModel item2 in syncTaskBean.Update)
				{
					ClearDueDate(item2);
					item2.userId = userId;
				}
			}
		}
		return syncBean;
	}

	public static void ClearDueDate(TaskModel task)
	{
		if (task != null && task.startDate.HasValue && task.dueDate.HasValue)
		{
			if (task.isAllDay.HasValue && task.isAllDay.Value)
			{
				if (task.dueDate.Value.Date <= task.startDate.Value.Date)
				{
					task.dueDate = null;
				}
			}
			else if (task.startDate.Value == task.dueDate.Value)
			{
				task.dueDate = null;
			}
		}
		if (task?.items == null)
		{
			return;
		}
		TaskDetailItemModel[] items = task.items;
		foreach (TaskDetailItemModel taskDetailItemModel in items)
		{
			if (string.IsNullOrEmpty(taskDetailItemModel.serverStartDate) || !(taskDetailItemModel.serverStartDate != "-1"))
			{
				continue;
			}
			try
			{
				taskDetailItemModel.startDate = DateTime.Parse(taskDetailItemModel.serverStartDate);
				if ((taskDetailItemModel.isAllDay.HasValue && taskDetailItemModel.isAllDay.Value) || task.Floating)
				{
					taskDetailItemModel.startDate = TimeZoneUtils.LocalToTargetTzTime(taskDetailItemModel.startDate.Value, task.timeZone);
				}
			}
			catch (Exception)
			{
			}
		}
	}

	public static async Task SkipSelectRecurrence(string taskId, DateTime dateTime, IToastShowWindow toastWindow = null)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			List<string> list = ((!string.IsNullOrEmpty(task.exDates)) ? ExDateSerilizer.ToArray(task.exDates).ToList() : new List<string>());
			string item = dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			if (!list.Contains(item))
			{
				list.Add(item);
			}
			task.exDates = ExDateSerilizer.ToString(list.ToArray());
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			if (toastWindow != null && task.repeatFlag != null && task.repeatFlag.Contains("COUNT"))
			{
				toastWindow.TryToastString(null, Utils.GetString("SkipCountToast"));
			}
		}
	}

	public static async Task<TaskModel> SkipCurrentRecurrence(string taskId, bool notify = true, IToastShowWindow toastWindow = null, bool toast = true, bool handleCount = false)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			DateTime? originalStartDate = task.startDate;
			int addForgotCycle = 1;
			DateTime nextDate = RepeatUtils.GetNextRepeatDate(task, ref addForgotCycle);
			if (!Utils.IsEmptyDate(nextDate) && originalStartDate.HasValue)
			{
				task.startDate = nextDate;
				task.modifiedTime = DateTime.Now;
				task.progress = null;
				if (task.dueDate.HasValue)
				{
					int num = (int)(task.dueDate.Value - originalStartDate.Value).TotalMinutes;
					task.dueDate = nextDate.AddMinutes(num);
				}
				if (task.repeatFlag != null && task.repeatFlag.Contains("FORGETTINGCURVE"))
				{
					task.repeatFlag = RepeatUtils.GetNextEbbinghausCycle(task.repeatFlag, addForgotCycle);
				}
				if (handleCount)
				{
					task.repeatFlag = RRuleUtils.GetNextCount(task.repeatFlag, handleEbbingCount: false);
				}
				else if (toast && task.repeatFlag != null && task.repeatFlag.Contains("COUNT"))
				{
					(toastWindow ?? App.Window).TryToastString(null, Utils.GetString("SkipCountToast"));
				}
				List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
				if (list != null && list.Any())
				{
					int num = (int)(nextDate - originalStartDate.Value).TotalMinutes;
					List<TaskDetailItemModel> list2 = new List<TaskDetailItemModel>();
					foreach (TaskDetailItemModel item in list)
					{
						bool flag = false;
						if (item.status != 0)
						{
							item.status = 0;
							flag = true;
						}
						if (item.startDate.HasValue)
						{
							item.startDate = item.startDate.Value.AddMinutes(num);
							flag = true;
						}
						if (flag)
						{
							list2.Add(item);
						}
					}
					if (list2.Any())
					{
						await TaskDetailItemDao.BatchUpdateChecklists(list2);
					}
				}
				await UpdateTaskOnTimeChanged(task);
				await SyncStatusDao.AddModifySyncStatus(task.id);
				if (notify)
				{
					TaskChangeNotifier.NotifyTaskDateChanged(task.id);
				}
			}
		}
		return task;
	}

	public static async Task<TaskModel> SetStartDate(string taskId, DateTime newDate, bool checkWrongSet = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		TaskModel taskModel = task;
		bool noDateTask = taskModel == null || !taskModel.startDate.HasValue;
		if (task != null)
		{
			task = await ModifyTaskDate(task, newDate);
			task.modifiedTime = DateTime.Now;
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(taskId);
			if (noDateTask && task.isAllDay.HasValue && task.isAllDay.Value)
			{
				task.reminders = TimeData.GetDefaultAllDayReminders().ToArray();
				task.exDates = string.Empty;
				await SaveTaskReminders(task);
			}
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			if (checkWrongSet)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(new TimeData
				{
					StartDate = newDate
				});
			}
		}
		return task;
	}

	public static async Task SetTaskDateAfterDropHandle(string taskId, DateTime startDate, DateTime? dueDate, bool onlyEnd)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			if (!task.startDate.HasValue)
			{
				task.isAllDay = true;
			}
			if (onlyEnd)
			{
				task.dueDate = dueDate;
			}
			else
			{
				task.startDate = startDate;
				task.dueDate = dueDate;
			}
			task.modifiedTime = DateTime.Now;
			task.exDates = string.Empty;
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
		}
	}

	private static async Task<TaskModel> ModifyTaskDate(string taskId, DateTime newDate)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			await ModifyTaskDate(task, newDate);
		}
		return task;
	}

	public static async Task<TaskModel> ModifyTaskDate(TaskModel task, DateTime newDate)
	{
		if (task != null)
		{
			if (!task.startDate.HasValue)
			{
				task.startDate = newDate;
				task.dueDate = null;
				task.isAllDay = true;
			}
			TimeData timeData = new TimeData
			{
				StartDate = task.startDate,
				DueDate = task.dueDate,
				IsAllDay = task.isAllDay,
				RepeatFlag = task.repeatFlag,
				RepeatFrom = task.repeatFrom
			};
			ChangeStartDate(ref timeData, newDate);
			task.startDate = timeData.StartDate;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.dueDate = timeData.DueDate;
			task.repeatFlag = timeData.RepeatFlag;
			task.modifiedTime = DateTime.Now;
			task.exDates = string.Empty;
		}
		return task;
	}

	public static void ChangeStartDate(ref TimeData timeData, DateTime changeDate, bool changeTime = false)
	{
		if (timeData.StartDate.HasValue && !Utils.IsEmptyDate(timeData.StartDate) && (!timeData.DueDate.HasValue || Utils.IsEmptyDate(timeData.DueDate)) && !changeTime)
		{
			timeData.StartDate = ticktick_WPF.Util.DateUtils.SetDateOnly(timeData.StartDate.Value, changeDate);
		}
		else if (timeData.StartDate.HasValue && !Utils.IsEmptyDate(timeData.StartDate) && timeData.DueDate.HasValue && !Utils.IsEmptyDate(timeData.DueDate))
		{
			double totalHours = (timeData.DueDate.Value - timeData.StartDate.Value).TotalHours;
			timeData.StartDate = ticktick_WPF.Util.DateUtils.SetDateOnly(timeData.StartDate.Value, changeDate);
			timeData.DueDate = timeData.StartDate.Value.AddHours(totalHours);
		}
		else
		{
			timeData.StartDate = changeDate;
		}
		TryFixRepeatFlag(timeData);
	}

	public static void TryFixRepeatFlag(TimeData timeData, bool ignoreNew = false)
	{
		timeData.RepeatFlag = TryFixRepeatFlag(timeData.StartDate, timeData.RepeatFlag, timeData.RepeatFrom, ignoreNew);
	}

	public static string TryFixRepeatFlag(DateTime? startDate, string repeatFlag, string repeatFrom, bool ignoreNew = false)
	{
		if (repeatFrom != null && repeatFlag != null && startDate.HasValue && repeatFrom == "2")
		{
			if (repeatFlag.Contains("RRULE") && !repeatFlag.Contains("TT_"))
			{
				RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(repeatFlag);
				switch (recurrenceModel.Frequency)
				{
				case FrequencyType.Monthly:
					if (!ignoreNew && recurrenceModel.Interval == 1 && recurrenceModel.ByMonthDay.Count == 1)
					{
						recurrenceModel.ByMonthDay = new List<int> { startDate.Value.Day };
					}
					break;
				case FrequencyType.Weekly:
					if (!ignoreNew && recurrenceModel.Interval == 1 && recurrenceModel.ByDay.Count == 1)
					{
						recurrenceModel.ByDay = new List<WeekDay>
						{
							new WeekDay(startDate.Value.DayOfWeek)
						};
					}
					break;
				case FrequencyType.Yearly:
					if (!ignoreNew && recurrenceModel.Interval == 1 && recurrenceModel.ByMonth.Count == 1 && recurrenceModel.ByMonthDay.Count == 1)
					{
						recurrenceModel.ByMonth = new List<int> { startDate.Value.Month };
						recurrenceModel.ByMonthDay = new List<int> { startDate.Value.Day };
					}
					break;
				}
				repeatFlag = "RRULE:" + RecurrenceModel.Normalize(recurrenceModel);
			}
			else if (!repeatFlag.Contains("RRULE") && !ignoreNew)
			{
				CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(repeatFlag);
				if (repeatFlagType != CalendarConvertType.Unknown)
				{
					repeatFlag = RepeatUtils.SetSubCalendarRepeatStartDate(repeatFlagType, repeatFlag, startDate.Value);
				}
			}
			if (repeatFlag.Contains("UNTIL="))
			{
				repeatFlag = repeatFlag.Replace("T000000", "");
			}
		}
		return repeatFlag;
	}

	public static async Task DeleteAgenda(string taskId, string projectId, string attendId)
	{
		await DeleteTask(taskId);
		await Communicator.DeleteTaskAttend(projectId, taskId);
	}

	public static async Task DeleteTask(string taskId, int status = 1)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			taskModel.deleted = status;
			taskModel.attendId = null;
			taskModel.modifiedTime = DateTime.Now;
			await UpdateTaskOnDeletedChanged(taskModel);
			if (status == 1)
			{
				UtilLog.Info("TaskService.DeleteTask : " + taskId);
				TaskChangeNotifier.NotifyTaskDeleted(taskId);
				await SyncStatusDao.AddDeleteSyncStatus(taskId);
			}
			else
			{
				UtilLog.Info("TaskService.DeleteTaskForever : " + taskId);
				List<string> taskIds = new List<string> { taskId };
				TaskChangeNotifier.NotifyTaskDeleted(taskId);
				await SyncStatusDao.BatchAddDeleteForeverSyncStatus(taskIds);
			}
		}
		SyncManager.TryDelaySync();
	}

	public static async Task UndoDeletedTask(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			if (task.deleted != 2)
			{
				await SyncStatusDao.RemoveDeleteSyncStatus(taskId);
			}
			else
			{
				await SyncStatusDao.RemoveDeleteForeverSyncStatus(taskId);
			}
			task.deleted = 0;
			task.modifiedTime = DateTime.Now;
			await UpdateTaskOnDeletedChanged(task);
			UtilLog.Info("TaskService.UndoDeletedTask : " + taskId);
			TaskChangeNotifier.NotifyDeleteUndo(taskId);
		}
	}

	public static async Task UndoDeletedCheckItem(TaskDetailItemModel model)
	{
		if (model != null)
		{
			await TaskDetailItemDao.InsertChecklistItem(model);
			if (await TaskDao.GetThinTaskById(model.TaskServerId) != null)
			{
				await SyncTaskChange(model);
				UtilLog.Info("TaskService.UndoDeletedCheckItem : " + model.id);
				ItemChangeNotifier.NotifyDeletedUndo(model);
			}
		}
	}

	public static List<TaskBaseViewModel> GetInterSetOfModels(List<TaskBaseViewModel> first, List<TaskBaseViewModel> second)
	{
		if (second == null)
		{
			return first;
		}
		List<string> interSetIds = first.Select((TaskBaseViewModel t) => t.Id).ToList().Intersect(second.Select((TaskBaseViewModel t) => t.Id).ToList())
			.ToList();
		if (interSetIds.Count <= 0)
		{
			return new List<TaskBaseViewModel>();
		}
		return first.Where((TaskBaseViewModel t) => interSetIds.Contains(t.Id)).ToList();
	}

	public static List<TaskBaseViewModel> GetUnionOfModels(List<TaskBaseViewModel> first, List<TaskBaseViewModel> second)
	{
		List<string> second2 = first.Select((TaskBaseViewModel t) => t.Id).ToList();
		List<string> first2 = second.Select((TaskBaseViewModel t) => t.Id).ToList();
		List<string> exceptFirstIds = first2.Except(second2).ToList();
		List<TaskBaseViewModel> second3 = second.Where((TaskBaseViewModel t) => exceptFirstIds.Contains(t.Id)).ToList();
		return first.Union(second3).ToList();
	}

	public static bool IsAdvanceModelValid(List<CardViewModel> cards)
	{
		if (cards.Count == 4)
		{
			CardViewModel cardViewModel = cards[1];
			if (cardViewModel != null && cardViewModel.Type == CardType.LogicAnd && cards[0] is DateCardViewModel dateCardViewModel && cards[2] is DateCardViewModel dateCardViewModel2 && ((dateCardViewModel.Values.Contains("today") && dateCardViewModel2.Values.Contains("tomorrow")) || (dateCardViewModel2.Values.Contains("today") && dateCardViewModel.Values.Contains("tomorrow"))))
			{
				return false;
			}
		}
		return true;
	}

	public static async Task SetAssignee(string taskId, string assignee, bool notify = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null && task.assignee != assignee)
		{
			task.assignee = assignee;
			task.modifiedTime = DateTime.Now;
			Communicator.SetTaskAssign(new List<AssignModel>
			{
				new AssignModel
				{
					assignee = assignee,
					projectId = task.projectId,
					taskId = task.id
				}
			});
			await UpdateTaskOnAssigneeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (notify)
			{
				TaskChangeNotifier.NotifyTaskAssigneeChanged(new List<string> { taskId }, assignee);
			}
		}
	}

	public static async Task SetSortOrder(string taskId, long sortOrder, string columnId = "")
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.sortOrder = sortOrder;
			task.modifiedTime = DateTime.Now;
			if (!string.IsNullOrEmpty(columnId))
			{
				task.columnId = columnId;
			}
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.SortOrder = task.sortOrder;
			taskBaseViewModel.ColumnId = task.columnId;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	public static async Task SetPriority(string taskId, int priority, string columnId = "", bool notify = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null && !(task.kind == "NOTE"))
		{
			task.priority = priority;
			if (!string.IsNullOrEmpty(columnId))
			{
				task.columnId = columnId;
			}
			task.modifiedTime = DateTime.Now;
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Priority = task.priority;
			viewModel.ColumnId = task.columnId;
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.OnlyFilter);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (notify)
			{
				TaskChangeNotifier.NotifyTaskPriorityChanged(taskId, priority);
			}
		}
	}

	public static async Task CopyTasks(List<string> ids)
	{
		List<TaskModel> list = await TaskDao.GetThinTasksInBatch(TaskDao.GetTreeTopIds(ids, null));
		if (list == null)
		{
			return;
		}
		list.Sort((TaskModel a, TaskModel b) => b.sortOrder.CompareTo(a.sortOrder));
		List<string> newTaskIds = new List<string>();
		foreach (TaskModel item in list)
		{
			TaskModel taskModel = await CopyTask(item.id);
			if (taskModel != null)
			{
				newTaskIds.Add(taskModel.id);
			}
		}
		TaskChangeNotifier.NotifyTasksCopied(newTaskIds);
	}

	public static async Task<List<TaskModel>> CopyTasks(List<string> taskIds, DependencyObject owner)
	{
		return await CopyTasksInternal(taskIds, owner);
	}

	private static async Task<List<TaskModel>> CopyTasksInternal(List<string> taskIds, DependencyObject owner, bool notify = true, bool delayNotify = false)
	{
		List<TaskModel> copyModels = new List<TaskModel>();
		LocalSettings settings = LocalSettings.Settings;
		bool num = await CheckIfTaskHasCompletedSubTasksOrCheckItems(taskIds);
		string copyNotShowDateStamp = settings.ExtraSettings.CopyNotShowDateStamp;
		if (num && DateTime.Today.ToString("yyyyMMdd", App.Ci) != copyNotShowDateStamp)
		{
			bool firstShowCopyDialog = settings.ExtraSettings.FirstShowCopyDialog;
			if (firstShowCopyDialog)
			{
				settings.ExtraSettings.FirstShowCopyDialog = false;
				settings.Save();
			}
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("CopyTask"), Utils.GetString("duplicate_task_keep_subtask_checkitem"), Utils.GetString("keep"), Utils.GetString("dont_keep"), Window.GetWindow(owner))
			{
				PositiveCheckbox = 
				{
					Visibility = (firstShowCopyDialog ? Visibility.Collapsed : Visibility.Visible),
					Content = Utils.GetString("no_reminders_for_today")
				}
			};
			if (customerDialog.ShowDialog() ?? false)
			{
				copyModels = await HandleKeepResults(taskIds, customerDialog, settings, keepStatus: true, notify, delayNotify);
			}
			else if (customerDialog.YesNoCancelResult == false)
			{
				copyModels = await HandleKeepResults(taskIds, customerDialog, settings, keepStatus: false, notify, delayNotify);
			}
		}
		else
		{
			copyModels = await PerformCopyTasks(taskIds, LocalSettings.Settings.ExtraSettings.CopyKeepStatus, notify, delayNotify);
		}
		return copyModels;
	}

	public static async Task<TaskModel> CopyTask(string taskId, DependencyObject owner, bool notify = true, bool delayNotify = false)
	{
		List<TaskModel> list = await CopyTasksInternal(new List<string> { taskId }, owner, notify, delayNotify);
		if (list != null && list.Count > 0)
		{
			return list[0];
		}
		return null;
	}

	private static async Task<List<TaskModel>> HandleKeepResults(List<string> taskIds, CustomerDialog window, LocalSettings settings, bool keepStatus, bool notify = true, bool delayNotify = false)
	{
		if (window.PositiveCheckbox.IsChecked == true)
		{
			settings.ExtraSettings.CopyNotShowDateStamp = DateTime.Today.ToString("yyyyMMdd");
		}
		settings.ExtraSettings.CopyKeepStatus = keepStatus;
		settings.Save();
		return await PerformCopyTasks(taskIds, keepStatus, notify, delayNotify);
	}

	private static async Task<List<TaskModel>> PerformCopyTasks(List<string> taskIds, bool keepCompletedStatus = false, bool notify = true, bool delayNotify = false)
	{
		List<TaskModel> result = new List<TaskModel>();
		foreach (string taskId in taskIds)
		{
			TaskModel taskModel = await CopyTask(taskId, notify, delayNotify, keepCompletedStatus);
			UtilLog.Info("ListItem.Copy task " + taskId + ",copy " + taskModel.id);
			result.Add(taskModel);
		}
		return result;
	}

	private static async Task<bool> CheckIfTaskHasCompletedSubTasksOrCheckItems(List<string> taskIds)
	{
		foreach (string taskId in taskIds)
		{
			if (await CheckIfTaskHasCompletedSubTasksOrCheckItems(taskId))
			{
				return true;
			}
		}
		return false;
	}

	private static async Task<bool> CheckIfTaskHasCompletedSubTasksOrCheckItems(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetFullTaskById(taskId);
		if (taskModel != null)
		{
			if (taskModel.kind == "CHECKLIST" && taskModel.items != null && taskModel.items.Length != 0 && taskModel.items.Any((TaskDetailItemModel it) => it.status != 0))
			{
				return true;
			}
			List<TaskModel> list = await TaskDao.GetAllSubTasksById(taskModel.id, taskModel.projectId);
			if (list != null && list.Count > 0 && list.Any((TaskModel it) => it.status != 0))
			{
				return true;
			}
		}
		return false;
	}

	private static async Task<TaskModel> CopyTask(string taskId, bool notify = true, bool delayNotify = false, bool keepOriginalStatus = false)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return null;
		}
		long nextTaskSortOrderInProject = ProjectSortOrderDao.GetNextTaskSortOrderInProject(task.projectId, task.sortOrder, task.parentId, top: true);
		TaskModel copy = new TaskModel(task)
		{
			sortOrder = nextTaskSortOrderInProject
		};
		if (!string.IsNullOrEmpty(task.parentId))
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(task.parentId);
			if (taskModel != null && taskModel.status != 0)
			{
				await SetTaskStatus(taskModel, 0, playSound: false);
			}
		}
		TaskModel copyModel = await ProjectCopyManager.CopyTask(copy, task.projectId, Utils.GetGuid(), checkParent: true, keepOriginalStatus);
		if (copyModel == null)
		{
			return null;
		}
		List<string> list = await ProjectCopyManager.TryCopySubTasks(copy.id, copyModel.id, task.projectId, 0, keepOriginalStatus);
		if (list.Any())
		{
			UtilLog.Info("CopyTaskChildren " + taskId + ",Children " + list.Join(";"));
		}
		AttachmentCache.ResetDictItems();
		if (notify)
		{
			list.Add(copyModel.id);
			TaskChangeNotifier.NotifyTaskCopied(list, delayNotify);
		}
		return copyModel;
	}

	public static async Task SetSnoozeTime(string taskId, DateTime snoozeTime)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.remindTime = snoozeTime;
			task.modifiedTime = DateTime.Now;
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.RemindTime = task.remindTime;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			await ResetTaskReminderTimes(task);
			TaskChangeNotifier.NotifyTaskDateChanged(taskId);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	private static async Task ResetTaskReminderTimes(TaskModel task)
	{
		if (task != null && ABTestManager.IsNewRemindCalculate())
		{
			List<TaskReminderModel> reminderModels = await TaskReminderDao.GetRemindersByTaskId(task.id);
			List<ReminderTimeModel> reminderTimeModels = ((!string.IsNullOrEmpty(task.repeatFlag)) ? TaskReminderCalculator.GetRepeatTaskReminders(task, reminderModels) : TaskReminderCalculator.GetTaskReminders(task, reminderModels));
			await ReminderTimeDao.DeleteReminderTimeByIdAndType(task.id, 0);
			await ReminderTimeDao.AddReminderTimes(reminderTimeModels);
		}
	}

	public static async Task<bool> TryMoveProject(MoveProjectArgs args, bool checkLimit = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(args.TaskId);
		if (task == null)
		{
			return false;
		}
		MoveProjectArgs moveProjectArgs = args;
		moveProjectArgs.ColumnId = await ResolveAddOrMoveColumnId(args.Project?.id, args.ColumnId);
		bool sameProject = task.projectId == args.Project.id;
		bool sameColumn = task.columnId == args.ColumnId || string.IsNullOrEmpty(args.ColumnId);
		List<string> ids = ExpandProjectMoveTaskIds(new List<string> { args.TaskId });
		bool needMoveProject = !sameProject || TaskCache.GetTasksByIds(ids).Any((TaskBaseViewModel t) => t.ProjectId != args.Project.id);
		if (sameColumn && !needMoveProject)
		{
			return true;
		}
		if (checkLimit && needMoveProject)
		{
			int num = (await GetSelectedTasks(ids)).Count((TaskModel t) => t.projectId != args.Project.id && t.status == 0);
			bool flag = num > 0;
			if (flag)
			{
				flag = await ProChecker.CheckTaskLimit(args.Project.id, num);
			}
			if (flag)
			{
				return false;
			}
			checkLimit = false;
		}
		if (!sameProject || !sameColumn)
		{
			await TaskDao.RemoveTaskParentId(task.id);
			task.parentId = "";
		}
		if (ids.Count > 1)
		{
			if (needMoveProject && !(await BatchMoveProject(ids, args, checkLimit)))
			{
				return false;
			}
			if (!sameColumn)
			{
				await BatchSaveTasksColumnId(ids, args.ColumnId, args.Project.id);
			}
		}
		else
		{
			if (!sameProject)
			{
				TaskModel taskModel = await MoveProject(args, checkLimit);
				if (taskModel == null)
				{
					return false;
				}
				task = taskModel;
			}
			if (!sameColumn)
			{
				if (sameProject)
				{
					task.sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(task.projectId);
				}
				task.columnId = args.ColumnId;
				await UpdateTaskColumnId(task);
				TaskChangeNotifier.NotifyTaskProjectChanged(task);
			}
		}
		await TaskDao.RemoveTaskParentId(task.id);
		task.parentId = "";
		SyncManager.TryDelaySync(2000);
		return true;
	}

	private static async Task<TaskModel> MoveProject(MoveProjectArgs args, bool checkLimit = true)
	{
		if (checkLimit && await ProChecker.CheckTaskLimit(args.Project.id))
		{
			return null;
		}
		TaskModel task = await TaskDao.GetThinTaskById(args.TaskId);
		if (task != null && task.projectId != args.Project.id)
		{
			string fromProjectId = task.projectId;
			task.deleted = 0;
			task.projectId = args.Project.id;
			TaskModel taskModel = task;
			taskModel.columnId = await ResolveAddOrMoveColumnId(args.Project.id, args.ColumnId);
			string oldAssign = task.assignee;
			long oldSort = task.sortOrder;
			if (!args.KeepAssignee)
			{
				task.assignee = "-1";
			}
			task.sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(args.Project.id, null, args.IsTop);
			CommentDao.ChangeCommentProjectId(task, args.Project.id);
			await UpdateTaskOnProjectChanged(task);
			if (!string.IsNullOrEmpty(args.UndoId))
			{
				TaskDragUndoModel.AddDragModel(args.UndoId, task.id, null, null, fromProjectId, args.Project.id, oldAssign, oldSort);
			}
			else
			{
				await SyncStatusDao.AddMoveOrRestoreProjectStatus(task.id, fromProjectId);
				if (oldAssign != task.assignee && !string.IsNullOrEmpty(oldAssign))
				{
					await SyncStatusDao.AddModifySyncStatus(task.id);
				}
			}
			if (args.Notify)
			{
				TaskChangeNotifier.NotifyTaskProjectChanged(task);
			}
		}
		return task;
	}

	public static async Task MoveProject(string taskId, string projectId, bool? isTop = null, string columnId = null)
	{
		ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
		if (projectModel != null)
		{
			await TryMoveProject(new MoveProjectArgs(taskId, projectModel)
			{
				IsTop = isTop,
				ColumnId = columnId
			});
		}
	}

	public static async Task ClearAgendaDate(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.isAllDay = true;
			task.startDate = null;
			task.dueDate = null;
			task.reminders = null;
			task.repeatFrom = string.Empty;
			task.repeatFlag = string.Empty;
			task.annoyingAlert = -1;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.exDates = string.Empty;
			task.modifiedTime = DateTime.Now;
			task.attendId = string.Empty;
			task.isFloating = false;
			task.timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			await Communicator.RemoveAgenda(task.projectId, task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
		}
	}

	public static async Task ClearDate(string taskId, string columnId = "")
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.isAllDay = true;
			task.startDate = null;
			task.dueDate = null;
			task.reminders = null;
			task.repeatFrom = string.Empty;
			task.repeatFlag = string.Empty;
			task.annoyingAlert = -1;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.exDates = string.Empty;
			task.modifiedTime = DateTime.Now;
			task.isFloating = false;
			task.timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
			if (!string.IsNullOrEmpty(columnId))
			{
				task.columnId = columnId;
			}
			if (!string.IsNullOrEmpty(task.attendId))
			{
				task.attendId = "";
				Communicator.RemoveAgenda(task.projectId, taskId);
			}
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
		}
	}

	public static async Task BatchSetDate(List<string> taskIds, TimeData model, bool notEditFlag = false)
	{
		List<TaskModel> tasks = await GetSelectedTasks(taskIds);
		for (int i = 0; i < tasks.Count; i++)
		{
			TaskModel task = tasks[i];
			if (!task.startDate.HasValue)
			{
				task.isAllDay = true;
			}
			bool flag = task.kind == "NOTE";
			bool flag2 = task.isAllDay != true && model.IsAllDay == true;
			if (model.ChangedDateOnly)
			{
				if (model.IsTimeUnified)
				{
					task.startDate = model.StartDate;
					task.dueDate = (flag ? ((DateTime?)null) : model.DueDate);
				}
				else
				{
					DateTime? startDate = task.startDate;
					task.startDate = ((!task.startDate.HasValue || !model.StartDate.HasValue) ? model.StartDate : new DateTime?(ticktick_WPF.Util.DateUtils.SetDateOnly(task.startDate.Value, model.StartDate.Value)));
					if (startDate.HasValue && task.dueDate.HasValue)
					{
						double totalMinutes = (task.dueDate.Value - startDate.Value).TotalMinutes;
						task.dueDate = task.startDate?.AddMinutes(totalMinutes);
					}
				}
				if (!notEditFlag)
				{
					task.repeatFlag = TryFixRepeatFlag(task.startDate, task.repeatFlag, task.repeatFrom);
				}
				continue;
			}
			task.startDate = model.StartDate;
			task.dueDate = (flag ? ((DateTime?)null) : model.DueDate);
			task.isAllDay = model.IsAllDay;
			task.repeatFrom = (flag ? null : model.RepeatFrom);
			task.repeatFlag = (flag ? null : model.RepeatFlag);
			task.remindTime = task.remindTime;
			task.modifiedTime = DateTime.Now;
			task.exDates = string.Empty;
			task.repeatFlag = TryFixRepeatFlag(task.startDate, task.repeatFlag, task.repeatFrom);
			task.isFloating = !flag2 && model.TimeZone.IsFloat;
			task.timeZone = (flag2 ? TimeZoneData.LocalTimeZoneModel.TimeZoneName : model.TimeZone.TimeZoneName);
			if (model.AnnoyingAlert.HasValue)
			{
				task.annoyingAlert = model.AnnoyingAlert;
			}
			await TaskReminderDao.DeleteRemindersByTaskId(task.id);
			List<TaskReminderModel> reminders = new List<TaskReminderModel>();
			if (model.Reminders != null)
			{
				foreach (TaskReminderModel reminder in model.Reminders)
				{
					TaskReminderModel taskReminderModel = new TaskReminderModel();
					taskReminderModel.Taskid = task._Id;
					taskReminderModel.taskserverid = task.id;
					taskReminderModel.id = Utils.GetGuid();
					taskReminderModel.trigger = reminder.trigger;
					reminders.Add(taskReminderModel);
				}
				await TaskReminderDao.InsertTaskReminders(reminders);
			}
			task.reminders = reminders.ToArray();
		}
		await BatchSaveTask(tasks, CheckMatchedType.CheckSmart);
		TaskChangeNotifier.NotifyBatchDateChanged(taskIds);
		ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(model);
	}

	public static async Task<TaskDetailItemModel> SetSubtaskDate(string taskId, string itemId, DateTime newDate, bool toAllDay = false)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null)
		{
			item.isAllDay = toAllDay || !item.startDate.HasValue || (item.isAllDay ?? true);
			item.startDate = ((!item.startDate.HasValue || toAllDay) ? newDate : ticktick_WPF.Util.DateUtils.SetDateOnly(item.startDate.Value, newDate));
			await TaskDetailItemDao.SaveChecklistItem(item);
			await SyncStatusDao.AddModifySyncStatus(taskId);
			ItemChangeNotifier.NotifyItemDateChanged(item);
		}
		return item;
	}

	public static async Task SetCheckItemDate(string taskId, string itemId, TimeDataModel newDate)
	{
		TaskDetailItemModel item = await TaskDetailItemDao.GetChecklistItemById(itemId);
		if (item != null)
		{
			item.startDate = newDate.StartDate;
			item.isAllDay = newDate.IsAllDay;
			item.snoozeReminderTime = null;
			await TaskDetailItemDao.SaveChecklistItem(item);
			await SyncStatusDao.AddModifySyncStatus(taskId);
			ItemChangeNotifier.NotifyItemDateChanged(item);
			ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(new TimeData
			{
				StartDate = newDate.StartDate,
				DueDate = newDate.DueDate
			});
		}
	}

	public static async Task SetDateTime(TimeDataModel time)
	{
		TaskModel task = await TaskDao.GetThinTaskById(time.TaskId);
		if (task != null)
		{
			task.startDate = time.StartDate;
			task.dueDate = time.DueDate;
			task.isAllDay = time.IsAllDay;
			task.exDates = string.Empty;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			if (task.isAllDay == true)
			{
				task.timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
				task.isFloating = false;
			}
			switch (time.HandleReminderMode)
			{
			case ReminderMode.Clear:
				await TaskReminderDao.DeleteRemindersByTaskId(task.id);
				task.annoyingAlert = -1;
				break;
			case ReminderMode.KeepDay:
				await KeepTaskDayReminder(time, task);
				break;
			case ReminderMode.SetDefault:
				await SetDefaultReminder(time, task);
				break;
			}
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			if (!time.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(new TimeData
				{
					StartDate = time.StartDate,
					DueDate = time.DueDate
				});
			}
		}
	}

	private static async Task KeepTaskDayReminder(TimeDataModel time, TaskModel task)
	{
		List<TaskReminderModel> original = await TaskReminderDao.GetRemindersByTaskId(task.id);
		if (original == null || original.Count <= 0)
		{
			return;
		}
		await TaskReminderDao.DeleteRemindersByTaskId(task.id);
		List<string> list = new List<string>();
		foreach (TaskReminderModel item2 in original)
		{
			AdvanceDateModel advanceDateModel = new AdvanceDateModel(item2.trigger, isAllDay: true);
			string item = $"TRIGGER:-P{advanceDateModel.AdvanceDays}D";
			if (advanceDateModel.AdvanceDays <= 0 && advanceDateModel.Hour <= 0 && advanceDateModel.Minute <= 0)
			{
				item = "TRIGGER:PT0S";
			}
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		if (list.Any())
		{
			foreach (string item3 in list)
			{
				await TaskReminderDao.SaveReminders(new TaskReminderModel
				{
					id = Utils.GetGuid(),
					_Id = task._Id,
					taskserverid = task.id,
					trigger = item3
				});
			}
		}
		else
		{
			await SetDefaultReminder(time, task);
		}
	}

	private static async Task SetDefaultReminder(TimeDataModel time, TaskModel task)
	{
		await TaskReminderDao.DeleteRemindersByTaskId(task.id);
		List<TaskReminderModel> list = ((time.IsAllDay.HasValue && time.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
		if (list == null || list.Count <= 0)
		{
			return;
		}
		list.ForEach(delegate(TaskReminderModel reminder)
		{
			reminder._Id = task._Id;
			reminder.taskserverid = task.id;
		});
		foreach (TaskReminderModel item in list)
		{
			await TaskReminderDao.SaveReminders(item);
		}
	}

	public static async Task SetDateAndClearRepeat(string taskId, DateTime date, bool handleReminder = true, bool clearRepeat = false)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (!taskModel.startDate.HasValue)
			{
				taskModel.isAllDay = true;
			}
			TimeData timeData = new TimeData
			{
				StartDate = taskModel.startDate,
				DueDate = taskModel.dueDate,
				IsAllDay = taskModel.isAllDay,
				RepeatFlag = (clearRepeat ? string.Empty : taskModel.repeatFlag),
				RepeatFrom = (clearRepeat ? string.Empty : taskModel.repeatFrom),
				ExDates = null,
				TimeZone = new TimeZoneViewModel(taskModel.Floating, taskModel.timeZone),
				AnnoyingAlert = taskModel.annoyingAlert
			};
			ChangeStartDate(ref timeData, date);
			await SetDate(taskId, timeData, handleReminder);
		}
	}

	public static async Task SetDate(string taskId, DateTime newDate, bool handleReminder = true, string columnId = "", bool? toAllDay = null)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (!taskModel.startDate.HasValue)
			{
				toAllDay = true;
			}
			TimeData timeData = new TimeData
			{
				StartDate = taskModel.startDate,
				DueDate = taskModel.dueDate,
				IsAllDay = (toAllDay ?? taskModel.isAllDay),
				RepeatFlag = taskModel.repeatFlag,
				RepeatFrom = taskModel.repeatFrom,
				ExDates = null,
				TimeZone = new TimeZoneViewModel(taskModel.Floating, taskModel.timeZone),
				AnnoyingAlert = taskModel.annoyingAlert
			};
			if (!handleReminder && !taskModel.startDate.HasValue && timeData.StartDate.HasValue)
			{
				handleReminder = true;
			}
			if (handleReminder && (taskModel.isAllDay != timeData.IsAllDay || !taskModel.startDate.HasValue))
			{
				timeData.Reminders = ((!timeData.IsAllDay.HasValue || timeData.IsAllDay.Value) ? TimeData.GetDefaultAllDayReminders() : TimeData.GetDefaultTimeReminders());
			}
			ChangeStartDate(ref timeData, newDate);
			if (toAllDay == true && !(taskModel.isAllDay ?? true))
			{
				timeData.DueDate = null;
			}
			if (timeData.IsAllDay ?? true)
			{
				timeData.StartDate = timeData.StartDate?.Date;
				timeData.DueDate = timeData.DueDate?.Date;
			}
			await SetDate(taskId, timeData, handleReminder, columnId);
		}
	}

	public static async Task SetDateSpan(string taskId, DateTime? start, DateTime? end, bool handleReminder = true, bool? isAllDay = null)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			if (!taskModel.startDate.HasValue)
			{
				taskModel.isAllDay = true;
			}
			TimeData model = new TimeData
			{
				StartDate = start,
				DueDate = end,
				IsAllDay = (isAllDay ?? taskModel.isAllDay),
				RepeatFlag = taskModel.repeatFlag,
				RepeatFrom = taskModel.repeatFrom,
				ExDates = null,
				TimeZone = new TimeZoneViewModel(taskModel.Floating, taskModel.timeZone),
				AnnoyingAlert = taskModel.annoyingAlert
			};
			await SetDate(taskId, model, handleReminder, null);
		}
	}

	public static TimeData GetChangedData(DateTime? startDate, DateTime? dueDate, bool? isAllDay, DateTime newDate, string repeatFlag, string repeatFrom, bool changeTime = false)
	{
		TimeData timeData = new TimeData
		{
			StartDate = startDate,
			DueDate = dueDate,
			IsAllDay = isAllDay,
			RepeatFlag = repeatFlag,
			RepeatFrom = repeatFrom
		};
		ChangeStartDate(ref timeData, newDate, changeTime);
		return timeData;
	}

	public static async Task SetDate(string taskId, TimeData model, bool handleReminder = true, string columnId = "", bool notify = true)
	{
		TryFixRepeatFlag(model);
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.isAllDay = model.IsAllDay;
			task.startDate = model.StartDate;
			task.dueDate = model.DueDate;
			task.reminders = model.Reminders?.ToArray();
			task.repeatFrom = model.RepeatFrom;
			task.repeatFlag = model.RepeatFlag;
			task.exDates = ExDateSerilizer.ToString(model.ExDates?.ToArray());
			task.isFloating = model.TimeZone.IsFloat;
			task.timeZone = model.TimeZone.TimeZoneName;
			task.annoyingAlert = model.AnnoyingAlert;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.modifiedTime = DateTime.Now;
			if (!string.IsNullOrEmpty(columnId))
			{
				task.columnId = columnId;
			}
			if (handleReminder)
			{
				task.reminders = model.Reminders?.ToArray();
				await SaveTaskReminders(task);
			}
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (notify)
			{
				TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			}
			if (!model.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(model);
			}
		}
	}

	public static async Task SetUntilToFlag(string taskId, DateTime? untilTime, TimeData model, bool isHolidayWeekend, bool handleReminder = true, bool notify = true)
	{
		if (!untilTime.HasValue)
		{
			return;
		}
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(task.repeatFlag);
			recurrenceModel.Until = untilTime.Value;
			recurrenceModel.Count = int.MinValue;
			task.repeatFlag = "RRULE:" + RecurrenceModel.Normalize(recurrenceModel);
			if (isHolidayWeekend)
			{
				task.repeatFlag = task.repeatFlag.Replace("T000000", ";TT_SKIP=OFFICIAL_WORKDAY");
			}
			await BaseDao<TaskModel>.UpdateAsync(task);
			if (handleReminder && model != null)
			{
				task.reminders = model.Reminders?.ToArray();
				await SaveTaskReminders(task);
			}
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (notify)
			{
				TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			}
			if (model == null || !model.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(model);
			}
		}
	}

	public static async Task UpdateNewFlagCountFromDateIndex(string taskId, DateTime? rawStartDate, DateTime? endDate, TimeData model, bool isHolidayWeekend, bool handleReminder = true, bool notify = true)
	{
		if (!endDate.HasValue || !rawStartDate.HasValue)
		{
			return;
		}
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null || string.IsNullOrEmpty(task.repeatFlag) || !task.startDate.HasValue)
		{
			return;
		}
		RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(task.repeatFlag);
		int count = recurrenceModel.Count;
		if (count <= 0)
		{
			return;
		}
		int passedCountInTotal = RepeatCalculator.GetPassedCountInTotal(task, rawStartDate.Value, endDate.Value, count);
		if (passedCountInTotal > 0 && passedCountInTotal <= count)
		{
			recurrenceModel.Count = count - passedCountInTotal;
			string text = "RRULE:" + RecurrenceModel.Normalize(recurrenceModel);
			if (isHolidayWeekend)
			{
				text += ";TT_SKIP=OFFICIAL_WORKDAY";
			}
			task.repeatFlag = text;
			await BaseDao<TaskModel>.UpdateAsync(task);
			if (handleReminder)
			{
				task.reminders = model.Reminders?.ToArray();
				await SaveTaskReminders(task);
			}
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (notify)
			{
				TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			}
			if (!model.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(model);
			}
		}
	}

	public static async Task SaveTaskReminders(TaskModel task)
	{
		await TaskReminderDao.DeleteRemindersByTaskId(task.id);
		if (task.reminders == null || task.reminders.Length == 0)
		{
			return;
		}
		HashSet<string> saved = new HashSet<string>();
		TaskReminderModel[] reminders = task.reminders;
		foreach (TaskReminderModel taskReminderModel in reminders)
		{
			if (!saved.Contains(taskReminderModel.trigger))
			{
				saved.Add(taskReminderModel.trigger);
				taskReminderModel.Taskid = task._Id;
				taskReminderModel.taskserverid = task.id;
				await TaskReminderDao.SaveReminders(taskReminderModel);
			}
		}
	}

	public static async Task SaveReminders(string taskId, List<TaskReminderModel> reminders)
	{
		await TaskReminderDao.DeleteRemindersByTaskId(taskId);
		if (reminders == null || reminders.Count <= 0)
		{
			return;
		}
		foreach (TaskReminderModel reminder in reminders)
		{
			reminder.taskserverid = taskId;
			await TaskReminderDao.SaveReminders(reminder);
		}
	}

	private static async Task<List<TaskModel>> GetSelectedTasks(List<string> selectedTaskIds)
	{
		if (selectedTaskIds != null && selectedTaskIds.Count > 0)
		{
			return await TaskDao.GetThinTasksInBatch(selectedTaskIds);
		}
		return new List<TaskModel>();
	}

	public static async Task OnCheckItemChanged(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.modifiedTime = DateTime.Now;
			await TaskDao.UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			SyncManager.TryDelaySync();
		}
	}

	public static async Task SetTags(string taskId, List<string> tags)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.modifiedTime = DateTime.Now;
			task.tags = tags.ToArray();
			task.tag = TagSerializer.ToJsonContent(tags);
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.ModifiedTime = task.modifiedTime;
			viewModel.SetTags(task.tags.ToList());
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.CheckTag);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			await Task.Delay(50);
			TaskChangeNotifier.NotifyTaskTagsChanged(new TagExtra(taskId, tags));
		}
	}

	public static async Task<bool> ReplaceTitleLink(string id, string url, string urlTitle, string originTaskTitle, bool isCheckItem)
	{
		bool success = false;
		if (isCheckItem)
		{
			TaskDetailItemModel checkItem = await TaskDetailItemDao.GetCheckItemById(id);
			if (checkItem != null && checkItem.title == originTaskTitle)
			{
				checkItem.title = checkItem.title.Replace(url, "[" + urlTitle + "](" + url + ")");
				await TaskDetailItemDao.SaveChecklistItem(checkItem);
				await SyncStatusDao.AddSyncStatus(checkItem.TaskServerId, 0);
				SyncManager.TryDelaySync();
				success = true;
			}
		}
		else
		{
			TaskModel task = await TaskDao.GetThinTaskById(id);
			if (task != null && task.title == originTaskTitle)
			{
				task.title = task.title.Replace(url, "[" + urlTitle + "](" + url + ")");
				task.modifiedTime = DateTime.Now;
				await UpdateTaskOnTitleChanged(task);
				await SyncStatusDao.AddModifySyncStatus(task.id);
				TaskChangeNotifier.NotifyTaskTextChanged(task.id);
				success = true;
			}
		}
		return success;
	}

	public static async Task SaveTaskTitle(string taskId, string title)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null && task.title != title)
		{
			task.title = title;
			task.modifiedTime = DateTime.Now;
			await UpdateTaskOnTitleChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskTextChanged(taskId);
			SyncManager.TryDelaySync(3000);
		}
	}

	public static async Task SaveTaskContent(string taskId, string content)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.content = content;
			task.modifiedTime = DateTime.Now;
			await UpdateTaskOnContentChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	public static async Task<TaskModel> AddTaskFromTemplate(string templateId, AddTaskViewModel taskDefault = null, AddTaskExtraInfo extra = null)
	{
		if (taskDefault == null)
		{
			taskDefault = new AddTaskViewModel(ProjectIdentity.CreateInboxProject());
		}
		TaskTemplateModel template = await TemplateDao.GetTemplateById(templateId);
		if (template != null)
		{
			TaskModel task = await AddTaskFromTemplate(template, taskDefault, extra);
			if (taskDefault.Files.Count > 0)
			{
				List<AttachmentModel> list = await AttachmentProvider.AddAttachmentModels(task.id, taskDefault.ClearAndGetFiles());
				if (list.Count > 0)
				{
					AttachmentProvider.AppendTaskContent(task, list);
					task.Attachments = list.ToArray();
					task.attachmentCount = list.Count;
					task.modifiedTime = DateTime.Now;
					AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() + list.Count);
					await SaveTask(task);
				}
			}
			if (task.kind != "NOTE")
			{
				await AddTaskChildrenFromTemplate(template, task.id, taskDefault);
			}
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId);
			string arg = ((projectModel == null) ? Utils.GetString("Inbox") : projectModel.name);
			Utils.Toast(string.Format(Utils.GetString("AddTaskTo"), arg));
			TaskChangeNotifier.NotifyTaskAdded(task);
			return task;
		}
		return null;
	}

	public static async Task AddTaskChildrenFromTemplate(string templateId, string taskId, AddTaskViewModel taskDefault = null)
	{
		if (!string.IsNullOrEmpty(templateId) && !string.IsNullOrEmpty(taskId))
		{
			TaskTemplateModel taskTemplateModel = await TemplateDao.GetTemplateById(templateId);
			if (taskTemplateModel != null)
			{
				await AddTaskChildrenFromTemplate(taskTemplateModel, taskId, taskDefault);
			}
		}
	}

	public static async Task AddPendingTemplateChildren(TaskDetailViewModel parent)
	{
		if (parent != null && !string.IsNullOrEmpty(parent.PendingTemplateId) && !string.IsNullOrEmpty(parent.TaskId))
		{
			AddTaskViewModel taskDefault = new AddTaskViewModel
			{
				ProjectId = parent.ProjectId,
				ColumnId = parent.ColumnId,
				AddToColumnId = parent.ColumnId,
				Priority = parent.Priority,
				TimeData = parent.GetTimeData(),
				Tags = TagSerializer.ToTags(parent.Tag)
			};
			await AddTaskChildrenFromTemplate(parent.PendingTemplateId, parent.TaskId, taskDefault);
			parent.ClearPendingTemplate();
			TaskChangeNotifier.NotifyTaskSortChanged(new string[1] { parent.TaskId });
		}
	}

	private static async Task AddTaskChildrenFromTemplate(TaskTemplateModel template, string taskId, AddTaskViewModel taskDefault = null)
	{
		List<TaskTemplateModel> chidlren = template.Chidlren;
		if (chidlren == null || chidlren.Count <= 0)
		{
			return;
		}
		long sortOrder = 0L;
		foreach (TaskTemplateModel childTemplate in template.Chidlren)
		{
			TaskModel taskModel = await AddTaskFromTemplate(childTemplate, taskDefault, null, taskId, sortOrder);
			sortOrder += 268435456;
			await AddTaskChildrenFromTemplate(childTemplate, taskModel.id, taskDefault);
		}
	}

	private static async Task<TaskModel> AddTaskFromTemplate(TaskTemplateModel model, AddTaskViewModel taskDefault = null, AddTaskExtraInfo extra = null, string parentId = null, long? sort = null)
	{
		List<string> tags = new List<string>();
		if (model.Tags != null && model.Tags.Any())
		{
			tags.AddRange(model.Tags);
		}
		if (taskDefault != null && taskDefault.Tags?.Count > 0 && string.IsNullOrEmpty(parentId))
		{
			tags.AddRange(taskDefault.Tags);
		}
		bool isList = model.Items != null && model.Items.Any();
		bool isNote = model.Kind == TemplateKind.Note.ToString();
		TaskModel task = new TaskModel
		{
			id = Utils.GetGuid(),
			title = model.Title,
			content = model.Content,
			kind = (isNote ? "NOTE" : (isList ? "CHECKLIST" : "TEXT")),
			desc = model.Desc,
			status = 0,
			projectId = Utils.GetInboxId(),
			modifiedTime = DateTime.Now,
			createdTime = DateTime.Now,
			sortOrder = sort.GetValueOrDefault()
		};
		if (taskDefault != null)
		{
			task.projectId = taskDefault.ProjectId;
			string preferredColumnId = ((!string.IsNullOrEmpty(taskDefault.AddToColumnId)) ? taskDefault.AddToColumnId : taskDefault.ColumnId);
			TaskModel taskModel = task;
			taskModel.columnId = await ResolveAddOrMoveColumnId(task.projectId, preferredColumnId);
			task.priority = ((!isNote && string.IsNullOrEmpty(parentId)) ? taskDefault.Priority : 0);
			if (taskDefault.TimeData != null && string.IsNullOrEmpty(parentId))
			{
				task.startDate = taskDefault.TimeData.StartDate;
				task.dueDate = (isNote ? ((DateTime?)null) : taskDefault.TimeData.DueDate);
				task.isAllDay = taskDefault.TimeData.IsAllDay;
				task.repeatFlag = (isNote ? null : taskDefault.TimeData.RepeatFlag);
				task.repeatFrom = (isNote ? null : taskDefault.TimeData.RepeatFrom);
				if (taskDefault.TimeData.Reminders != null && taskDefault.TimeData.Reminders.Count > 0)
				{
					task.reminders = taskDefault.TimeData.Reminders.ToArray();
					task.reminderCount = (task.reminders?.Length).Value;
					TaskReminderModel[] reminders = task.reminders;
					foreach (TaskReminderModel obj in reminders)
					{
						obj.id = Utils.GetGuid();
						obj.taskserverid = task.id;
						await TaskReminderDao.SaveReminders(obj);
					}
				}
			}
		}
		if (extra != null)
		{
			if (!string.IsNullOrEmpty(extra.Title.Trim()))
			{
				task.title = extra.Title.Trim();
			}
			if (!string.IsNullOrEmpty(extra.Content.Trim()))
			{
				if (task.kind == "CHECKLIST")
				{
					string desc = task.desc;
					string text = extra.Content.Trim();
					if (!string.IsNullOrEmpty(desc))
					{
						task.desc = text + "\n" + desc;
					}
					else
					{
						task.desc = text;
					}
				}
				else
				{
					string content = task.content;
					string text2 = extra.Content.Trim();
					if (!string.IsNullOrEmpty(content))
					{
						task.content = text2 + "\n" + content;
					}
					else
					{
						task.content = text2;
					}
				}
			}
			task.assignee = extra.AssigneeId;
			if (extra.Tags != null && extra.Tags.Any())
			{
				tags.AddRange(extra.Tags);
			}
		}
		tags = TagSerializer.NormalizeLabels(tags);
		if (tags.Any())
		{
			List<string> localTags = (from tag in CacheManager.GetTags()
				select tag.name).ToList();
			List<string> list = tags.Where((string tag) => !localTags.Contains(tag.ToLower())).ToList();
			if (list.Any())
			{
				foreach (string item in list)
				{
					await TagService.TryCreateTag(item);
				}
			}
		}
		task.tag = TagSerializer.ToJsonContent(tags);
		task.parentId = parentId;
		if (taskDefault != null)
		{
			if (taskDefault.IsPin)
			{
				task.pinnedTime = DateTime.Now.ToString(UtcDateTimeConverter.GetConverterValue(DateTime.Now));
			}
			if (taskDefault.IsComplete)
			{
				task.status = 2;
				task.completedTime = DateTime.Now;
				task.completedUserId = LocalSettings.Settings.LoginUserId;
			}
		}
		if (isList)
		{
			List<string> items = model.Items;
			if (items != null && items.Count > 0)
			{
				long sortOrder = 0L;
				foreach (string item2 in model.Items)
				{
					TaskDetailItemModel model2 = new TaskDetailItemModel
					{
						id = Utils.GetGuid(),
						TaskId = 0,
						TaskServerId = task.id,
						title = item2,
						sortOrder = sortOrder
					};
					sortOrder += 268435456;
					await TaskDetailItemDao.InsertChecklistItem(model2);
				}
			}
		}
		return await AddTask(task, !sort.HasValue, notify: false);
	}

	public static async Task<TaskModel> AddTask(TaskModel task, bool setSortOrder = true, bool notify = true, object sender = null)
	{
		if (task == null)
		{
			return null;
		}
		if (!task.createdTime.HasValue)
		{
			task.createdTime = DateTime.Now;
		}
		if (task.creator == null)
		{
			task.creator = LocalSettings.Settings.LoginUserId;
		}
		if (!task.modifiedTime.HasValue)
		{
			task.modifiedTime = DateTime.Now;
		}
		if (string.IsNullOrEmpty(task.id))
		{
			task.id = Utils.GetGuid();
		}
		if (string.IsNullOrEmpty(task.projectId))
		{
			task.projectId = TaskDefaultDao.GetDefaultSafely().ProjectId;
		}
		if (string.IsNullOrEmpty(task.userId))
		{
			task.userId = Utils.GetCurrentUserStr();
		}
		if (string.IsNullOrEmpty(task.timeZone))
		{
			task.timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName;
		}
		if (KanbanHelper.IsProjectInKanbanMode(task.projectId) && string.IsNullOrEmpty(task.columnId))
		{
			task.columnId = await ColumnDao.GetProjectDefaultColumnId(task.projectId);
		}
		PrepareTaskForSave(task);
		await TaskDao.InsertTask(task);
		await SyncStatusDao.AddCreateSyncStatus(task.id);
		UtilLog.Info("TaskService.AddTask : " + task.id);
		if (!string.IsNullOrEmpty(task.parentId))
		{
			await TaskDao.AddOrRemoveTaskChildIds(task.parentId, new List<string> { task.id }, addChild: true);
			await SyncStatusDao.AddSetParentSyncStatus(task.id, "");
		}
		if (notify)
		{
			TaskChangeNotifier.NotifyTaskAdded(task);
		}
		return task;
	}

	public static async Task<TaskModel> SaveTask(TaskModel task)
	{
		if (task == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(task.userId))
		{
			task.userId = Utils.GetCurrentUserIdInt().ToString();
		}
		if (string.IsNullOrEmpty(task.timeZone))
		{
			task.timeZone = Utils.GetLocalTimeZone();
		}
		await UpdateTask(task);
		await SyncStatusDao.AddModifySyncStatus(task.id);
		return task;
	}

	public static void PrepareTaskForSave(TaskModel task)
	{
		if (!(task?.kind != "NOTE") && task.reminders != null)
		{
			task.reminders = task.reminders.Where((TaskReminderModel reminder) => reminder != null && !reminder.IsEnd()).ToArray();
			task.reminderCount = task.reminders.Length;
		}
	}

	public static async Task<List<TaskModel>> AddSubTasks(TaskBaseViewModel parent, params TaskModel[] tasks)
	{
		if (await ProChecker.CheckTaskLimit(parent.ProjectId))
		{
			return null;
		}
		List<TaskModel> list = new List<TaskModel>();
		TaskDefaultModel defaultTask = TaskDefaultDao.GetDefaultSafely();
		foreach (TaskModel taskModel in tasks)
		{
			TaskBaseViewModel newTask = new TaskBaseViewModel
			{
				Id = (string.IsNullOrEmpty(taskModel.id) ? Utils.GetGuid() : taskModel.id),
				ParentId = parent.Id,
				ProjectId = parent.ProjectId,
				IsAllDay = true,
				Kind = "TEXT",
				Title = taskModel.title,
				Priority = defaultTask.Priority,
				Status = taskModel.status,
				Type = DisplayType.Task,
				ColumnId = parent.ColumnId
			};
			if (newTask.Status != 0)
			{
				newTask.CompletedTime = DateTime.Now;
				newTask.CompletedUser = LocalSettings.Settings.LoginUserId;
			}
			if (parent.IsPinned)
			{
				newTask.PinnedTime = Utils.GetNowTimeStamp();
			}
			newTask.StartDate = taskModel.startDate ?? defaultTask.GetDefaultDateTime();
			newTask.SortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(newTask.ProjectId, newTask.ParentId, false);
			newTask.CreatedTime = DateTime.Now;
			if (newTask.StartDate.HasValue)
			{
				List<TaskReminderModel> defaultAllDayReminders = TimeData.GetDefaultAllDayReminders();
				foreach (TaskReminderModel item in defaultAllDayReminders)
				{
					item.taskserverid = newTask.Id;
					await TaskReminderDao.SaveReminders(item);
				}
			}
			TaskCache.AddToDict(newTask);
			DisplayItemModel displayItemModel = new DisplayItemModel(newTask);
			UtilLog.Info("AddSubTask " + newTask.Id);
			list.Add(await AddTask(displayItemModel.GetTaskModel()));
		}
		TaskChangeNotifier.NotifyTaskTextChanged(parent.Id);
		return list;
	}

	public static async Task DeleteAllSubTasks(TaskModel task)
	{
		List<TaskModel> list = await GetAllSubTasksByIdAsync(task.id, task.projectId);
		if (list != null && list.Count > 0)
		{
			await BatchDeleteTasks(list, showUndo: false);
		}
	}

	public static async Task DeletePomodoro(string pomoId, List<PomoTask> pomoTasks, bool isPomo, bool isDelete = false)
	{
		Dictionary<string, long> taskDict = new Dictionary<string, long>();
		foreach (PomoTask pomoTask in pomoTasks)
		{
			long totalSecond = Utils.GetTotalSecond(pomoTask.StartTime, pomoTask.EndTime);
			if (!string.IsNullOrEmpty(pomoTask.TaskId))
			{
				if (taskDict.ContainsKey(pomoTask.TaskId))
				{
					taskDict[pomoTask.TaskId] += totalSecond;
				}
				else
				{
					taskDict.Add(pomoTask.TaskId, totalSecond);
				}
			}
		}
		foreach (KeyValuePair<string, long> kv in taskDict)
		{
			TaskModel task = await TaskDao.GetThinTaskById(kv.Key);
			if (task != null)
			{
				long num = -1 * kv.Value;
				int num2 = ((Math.Abs(num) >= Constants.MinFocusDuration) ? 1 : 0);
				num2 *= ((!isDelete) ? 1 : (-1));
				PomodoroSummaryModel pomo = await DeletePomoSummary(pomoId, kv.Key, num, isPomo, num2);
				await PomoSummaryDao.SavePomoSummary(pomo);
				task.modifiedTime = DateTime.Now;
				await TaskDao.UpdateTask(task);
				await SyncStatusDao.AddModifySyncStatus(pomo.taskId);
			}
		}
		TaskChangeNotifier.NotifyTaskPomoAdded(taskDict.Keys.ToList());
	}

	private static async Task<PomodoroSummaryModel> DeletePomoSummary(string pomoId, string taskId, long duration, bool isPomo, int count)
	{
		PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(taskId);
		if (pomodoroSummaryModel == null)
		{
			return new PomodoroSummaryModel
			{
				id = Utils.GetGuid(),
				userId = LocalSettings.Settings.LoginUserId,
				estimatedPomo = 0,
				EstimatedDuration = 0L,
				taskId = taskId
			};
		}
		object[] array = pomodoroSummaryModel.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
		if (array != null)
		{
			pomodoroSummaryModel.focuses.Remove(array);
		}
		else if (isPomo)
		{
			pomodoroSummaryModel.count += count;
			pomodoroSummaryModel.count = ((pomodoroSummaryModel.count > 0) ? pomodoroSummaryModel.count : 0);
			pomodoroSummaryModel.PomoDuration += duration;
			pomodoroSummaryModel.PomoDuration = ((pomodoroSummaryModel.PomoDuration > 0) ? pomodoroSummaryModel.PomoDuration : 0);
		}
		else
		{
			pomodoroSummaryModel.StopwatchDuration += duration;
			pomodoroSummaryModel.StopwatchDuration = ((pomodoroSummaryModel.StopwatchDuration > 0) ? pomodoroSummaryModel.StopwatchDuration : 0);
		}
		return pomodoroSummaryModel;
	}

	public static async Task UpdatePomodoro(string pomoId, List<PomoTask> pomoTasks, bool isPomo)
	{
		Dictionary<string, long> taskDict = new Dictionary<string, long>();
		Dictionary<string, long> habitDict = new Dictionary<string, long>();
		foreach (PomoTask pomoTask in pomoTasks)
		{
			if (!string.IsNullOrEmpty(pomoTask.TaskId))
			{
				long totalSecond = Utils.GetTotalSecond(pomoTask.StartTime, pomoTask.EndTime);
				if (taskDict.ContainsKey(pomoTask.TaskId))
				{
					taskDict[pomoTask.TaskId] += totalSecond;
				}
				else
				{
					taskDict.Add(pomoTask.TaskId, totalSecond);
				}
			}
			if (!string.IsNullOrEmpty(pomoTask.HabitId) && pomoTask.EndTime > DateTime.Today)
			{
				long totalSecond2 = Utils.GetTotalSecond((pomoTask.StartTime > DateTime.Today) ? pomoTask.StartTime : DateTime.Today, pomoTask.EndTime);
				if (habitDict.ContainsKey(pomoTask.HabitId))
				{
					habitDict[pomoTask.HabitId] += totalSecond2;
				}
				else
				{
					habitDict.Add(pomoTask.HabitId, totalSecond2);
				}
			}
		}
		foreach (KeyValuePair<string, long> kv in taskDict)
		{
			TaskModel task = await TaskDao.GetThinTaskById(kv.Key);
			if (task != null)
			{
				long value = kv.Value;
				int count = ((Math.Abs(value) >= Constants.MinFocusDuration) ? 1 : 0);
				PomodoroSummaryModel pomo = await UpdatePomoSummary(pomoId, kv.Key, value, isPomo, count);
				await PomoSummaryDao.SavePomoSummary(pomo);
				task.modifiedTime = DateTime.Now;
				await TaskDao.UpdateTask(task);
				await SyncStatusDao.AddModifySyncStatus(pomo.taskId);
			}
		}
		TaskChangeNotifier.NotifyTaskPomoAdded(taskDict.Keys.ToList());
		foreach (KeyValuePair<string, long> kv in habitDict)
		{
			HabitModel habitModel = await HabitDao.GetHabitById(kv.Key);
			if (habitModel != null && !habitModel.IsBoolHabit())
			{
				float num = 0f;
				if (FocusConstance.HourUnit.Contains(habitModel.Unit.Trim().ToLower()))
				{
					num = (float)kv.Value / 3600f;
				}
				if (FocusConstance.MinuteUnit.Contains(habitModel.Unit.Trim().ToLower()))
				{
					num = (float)kv.Value / 60f;
				}
				if (num > 0f)
				{
					HabitService.CheckInHabit(habitModel.Id, DateTime.Today, Math.Round(num, 2));
				}
			}
		}
	}

	private static async Task<PomodoroSummaryModel> GetPomoSummary(string taskId, long duration, bool isPomo, int count = 1)
	{
		PomodoroSummaryModel pomodoroSummaryModel = (await PomoSummaryDao.GetPomoByTaskId(taskId)) ?? new PomodoroSummaryModel
		{
			id = Utils.GetGuid(),
			userId = LocalSettings.Settings.LoginUserId,
			estimatedPomo = 0,
			EstimatedDuration = 0L,
			taskId = taskId
		};
		if (isPomo)
		{
			pomodoroSummaryModel.count += count;
			pomodoroSummaryModel.count = ((pomodoroSummaryModel.count > 0) ? pomodoroSummaryModel.count : 0);
			pomodoroSummaryModel.PomoDuration += duration;
			pomodoroSummaryModel.PomoDuration = ((pomodoroSummaryModel.PomoDuration > 0) ? pomodoroSummaryModel.PomoDuration : 0);
		}
		else
		{
			pomodoroSummaryModel.StopwatchDuration += duration;
			pomodoroSummaryModel.StopwatchDuration = ((pomodoroSummaryModel.StopwatchDuration > 0) ? pomodoroSummaryModel.StopwatchDuration : 0);
		}
		return pomodoroSummaryModel;
	}

	private static async Task<PomodoroSummaryModel> UpdatePomoSummary(string pomoId, string taskId, long duration, bool isPomo, int count)
	{
		bool isDelete = duration < 0;
		PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(taskId);
		if (pomodoroSummaryModel == null)
		{
			pomodoroSummaryModel = new PomodoroSummaryModel
			{
				id = Utils.GetGuid(),
				userId = LocalSettings.Settings.LoginUserId,
				estimatedPomo = 0,
				EstimatedDuration = 0L,
				taskId = taskId
			};
			if (isDelete)
			{
				return pomodoroSummaryModel;
			}
		}
		object[] array = pomodoroSummaryModel.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
		if (isDelete)
		{
			if (array != null)
			{
				pomodoroSummaryModel.focuses.Remove(array);
			}
			else if (isPomo)
			{
				pomodoroSummaryModel.count += count;
				pomodoroSummaryModel.count = ((pomodoroSummaryModel.count > 0) ? pomodoroSummaryModel.count : 0);
				pomodoroSummaryModel.PomoDuration += duration;
				pomodoroSummaryModel.PomoDuration = ((pomodoroSummaryModel.PomoDuration > 0) ? pomodoroSummaryModel.PomoDuration : 0);
			}
			else
			{
				pomodoroSummaryModel.StopwatchDuration += duration;
				pomodoroSummaryModel.StopwatchDuration = ((pomodoroSummaryModel.StopwatchDuration > 0) ? pomodoroSummaryModel.StopwatchDuration : 0);
			}
		}
		else if (array != null)
		{
			array[2] = duration;
		}
		else
		{
			pomodoroSummaryModel.AddFocuses(new object[3]
			{
				pomoId,
				(!isPomo) ? 1 : 0,
				duration
			});
		}
		return pomodoroSummaryModel;
	}

	public static async Task SaveTaskPomoDuration(string focusId, string taskId, int duration)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			taskModel.modifiedTime = DateTime.Now;
			await TaskDao.UpdateTask(taskModel);
			await PomoSummaryDao.SavePomoSummary(await UpdatePomoSummary(focusId, taskId, duration, isPomo: true, 0));
			SyncStatusDao.AddModifySyncStatus(taskId);
		}
	}

	public static async Task DeleteAllTasksInProject(string projectId)
	{
		List<TaskModel> tasks = await TaskDao.GetTasksInProjectAsync(projectId);
		if (tasks == null || tasks.Count <= 0)
		{
			return;
		}
		foreach (TaskModel item in tasks)
		{
			item.deleted = 1;
		}
		await TaskDao.BatchUpdateTasks(tasks.ToList(), checkUserId: true, CheckMatchedType.All);
		List<string> taskIds = tasks.Select((TaskModel task) => task.id).ToList();
		TaskChangeNotifier.NotifyTaskBatchDeletedChanged(taskIds);
		await SyncStatusDao.BatchAddDeleteSyncStatus(taskIds);
	}

	public static bool IsEmptyTask(TaskModel fullTaskModel, bool checkChildren = false)
	{
		if (!string.IsNullOrEmpty(fullTaskModel.title))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(fullTaskModel.content))
		{
			return false;
		}
		if (fullTaskModel.items != null && fullTaskModel.items.Any())
		{
			return false;
		}
		int.TryParse(fullTaskModel.commentCount, out var result);
		if (result > 0)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(fullTaskModel.desc))
		{
			return false;
		}
		return true;
	}

	public static async Task<TaskModel> TryMergeTask(List<string> taskIds, string primaryTaskId, IProjectTaskDefault taskDefault)
	{
		List<string> topIds = TaskDao.GetTreeTopIds(taskIds, null);
		List<TaskModel> tasks = await TaskDao.GetTaskAndChildrenInBatch(topIds, checkExist: false);
		TaskModel primaryTask = tasks?.FirstOrDefault((TaskModel taskModel) => taskModel.id == primaryTaskId);
		if (primaryTask == null)
		{
			return null;
		}
		ProjectModel mergeProject = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == primaryTask.projectId);
		string content = string.Format(Utils.GetString("MergeHint"), tasks.Count);
		bool? flag = new CustomerDialog(Utils.GetString("Merge"), content, Utils.GetString("Merge"), Utils.GetString("Cancel"))
		{
			Owner = App.Window
		}.ShowDialog();
		if (!flag.HasValue || !flag.Value)
		{
			return null;
		}
		bool needPin = primaryTask.pinnedTimeStamp > 0;
		TimeData defaultTimeData = taskDefault.GetTimeData();
		TaskModel parent = new TaskModel
		{
			id = Utils.GetGuid(),
			title = Utils.GetString("NewMergeTask"),
			sortOrder = ProjectSortOrderDao.GetNextTaskSortOrderInProject(mergeProject.id, primaryTask.sortOrder),
			pinnedTime = (needPin ? DateTime.Now.ToString(UtcDateTimeConverter.GetConverterValue(DateTime.Now)) : null),
			columnId = primaryTask.columnId,
			startDate = defaultTimeData?.StartDate,
			priority = taskDefault.GetPriority(),
			tags = taskDefault.GetTags()?.ToArray(),
			projectId = primaryTask.projectId,
			isAllDay = true,
			timeZone = TimeZoneData.LocalTimeZoneModel.TimeZoneName,
			kind = "TEXT"
		};
		parent.tag = TagSerializer.ToJsonContent(parent.tags.ToList());
		List<string> newChildIds = new List<string>();
		int i = 0;
		foreach (TaskModel task in tasks)
		{
			if (topIds.Contains(task.id) && !string.IsNullOrEmpty(task.parentId))
			{
				await TaskDao.AddOrRemoveTaskChildIds(task.parentId, new List<string> { task.id }, addChild: false);
			}
			await SyncStatusDao.AddSetParentSyncStatus(task.id, task.parentId);
			task.parentId = parent.id;
			newChildIds.Add(task.id);
			task.childrenString = null;
			task.childIds = null;
			task.columnId = parent.columnId;
			task.sortOrder = 268435456L * (long)i;
			i++;
			await SyncStatusDao.AddModifySyncStatus(task.id);
			if (task.projectId != mergeProject.id)
			{
				await SyncStatusDao.AddMoveProjectStatus(task.id, task.projectId);
				task.projectId = mergeProject.id;
			}
			if (task.pinnedTimeStamp <= 0 && needPin)
			{
				task.pinnedTime = DateTime.Now.ToString(UtcDateTimeConverter.GetConverterValue(DateTime.Now));
			}
		}
		await AddTask(parent);
		await TaskDao.BatchUpdateTasks(tasks, checkUserId: true, CheckMatchedType.All);
		await TaskDao.AddOrRemoveTaskChildIds(parent.id, newChildIds, addChild: true);
		List<TaskReminderModel> list = defaultTimeData?.Reminders;
		if (list != null)
		{
			foreach (TaskReminderModel item in list)
			{
				item.taskserverid = parent.id;
				await TaskReminderDao.SaveReminders(item);
			}
		}
		return parent;
	}

	public static Task<bool> MergeTasks(IEnumerable<TaskModel> tasks)
	{
		return MergeTasksCore(tasks, MergeReceivedTasks);
	}

	internal static Task<bool> MergeTasksCore(IEnumerable<TaskModel> tasks, Func<IEnumerable<TaskModel>, Task<bool>> merge)
	{
		if (tasks == null)
		{
			return Task.FromResult(result: false);
		}
		List<TaskModel> list = tasks.ToList();
		foreach (TaskModel item in list)
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
		return merge(list);
	}

	private static async Task<bool> MergeReceivedTasks(IEnumerable<TaskModel> tasks)
	{
		HashSet<string> allTaskIdMap = TaskCache.GetAllTaskIdMap();
		List<TaskModel> newTaskList = new List<TaskModel>();
		List<TaskModel> updateTaskList = new List<TaskModel>();
		List<TaskModel> tagCorrectionTasks = new List<TaskModel>();
		List<TaskReminderModel> reminderList = new List<TaskReminderModel>();
		List<TaskDetailItemModel> checkListItems = new List<TaskDetailItemModel>();
		List<AttachmentModel> attachments = new List<AttachmentModel>();
		List<PomodoroSummaryModel> pomos = new List<PomodoroSummaryModel>();
		IEnumerable<string> syncStatusEntityIds = (await SyncStatusDao.GetAllSyncStatus())?.Select((SyncStatusModel s) => s.EntityId).Distinct();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (TaskModel task in tasks)
		{
			if (string.IsNullOrEmpty(task.userId))
			{
				task.userId = userId;
			}
			if (!allTaskIdMap.Contains(task.id))
			{
				TaskDao.HandleServerDate(task);
				newTaskList.Add(task);
				if (task.reminders != null)
				{
					TaskReminderModel[] reminders = task.reminders;
					foreach (TaskReminderModel obj in reminders)
					{
						obj.Taskid = task._Id;
						obj.taskserverid = task.id;
					}
					reminderList.AddRange(task.reminders);
				}
				if (task.items != null)
				{
					TaskDetailItemModel[] items = task.items;
					foreach (TaskDetailItemModel obj2 in items)
					{
						obj2.TaskId = task._Id;
						obj2.TaskServerId = task.id;
					}
					checkListItems.AddRange(task.items);
				}
				if (task.Attachments != null)
				{
					AttachmentModel[] attachments2 = task.Attachments;
					foreach (AttachmentModel obj3 in attachments2)
					{
						obj3.taskId = task.id;
						obj3.sync_status = 2.ToString();
					}
					attachments.AddRange(task.Attachments);
				}
				if (task.FocusSummaries != null)
				{
					PomodoroSummaryModel[] focusSummaries = task.FocusSummaries;
					for (int num = 0; num < focusSummaries.Length; num++)
					{
						focusSummaries[num].taskId = task.id;
					}
					pomos.AddRange(task.FocusSummaries);
				}
				List<string> childIds = task.childIds;
				if (childIds != null && childIds.Count > 0)
				{
					task.childrenString = JsonConvert.SerializeObject(task.childIds);
				}
			}
			else if (task.TagsNeedSync)
			{
				tagCorrectionTasks.Add(task);
			}
			else
			{
				TaskModel taskModel = await TaskDao.GetTaskById(task.id);
				if (taskModel != null && taskModel.deleted != task.deleted && (syncStatusEntityIds == null || !syncStatusEntityIds.Contains(task.id)))
				{
					taskModel.deleted = task.deleted;
					taskModel.modifiedTime = task.modifiedTime;
					updateTaskList.Add(taskModel);
				}
			}
		}
		if (newTaskList.Count > 0)
		{
			await TaskDao.BatchInsertTasks(newTaskList, fromRemote: true);
			TaskChangeNotifier.NotifyTaskBatchAdded(newTaskList.Select((TaskModel t) => t.id).ToList());
			TaskSyncedJsonBean taskSyncedJsonBean = new TaskSyncedJsonBean();
			taskSyncedJsonBean.Added.AddRange(newTaskList);
			await TaskSyncedJsonDao.SaveTaskSyncedJsons(taskSyncedJsonBean);
		}
		if (updateTaskList.Count > 0)
		{
			await TaskDao.BatchUpdateTasks(updateTaskList);
		}
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
		if (pomos.Count > 0)
		{
			await App.Connection.InsertAllAsync(pomos);
		}
		IEnumerable<TaskModel> tasks2 = tagCorrectionTasks;
		Func<SyncTaskBean, Task> merge = async delegate(SyncTaskBean bean)
		{
			await new TaskBatchHandler(userId, new SyncResult()).MergeWithServer(bean, 1, new LogModel("Historical task tag correction"), ignoreEtag: true);
		};
		bool flag = await MergeHistoricalTagCorrections(tasks2, merge, await SyncStatusDao.GetAllSyncStatus());
		return newTaskList.Count > 0 || updateTaskList.Count > 0 || flag;
	}

	internal static async Task<bool> MergeHistoricalTagCorrections(IEnumerable<TaskModel> tasks, Func<SyncTaskBean, Task> merge, IEnumerable<SyncStatusModel> pendingChanges = null)
	{
		HashSet<string> protectedIds = new HashSet<string>(from change in pendingChanges ?? Enumerable.Empty<SyncStatusModel>()
			where change.Type != 0
			select change.EntityId);
		SyncTaskBean syncTaskBean = new SyncTaskBean();
		syncTaskBean.Update.AddRange(tasks.Where((TaskModel task) => task.TagsNeedSync && !protectedIds.Contains(task.id)));
		if (syncTaskBean.Update.Count == 0)
		{
			return false;
		}
		await merge(syncTaskBean);
		return true;
	}

	public static async Task<List<TaskDetailItemModel>> TaskToSubtask(List<TaskModel> fromTasks, string toTaskId, string targetItemId, bool topOrBottom)
	{
		if (fromTasks == null || fromTasks.Count == 0)
		{
			return null;
		}
		TaskModel toTask = await TaskDao.GetThinTaskById(toTaskId);
		Dictionary<string, long> dictionary = await TaskDetailItemDao.GetCheckItemSortOrderInTask(new List<string> { toTaskId });
		List<long> orders = dictionary.Values.ToList();
		long limits = LimitCache.GetLimitByKey(Constants.LimitKind.SubtaskNumber);
		long firstOrder;
		long secondOrder;
		if (orders.Count == 0)
		{
			firstOrder = 0L;
			secondOrder = 268435456L;
		}
		else
		{
			firstOrder = (dictionary.ContainsKey(targetItemId) ? dictionary[targetItemId] : dictionary.Values.Min());
			if (topOrBottom)
			{
				orders = orders.OrderByDescending((long o) => o).ToList();
				secondOrder = orders.FirstOrDefault((long o) => o < firstOrder);
				if ((secondOrder == orders.Min() && secondOrder == firstOrder) || secondOrder >= firstOrder)
				{
					secondOrder = firstOrder - 268435456;
				}
			}
			else
			{
				orders = orders.OrderBy((long o) => o).ToList();
				secondOrder = orders.FirstOrDefault((long o) => o > firstOrder);
				if ((secondOrder == orders.Max() && secondOrder == firstOrder) || secondOrder <= firstOrder)
				{
					secondOrder = firstOrder + 268435456;
				}
			}
		}
		if (firstOrder > secondOrder)
		{
			long num = firstOrder;
			firstOrder = secondOrder;
			secondOrder = num;
		}
		List<TaskDetailItemModel> subItems = new List<TaskDetailItemModel>();
		for (int i = 0; i < fromTasks.Count; i++)
		{
			List<TaskDetailItemModel> checkItems = new List<TaskDetailItemModel>();
			TaskModel taskModel = fromTasks[i];
			long sortOrder = firstOrder + (secondOrder - firstOrder) * (i + 1) / (fromTasks.Count + 1);
			TaskDetailItemModel taskDetailItemModel = new TaskDetailItemModel
			{
				id = Utils.GetGuid(),
				TaskId = toTask._Id,
				TaskServerId = toTaskId,
				title = taskModel.title,
				status = 0,
				sortOrder = sortOrder
			};
			checkItems.Add(taskDetailItemModel);
			if (taskModel.startDate.HasValue && UserDao.IsPro())
			{
				taskDetailItemModel.startDate = taskModel.startDate;
				taskDetailItemModel.isAllDay = taskModel.isAllDay;
			}
			if (taskModel.kind == "CHECKLIST")
			{
				if (!string.IsNullOrEmpty(taskModel.desc))
				{
					TaskDetailItemModel item = new TaskDetailItemModel
					{
						id = Utils.GetGuid(),
						TaskId = toTask._Id,
						TaskServerId = toTaskId,
						title = taskModel.desc,
						status = 0,
						sortOrder = sortOrder + 1
					};
					checkItems.Add(item);
				}
				List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(fromTasks[i].id);
				if (list != null && list.Any())
				{
					List<TaskDetailItemModel> collection = list.OrderBy((TaskDetailItemModel taskDetailItemModel2) => taskDetailItemModel2.sortOrder).ToList().Select((TaskDetailItemModel originalItem, int index) => new TaskDetailItemModel
					{
						id = Utils.GetGuid(),
						TaskServerId = toTaskId,
						title = originalItem.title,
						status = originalItem.status,
						sortOrder = sortOrder + i + 1,
						startDate = originalItem.startDate,
						isAllDay = originalItem.isAllDay
					})
						.ToList();
					checkItems.AddRange(collection);
				}
			}
			else if (!string.IsNullOrEmpty(taskModel.content))
			{
				TaskDetailItemModel item2 = new TaskDetailItemModel
				{
					id = Utils.GetGuid(),
					TaskId = toTask._Id,
					TaskServerId = toTaskId,
					title = taskModel.content,
					status = 0,
					sortOrder = sortOrder + 1
				};
				checkItems.Add(item2);
			}
			subItems.AddRange(checkItems);
			if (subItems.Count + orders.Count > limits)
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreSubTasks);
				return null;
			}
		}
		await SyncStatusDao.AddModifySyncStatus(toTaskId);
		await TaskDetailItemDao.BatchInsertChecklists(subItems);
		await BatchDeleteTasks(fromTasks, showUndo: false);
		return subItems;
	}

	internal static async Task<int> GetQuadrantTaskBelong(string taskId, int previous)
	{
		TaskBaseViewModel task = TaskCache.GetTaskById(taskId);
		if (task == null)
		{
			return 0;
		}
		List<QuadrantModel> obj = await MatrixProfileDao.GetActiveQuadrantsAsync();
		string previousId = "quadrant" + previous;
		obj.Sort(delegate(QuadrantModel a, QuadrantModel b)
		{
			if (a.id == previousId)
			{
				return -1;
			}
			return (b.id == previousId && a.id != previousId) ? 1 : 0;
		});
		foreach (QuadrantModel item in obj)
		{
			List<TaskBaseViewModel> taskMatchedNormalFilter = GetTaskMatchedNormalFilter(ticktick_WPF.Util.Filter.Parser.ToNormalModel(item.rule), new List<TaskBaseViewModel> { task }, inAll: true);
			int result;
			if (taskMatchedNormalFilter != null && taskMatchedNormalFilter.Count > 0)
			{
				return int.TryParse(item.id.Substring(8), out result) ? result : 0;
			}
		}
		return 0;
	}

	public static async Task<TaskModel> SubtaskToTask(string checkItemId, TaskPrimaryProperty model, string columnId = "")
	{
		TaskDetailItemModel checkitem = await TaskDetailItemDao.GetChecklistItemById(checkItemId);
		if (checkitem != null)
		{
			TaskModel taskModel = new TaskModel
			{
				id = Utils.GetGuid(),
				title = checkitem.title,
				projectId = model.ProjectId,
				status = model.TaskStatus,
				priority = model.Priority.GetValueOrDefault(),
				sortOrder = model.SortOrder,
				assignee = model.AssigneeId,
				kind = "TEXT",
				parentId = model.ParentId,
				columnId = columnId,
				createdTime = DateTime.Now
			};
			if (model.Tags != null && model.Tags.Count > 0)
			{
				taskModel.tag = TagSerializer.ToJsonContent(model.Tags.ToList());
			}
			if (model.TimeData?.StartDate.HasValue ?? false)
			{
				taskModel.startDate = model.TimeData.StartDate;
				taskModel.isAllDay = model.TimeData.IsAllDay;
				taskModel.dueDate = model.TimeData.DueDate;
				taskModel.repeatFlag = model.TimeData.RepeatFlag;
				taskModel.repeatFrom = model.TimeData.RepeatFrom;
				taskModel.reminders = model.TimeData.Reminders?.ToArray();
			}
			if (model.TimeData?.TimeZone != null)
			{
				taskModel.isFloating = model.TimeData.TimeZone.IsFloat;
				taskModel.timeZone = model.TimeData.TimeZone.TimeZoneName;
			}
			if (taskModel.status != 0)
			{
				taskModel.completedTime = DateTime.Now;
			}
			await AddTask(taskModel);
			await TaskDetailItemDao.DeleteById(checkitem.id);
			CheckItemCompleteExtra checkItemCompleteExtra = await SyncTaskChange(checkitem);
			if (checkItemCompleteExtra != null)
			{
				UpdateTask(checkItemCompleteExtra.OriginalTask);
			}
			ItemChangeNotifier.NotifyItemDeleted(checkitem);
			if (taskModel.reminders != null && taskModel.reminders.Any() && !Utils.IsEmptyDate(taskModel.startDate))
			{
				List<TaskReminderModel> list = taskModel.reminders.ToList();
				list.ForEach(delegate(TaskReminderModel reminder)
				{
					reminder.taskserverid = taskModel.id;
				});
				await TaskReminderDao.BatchAddTaskReminders(list);
			}
			return taskModel;
		}
		return null;
	}

	public static async Task<TaskModel> SetTaskMultiProperty(TaskModel task, IDroppable droppable)
	{
		string projectId = task.projectId;
		if (!string.IsNullOrEmpty(droppable.ProjectId))
		{
			task.projectId = droppable.ProjectId;
		}
		task.priority = droppable.Priority;
		if (droppable.Tags != null && droppable.Tags.Any())
		{
			List<string> first = TagSerializer.ToTags(task.tag);
			task.tag = TagSerializer.ToJsonContent(first.Union(droppable.Tags).ToList());
		}
		if (droppable.DefaultDate.HasValue)
		{
			task.startDate = droppable.DefaultDate.Value;
			task.reminders = TimeData.GetDefaultAllDayReminders().ToArray();
			task.isAllDay = true;
			task.dueDate = null;
		}
		await SetTaskProperties(task, projectId);
		return task;
	}

	public static async Task SetTaskProperties(TaskModel task, string fromProjectId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(task.id);
		if (taskModel != null)
		{
			task._Id = taskModel._Id;
			await UpdateTask(task);
			await SyncStatusDao.AddMoveProjectStatus(task.id, fromProjectId);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskProjectChanged(task);
		}
	}

	public static async Task SetItemTitle(string taskId, string subtaskId, string text)
	{
		TaskDetailItemModel taskDetailItemModel = await TaskDetailItemDao.GetChecklistItemById(subtaskId);
		if (taskDetailItemModel != null && taskDetailItemModel.title != text)
		{
			taskDetailItemModel.title = text;
			await TaskDetailItemDao.TrySaveChecklistItem(taskDetailItemModel, checkCount: false);
			TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
			if (taskModel != null)
			{
				taskModel.modifiedTime = DateTime.Now;
				await TaskDao.UpdateTask(taskModel);
			}
			await SyncStatusDao.AddModifySyncStatus(taskId);
			SyncManager.TryDelaySync(3000);
		}
	}

	public static async Task SaveTaskColumnId(string taskId, string columnId, bool checkEmpty = false, bool setSortOrder = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return;
		}
		if (checkEmpty && !string.IsNullOrEmpty(task.columnId))
		{
			TaskBaseViewModel taskById = TaskCache.GetTaskById(task.id);
			if (taskById != null)
			{
				taskById.ColumnId = task.columnId;
			}
			return;
		}
		task.columnId = columnId;
		task.modifiedTime = DateTime.Now;
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.projectId);
		if (setSortOrder && projectModel != null && projectModel.sortType == Constants.SortType.sortOrder.ToString())
		{
			task.sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(task.projectId);
		}
		await UpdateTaskColumnId(task);
		List<TaskModel> list = await GetAllSubTasksByIdAsync(taskId, task.projectId);
		if (list != null && list.Count > 0)
		{
			foreach (TaskModel item in list)
			{
				item.columnId = columnId;
				item.modifiedTime = DateTime.Now;
				await UpdateTaskColumnId(item);
			}
		}
		DataChangedNotifier.NotifyColumnChanged(task.projectId);
	}

	public static async Task BatchSaveTasksColumnId(List<string> taskIds, string columnId, string projectId)
	{
		List<TaskBaseViewModel> models = TaskCache.GetTaskAndChildrenInBatch(taskIds, checkExist: true, projectId);
		long sortOrder = ProjectSortOrderDao.GetNewTaskSortOrderInProject(projectId, null, false);
		models.Sort((TaskBaseViewModel a, TaskBaseViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
		if (models != null && models.Count > 0)
		{
			for (int i = 0; i < models.Count; i++)
			{
				TaskModel taskModel = await TaskDao.GetThinTaskById(models[i].Id);
				if (taskModel != null)
				{
					taskModel.columnId = columnId;
					taskModel.modifiedTime = DateTime.Now;
					if (string.IsNullOrEmpty(taskModel.parentId))
					{
						taskModel.sortOrder = sortOrder + 268435456L / (long)models.Count * i;
					}
					await UpdateTaskColumnId(taskModel);
				}
			}
		}
		TaskChangeNotifier.NotifyTaskBatchChanged(taskIds);
	}

	public static async Task<bool> AdjustKanbanData(string projectId)
	{
		List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(projectId);
		if (list == null || !list.Any())
		{
			return false;
		}
		List<ColumnModel> list2 = list.OrderBy((ColumnModel c) => c.sortOrder).ToList();
		string currentDefaultColumnId = list2[0].id;
		List<string> collection = list2.Select((ColumnModel c) => c.id).ToList();
		HashSet<string> validColumnIds = new HashSet<string>(collection);
		string lastDefaultColumnId = TaskCache.GetLastDefaultColumnId(projectId);
		if (string.IsNullOrEmpty(lastDefaultColumnId))
		{
			TaskCache.SetLastDefaultColumnId(projectId, currentDefaultColumnId);
			return false;
		}
		if (lastDefaultColumnId == currentDefaultColumnId)
		{
			return false;
		}
		List<TaskModel> invalidTasks = (await TaskDao.GetTasksInProjectAsync(projectId))?.Where((TaskModel task) => task.deleted == 0 && task.status == 0 && (string.IsNullOrEmpty(task.columnId) || !validColumnIds.Contains(task.columnId))).ToList();
		if (invalidTasks != null && invalidTasks.Any())
		{
			foreach (TaskModel item in invalidTasks)
			{
				item.columnId = lastDefaultColumnId;
				item.modifiedTime = DateTime.Now;
				await UpdateTaskColumnId(item);
			}
			SyncManager.TryDelaySync();
		}
		TaskCache.SetLastDefaultColumnId(projectId, currentDefaultColumnId);
		return invalidTasks?.Any() ?? false;
	}

	private static async Task<string> ResolveAddOrMoveColumnId(string projectId, string preferredColumnId = null)
	{
		if (string.IsNullOrEmpty(projectId) || !KanbanHelper.IsProjectInKanbanMode(projectId))
		{
			return preferredColumnId;
		}
		List<ColumnModel> list = await ColumnDao.GetColumnsByProjectId(projectId);
		if (list == null || !list.Any())
		{
			return preferredColumnId;
		}
		string text = list.OrderBy((ColumnModel c) => c.sortOrder).FirstOrDefault()?.id;
		if (string.IsNullOrEmpty(text))
		{
			return preferredColumnId;
		}
		if (string.IsNullOrEmpty(preferredColumnId))
		{
			return text;
		}
		return list.Any((ColumnModel c) => c.id == preferredColumnId) ? preferredColumnId : text;
	}

	public static async Task SaveCommentCount(string taskId, bool sync = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return;
		}
		List<CommentModel> list = await CommentDao.GetCommentsByTaskId(taskId);
		TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
		if (task.commentCount != list.Count.ToString())
		{
			task.commentCount = list.Count.ToString();
			task.modifiedTime = DateTime.Now;
			taskBaseViewModel.CommentCount = task.commentCount;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			TaskChangeNotifier.NotifyTaskTextChanged(task.id);
			if (sync)
			{
				await SyncStatusDao.AddModifySyncStatus(task.id);
			}
			SyncManager.TryDelaySync();
		}
		else
		{
			taskBaseViewModel.NotifyCommentsChanged();
		}
	}

	public static async Task<bool> PullTasksOfProject(string projectId)
	{
		if (TaskPullingProjectIds.Contains(projectId))
		{
			return false;
		}
		TaskPullingProjectIds.Add(projectId);
		try
		{
			List<string> addStatus = (await SyncStatusDao.GetSyncStatusByType(4)).Select((SyncStatusModel status) => status.EntityId).ToList();
			List<string> moveStatus = (await SyncStatusDao.GetSyncStatusByType(2)).Select((SyncStatusModel status) => status.EntityId).ToList();
			List<string> restoreStatus = (await SyncStatusDao.GetSyncStatusByType(7)).Select((SyncStatusModel status) => status.EntityId).ToList();
			ObservableCollection<TaskModel> tasks = await Communicator.PullServerTasksByProjectId(projectId);
			if (tasks == null)
			{
				return false;
			}
			List<TaskModel> list = await TaskDao.GetTasksInProjectAsync(projectId);
			HashSet<string> remoteIds = new HashSet<string>();
			List<TaskModel> needAdded = new List<TaskModel>();
			foreach (TaskModel item in tasks)
			{
				remoteIds.Add(item.id);
				if (!TaskCache.ExistTask(item.id))
				{
					needAdded.Add(item);
				}
			}
			List<TaskModel> list2 = list?.Where((TaskModel task) => task.status == 0 && task.deleted == 0 && !remoteIds.Contains(task.id)).ToList();
			bool changed = false;
			if (list2 != null && list2.Any())
			{
				foreach (TaskModel item2 in list2)
				{
					if (!addStatus.Contains(item2.id) && !moveStatus.Contains(item2.id) && !restoreStatus.Contains(item2.id))
					{
						changed = true;
						await TaskDao.DeleteTaskInDb(item2.id);
					}
				}
			}
			if (needAdded.Count > 0)
			{
				changed = true;
				await MergeTasks(needAdded);
			}
			ProjectModel projectById = CacheManager.GetProjectById(projectId);
			if (projectById != null && projectById.needPullRemote)
			{
				projectById.needPullRemote = false;
			}
			await ProjectDao.SetProjectNeedPullRemote(projectId, needPullRemote: false);
			return changed;
		}
		finally
		{
			TaskPullingProjectIds.Remove(projectId);
		}
	}

	public static async Task ChangeCompleteDate(string taskId, DateTime date)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null && taskModel.status != 0)
		{
			taskModel.completedTime = date;
			taskModel.modifiedTime = DateTime.Now;
			await SaveTask(taskModel);
		}
	}

	public static async Task TryLoadTaskChildren(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null && taskModel.deleted == 0)
		{
			await LoadTaskChildren(taskModel.id, taskModel.projectId);
		}
	}

	private static async Task LoadTaskChildren(string taskId, string projectId, int count = 0)
	{
		if (count > 5)
		{
			return;
		}
		bool flag = false;
		Node<TaskBaseViewModel> taskNode = TaskCache.GetTaskNode(taskId, projectId);
		if (taskNode == null)
		{
			return;
		}
		List<Node<TaskBaseViewModel>> list = taskNode?.GetAllChildrenNode() ?? new List<Node<TaskBaseViewModel>>();
		list.Insert(0, taskNode);
		foreach (Node<TaskBaseViewModel> item in list)
		{
			List<string> list2 = ((item.Value?.ChildrenIds == null) ? null : JsonConvert.DeserializeObject<List<string>>(item.Value.ChildrenIds));
			if (list2 == null)
			{
				continue;
			}
			foreach (string id in list2)
			{
				if (item.Children != null && item.Children.Exists((Node<TaskBaseViewModel> n) => n.NodeId == id))
				{
					continue;
				}
				if (TaskCache.ExistTask(id))
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(id);
					if (taskById.ProjectId == projectId && taskById.Deleted == 0)
					{
						continue;
					}
				}
				await PullChildren(await TaskDao.GetTaskById(item.Value.Id));
				flag = true;
				break;
			}
		}
		if (flag)
		{
			await Task.Delay(50);
			int num = count + 1;
			count = num;
			await LoadTaskChildren(taskId, projectId, num);
		}
	}

	private static async Task PullChildren(TaskModel task)
	{
		if (task == null || task.deleted != 0)
		{
			return;
		}
		TaskModel remote = await Communicator.GetTask(task.id, task.projectId, withChildren: true);
		if (remote != null && remote.id == task.id)
		{
			List<TaskModel> children = remote.children;
			if (children != null && children.Count > 0)
			{
				await MergeTasks(remote.children);
			}
			task.parentId = remote.parentId;
			List<string> childIds = remote.childIds;
			task.childrenString = ((childIds != null && childIds.Count > 0) ? JsonConvert.SerializeObject(remote.childIds) : null);
			await UpdateTaskParent(task);
		}
	}

	public static async Task<bool> SwitchTaskOrNote(string taskId, bool toast = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return false;
		}
		if (task.kind == "NOTE")
		{
			task.kind = "TEXT";
		}
		else
		{
			if (task.kind == "CHECKLIST")
			{
				List<string> checklist = (from s in (await TaskDetailItemDao.GetCheckItemsByTaskId(taskId))?.OrderBy((TaskDetailItemModel c) => c.sortOrder)
					select s.title).ToList();
				task.content = ChecklistUtils.Items2Text(task.desc, checklist);
				TaskModel taskModel = task;
				taskModel.content = await AttachmentDao.AddAttachmentStrings(task.id, task.content);
				task.desc = "";
			}
			task.dueDate = null;
			task.kind = "NOTE";
			task.status = 0;
			task.completedTime = null;
			task.priority = 0;
			task.progress = 0;
			task.repeatFlag = null;
			task.repeatFrom = null;
		}
		task.modifiedTime = DateTime.Now;
		await UpdateTask(task);
		await SyncStatusDao.AddModifySyncStatus(taskId);
		if (toast)
		{
			Utils.Toast(Utils.GetString("Converted"));
		}
		TaskChangeNotifier.NotifyTaskKindChanged(new List<string> { taskId });
		return true;
	}

	public static async Task GetRemoteTaskCheckItems(string projectId, string taskId)
	{
		UtilLog.Info("CheckItemsNotFound TaskId: " + taskId);
		if ((await SyncStatusDao.GetSyncStatusByType(4))?.Any((SyncStatusModel status) => status.EntityId == taskId) == true || _checkItemPulledIds.Contains(taskId))
		{
			return;
		}
		_checkItemPulledIds.Add(taskId);
		TaskModel remote = await Communicator.GetTask(taskId, projectId);
		if (remote == null)
		{
			SyncStatusDao.DeleteSyncStatus(taskId, 0);
			return;
		}
		if (string.IsNullOrEmpty(remote.id))
		{
			TaskModel taskModel = await TaskDao.GetTaskById(taskId);
			if (taskModel != null)
			{
				taskModel.kind = "TEXT";
				await UpdateTaskOnKindChanged(taskModel);
			}
			return;
		}
		TaskDetailItemModel[] items = remote.items;
		if (items != null && items.Length != 0)
		{
			TaskDetailItemModel[] items2 = remote.items;
			foreach (TaskDetailItemModel taskDetailItemModel in items2)
			{
				if (string.IsNullOrEmpty(taskDetailItemModel.serverStartDate) || !(taskDetailItemModel.serverStartDate != "-1"))
				{
					continue;
				}
				try
				{
					taskDetailItemModel.startDate = DateTime.Parse(taskDetailItemModel.serverStartDate);
					if ((taskDetailItemModel.isAllDay.HasValue && taskDetailItemModel.isAllDay.Value) || remote.Floating)
					{
						taskDetailItemModel.startDate = TimeZoneUtils.LocalToTargetTzTime(taskDetailItemModel.startDate.Value, remote.timeZone);
					}
				}
				catch (Exception)
				{
				}
			}
			await TaskDao.SaveCheckItems(remote);
		}
		TaskModel taskModel2 = await TaskDao.GetTaskById(taskId);
		if (taskModel2 != null)
		{
			taskModel2.kind = remote.kind;
			taskModel2.deleted = remote.deleted;
			taskModel2.projectId = remote.projectId;
			await UpdateTask(taskModel2);
		}
	}

	public static async Task<List<AttachmentModel>> RepairMissingTaskAttachments(TaskModel task, IEnumerable<string> referencedAttachmentIds, List<AttachmentModel> localAttachments)
	{
		localAttachments = localAttachments ?? new List<AttachmentModel>();
		HashSet<string> recoveryIds = new HashSet<string>(referencedAttachmentIds?.Where((string id) => !string.IsNullOrEmpty(id)) ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		if (task == null || string.IsNullOrEmpty(task.id) || recoveryIds.Count == 0)
		{
			return localAttachments;
		}
		recoveryIds.ExceptWith(from attachment in localAttachments
			where !string.IsNullOrEmpty(attachment?.id)
			select attachment.id);
		if (recoveryIds.Count == 0)
		{
			return localAttachments;
		}
		AttachmentRecoveryLockEntry recoveryLock;
		lock (AttachmentRecoveryLocksGate)
		{
			if (!AttachmentRecoveryLocks.TryGetValue(task.id, out recoveryLock))
			{
				recoveryLock = new AttachmentRecoveryLockEntry();
				AttachmentRecoveryLocks[task.id] = recoveryLock;
			}
			recoveryLock.Users++;
		}
		await recoveryLock.Semaphore.WaitAsync();
		try
		{
			localAttachments = (await AttachmentDao.GetTaskAttachments(task.id)) ?? new List<AttachmentModel>();
			recoveryIds.ExceptWith(from attachment in localAttachments
				where !string.IsNullOrEmpty(attachment?.id)
				select attachment.id);
			foreach (string attachmentId in recoveryIds.ToList())
			{
				if (HasPendingLocalAttachmentChange(await AttachmentDao.GetAttachmentById(attachmentId)))
				{
					UtilLog.Info("AttachmentRecovery skipped pending local change taskId:" + task.id + ", attachmentId:" + attachmentId);
					recoveryIds.Remove(attachmentId);
				}
			}
			if (recoveryIds.Count == 0)
			{
				return localAttachments;
			}
			UtilLog.Info("AttachmentRecovery request taskId:" + task.id + ", attachmentIds:" + string.Join(",", recoveryIds));
			TaskModel remoteTask = await Communicator.GetTask(task.id, task.projectId);
			if (remoteTask == null || !string.Equals(remoteTask.id, task.id, StringComparison.Ordinal) || remoteTask.Attachments == null)
			{
				UtilLog.Info("AttachmentRecovery skipped remote task unavailable taskId:" + task.id);
				return localAttachments;
			}
			TaskModel taskModel = await TaskDao.GetTaskById(task.id);
			if (taskModel == null || !string.Equals(taskModel.content ?? string.Empty, task.content ?? string.Empty, StringComparison.Ordinal))
			{
				UtilLog.Info("AttachmentRecovery skipped content changed taskId:" + task.id);
				return localAttachments;
			}
			int repairedCount = 0;
			AttachmentModel[] attachments = remoteTask.Attachments;
			foreach (AttachmentModel remoteAttachment in attachments)
			{
				if (remoteAttachment == null || string.IsNullOrEmpty(remoteAttachment.id) || !recoveryIds.Contains(remoteAttachment.id) || remoteAttachment.deleted || remoteAttachment.status != 0)
				{
					continue;
				}
				try
				{
					if (string.IsNullOrEmpty(remoteAttachment.fileType) && !string.IsNullOrEmpty(remoteAttachment.fileName))
					{
						remoteAttachment.fileType = AttachmentProvider.GetFileType(remoteAttachment.fileName).ToString();
					}
					if (!HasPendingLocalAttachmentChange(await AttachmentDao.GetAttachmentById(remoteAttachment.id)))
					{
						await AttachmentDao.InsertOrUpdateAttachment(AttachmentTransfer.ConvertServerToLocal(remoteAttachment, task.id));
						repairedCount++;
					}
				}
				catch (Exception ex)
				{
					UtilLog.Warn("AttachmentRecovery item failed taskId:" + task.id + ", attachmentId:" + remoteAttachment.id + ", " + ExceptionUtils.BuildExceptionMessage(ex));
				}
			}
			List<AttachmentModel> refreshedAttachments = (await AttachmentDao.GetTaskAttachments(task.id)) ?? new List<AttachmentModel>();
			UtilLog.Info("AttachmentRecovery completed taskId:" + task.id + ", " + $"requested:{recoveryIds.Count}, repaired:{repairedCount}");
			if (repairedCount > 0)
			{
				await AttachmentCache.ResetDictItems();
				TaskDao.UpdateTaskAttachmentCount(task.id, refreshedAttachments.Count((AttachmentModel attachment) => attachment.IsActive()));
				TaskChangeNotifier.NotifyAttachmentChanged(task.id, null);
			}
			return refreshedAttachments;
		}
		catch (Exception ex2)
		{
			UtilLog.Warn("AttachmentRecovery failed taskId:" + task?.id + ", " + ExceptionUtils.BuildExceptionMessage(ex2));
			return localAttachments;
		}
		finally
		{
			recoveryLock.Semaphore.Release();
			lock (AttachmentRecoveryLocksGate)
			{
				recoveryLock.Users--;
				if (recoveryLock.Users == 0 && AttachmentRecoveryLocks.TryGetValue(task.id, out var value) && value == recoveryLock)
				{
					AttachmentRecoveryLocks.Remove(task.id);
				}
			}
		}
	}

	private static bool HasPendingLocalAttachmentChange(AttachmentModel attachment)
	{
		if (attachment != null)
		{
			if (!(attachment.sync_status == 0.ToString()))
			{
				return attachment.sync_status == 1.ToString();
			}
			return true;
		}
		return false;
	}

	public static async Task CheckAttachmentPath(string projectId, string taskId, List<AttachmentModel> local)
	{
		UtilLog.Info("AttachmentPath is null TaskId: " + taskId);
		if (local == null || (_pulledAttachmentTaskId.Contains(taskId) && local.All((AttachmentModel x) => !string.IsNullOrEmpty(x.path))))
		{
			return;
		}
		_pulledAttachmentTaskId.Add(taskId);
		TaskModel task = await Communicator.GetTask(taskId, projectId);
		if (task == null || string.IsNullOrEmpty(task.id))
		{
			return;
		}
		AttachmentModel[] attachments = task.Attachments;
		if (attachments == null || attachments.Length == 0)
		{
			return;
		}
		foreach (AttachmentModel attachment in local)
		{
			if (string.IsNullOrEmpty(attachment.path))
			{
				AttachmentModel attachmentModel = task.Attachments.FirstOrDefault((AttachmentModel a) => a.id == attachment.id);
				if (attachmentModel != null)
				{
					attachment.path = attachmentModel.path;
					await AttachmentDao.UpdateAttachment(attachment);
					await AttachmentDao.TryRecoverRemoteAttachment(attachment);
				}
			}
		}
	}

	public static async Task SetImageMode(string taskId, int imageMode)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		task.imgMode = imageMode;
		task.modifiedTime = DateTime.Now;
		await UpdateTaskOnImgModeChanged(task);
		await SyncStatusDao.AddModifySyncStatus(task.id);
		SyncManager.TryDelaySync();
	}

	public static async Task TogglesStarred(string taskId, string projectId, bool? isPin = null, long? sortOrder = null, bool isInDetail = false, bool notify = true)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		TaskPinExtra notifyExt = new TaskPinExtra
		{
			Ids = new List<string>(),
			IsPin = (isPin == true)
		};
		DateTime modifyTime = DateTime.Now;
		if (task == null || !task.CheckEnable())
		{
			return;
		}
		if ((task.pinnedTimeStamp > 0 && !isPin.HasValue) || isPin == false)
		{
			notifyExt.IsPin = false;
			task.pinnedTimeStamp = -1L;
			notifyExt.Ids.Add(task.id);
			await ToggleSubTasks(notifyExt, await GetAllSubTasksByIdAsync(task.id, task.projectId), modifyTime, -1L);
		}
		else
		{
			notifyExt.IsPin = true;
			long pinnedTime = (task.pinnedTimeStamp = Utils.GetNowTimeStamp());
			if (string.IsNullOrEmpty(projectId))
			{
				projectId = task.projectId ?? string.Empty;
			}
			if (!sortOrder.HasValue)
			{
				await TaskSortOrderService.DeleteAsync("taskPinned", projectId, taskId);
			}
			else
			{
				await TaskSortOrderService.InsertOrUpdateAsync("taskPinned", projectId, taskId, 1, sortOrder);
			}
			notifyExt.Ids.Add(task.id);
			await ToggleSubTasks(notifyExt, await GetAllSubTasksByIdAsync(task.id, task.projectId), modifyTime, pinnedTime);
		}
		task.modifiedTime = modifyTime;
		await UpdateTaskPinned(task);
		await SyncStatusDao.AddModifySyncStatus(task.id);
		if (notify)
		{
			TaskChangeNotifier.NotifyBatchTaskStarredChanged(notifyExt);
		}
		SyncManager.TryDelaySync();
		static async Task ToggleSubTasks(TaskPinExtra notifyExtra, List<TaskModel> children, DateTime modifiedTime, long pinnedTimeStamp)
		{
			if (children != null && children.Any())
			{
				foreach (TaskModel child in children)
				{
					child.pinnedTimeStamp = pinnedTimeStamp;
					child.modifiedTime = modifiedTime;
					notifyExtra.Ids.Add(child.id);
					await UpdateTaskPinned(child);
					await SyncStatusDao.AddModifySyncStatus(child.id);
				}
			}
		}
	}

	public static async Task UndoDeleteTaskRecurrence(TaskDeleteRecurrenceUndoEntity undoEntity)
	{
		TaskModel task = await TaskDao.GetThinTaskById(undoEntity.sid);
		if (task != null)
		{
			task.status = undoEntity.taskStatus;
			task.startDate = ModelsDao.TTCalendarToDateTime(undoEntity.startDate);
			task.dueDate = ModelsDao.TTCalendarToDateTime(undoEntity.dueDate);
			task.remindTime = ModelsDao.TTCalendarToDateTime(undoEntity.snoozeRemindTime);
			task.repeatFlag = undoEntity.repeatFlag;
			task.exDate = undoEntity.exDate?.ToArray();
			task.exDates = ((task.exDate == null) ? string.Empty : ExDateSerilizer.ToString(task.exDate));
			await UpdateTaskOnTimeChanged(task);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	public static async Task SkipRecurrenceByDate(string taskId, DateTime date, IToastShowWindow toastWindow = null)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			List<string> list = ((!string.IsNullOrEmpty(task.exDates)) ? ExDateSerilizer.ToArray(task.exDates).ToList() : new List<string>());
			string item = date.ToString("yyyyMMdd");
			if (!list.Contains(item))
			{
				list.Add(item);
			}
			task.exDates = ExDateSerilizer.ToString(list.ToArray());
			await UpdateTaskOnTimeChanged(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			if (toastWindow != null && task.repeatFlag != null && task.repeatFlag.Contains("COUNT"))
			{
				toastWindow.TryToastString(null, Utils.GetString("SkipCountToast"));
			}
		}
	}

	public static async Task SetDateAndTitle(string taskId, string text, TimeData model)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task != null)
		{
			task.isAllDay = model.IsAllDay;
			task.startDate = model.StartDate;
			task.dueDate = model.DueDate;
			task.reminders = model.Reminders?.ToArray();
			task.repeatFrom = model.RepeatFrom;
			task.repeatFlag = model.RepeatFlag;
			task.exDates = string.Empty;
			task.isFloating = model.TimeZone.IsFloat;
			task.timeZone = model.TimeZone.TimeZoneName;
			task.annoyingAlert = model.AnnoyingAlert;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.modifiedTime = DateTime.Now;
			task.title = text;
			await SaveTaskReminders(task);
			await UpdateTask(task, CheckMatchedType.CheckSmart);
			await SyncStatusDao.AddModifySyncStatus(task.id);
			TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			TaskChangeNotifier.NotifyTaskTextChanged(task.id);
			if (!model.DueDate.HasValue)
			{
				ticktick_WPF.Util.DateUtils.CheckIfTomorrowWronglySet(model);
			}
		}
	}

	public static async Task UpdateTaskPomoSummary(string oldTaskId, string newTaskId, PomoTask pomoTask, List<PomoTask> pomoTasks, PomodoroModel pomoModel)
	{
		long oldTaskOriginDuration = 0L;
		long newTaskOriginDuration = 0L;
		foreach (PomoTask pomoTask2 in pomoTasks)
		{
			if (!string.IsNullOrEmpty(pomoTask2.TaskId))
			{
				if (pomoTask2.TaskId == oldTaskId)
				{
					oldTaskOriginDuration += Utils.GetTotalSecond(pomoTask2.StartTime, pomoTask2.EndTime);
				}
				else if (pomoTask2.TaskId == newTaskId)
				{
					newTaskOriginDuration += Utils.GetTotalSecond(pomoTask2.StartTime, pomoTask2.EndTime);
				}
			}
		}
		long duration = Utils.GetTotalSecond(pomoTask.StartTime, pomoTask.EndTime);
		long oldTaskDuration = oldTaskOriginDuration - duration;
		long newTaskDuration = newTaskOriginDuration + duration;
		TaskModel newTask = await TaskDao.GetThinTaskById(newTaskId);
		if (newTask != null && newTask.deleted == 0)
		{
			pomoTask.TaskId = newTask.id;
			pomoTask.HabitId = string.Empty;
			pomoTask.ProjectName = CacheManager.GetProjectById(newTask.projectId)?.name;
			pomoTask.TagString = newTask.tag;
			pomoTask.Title = newTask.title;
			PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(newTaskId);
			if (newTaskOriginDuration == 0L && pomodoroSummaryModel == null)
			{
				PomodoroSummaryModel pomodoroSummaryModel2 = new PomodoroSummaryModel();
				pomodoroSummaryModel2.id = Utils.GetGuid();
				pomodoroSummaryModel2.userId = LocalSettings.Settings.LoginUserId;
				pomodoroSummaryModel2.estimatedPomo = 0;
				pomodoroSummaryModel2.EstimatedDuration = 0L;
				pomodoroSummaryModel2.taskId = newTaskId;
				pomodoroSummaryModel2.focuses = new List<object[]> { new object[3] { pomoModel.Id, pomoModel.Type, newTaskDuration } };
				pomodoroSummaryModel = pomodoroSummaryModel2;
				await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
			}
			else if (pomodoroSummaryModel != null)
			{
				object[] array = pomodoroSummaryModel.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoModel.Id);
				if (array == null)
				{
					if (newTaskOriginDuration == 0L)
					{
						pomodoroSummaryModel.AddFocuses(new object[3] { pomoModel.Id, pomoModel.Type, newTaskDuration });
					}
					else if (pomoModel.Type == 0)
					{
						pomodoroSummaryModel.PomoDuration += duration;
						if (newTaskOriginDuration < Constants.MinFocusDuration && newTaskDuration >= Constants.MinFocusDuration)
						{
							pomodoroSummaryModel.count++;
						}
					}
					else
					{
						pomodoroSummaryModel.StopwatchDuration += duration;
					}
				}
				else
				{
					array[2] = newTaskDuration;
				}
				await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
			}
			newTask.modifiedTime = DateTime.Now;
			await TaskDao.UpdateTask(newTask);
			await SyncStatusDao.AddModifySyncStatus(newTask.id);
		}
		TaskModel oldTask = await TaskDao.GetThinTaskById(oldTaskId);
		if (oldTask == null)
		{
			return;
		}
		PomodoroSummaryModel pomodoroSummaryModel3 = await PomoSummaryDao.GetPomoByTaskId(oldTaskId);
		if (pomodoroSummaryModel3 != null)
		{
			object[] array2 = pomodoroSummaryModel3.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoModel.Id);
			if (array2 == null)
			{
				if (pomoModel.Type == 0)
				{
					pomodoroSummaryModel3.PomoDuration -= duration;
					if (oldTaskOriginDuration >= Constants.MinFocusDuration && oldTaskDuration < Constants.MinFocusDuration)
					{
						pomodoroSummaryModel3.count--;
					}
				}
				else
				{
					pomodoroSummaryModel3.StopwatchDuration -= duration;
				}
			}
			else if (oldTaskDuration == 0L)
			{
				pomodoroSummaryModel3.focuses.Remove(array2);
			}
			else
			{
				array2[2] = Math.Max(0L, oldTaskDuration);
			}
			pomodoroSummaryModel3.CheckNegative();
			await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel3);
		}
		oldTask.modifiedTime = DateTime.Now;
		await TaskDao.UpdateTask(oldTask);
		await SyncStatusDao.AddModifySyncStatus(oldTask.id);
	}

	public static async Task UncheckTaskItem(string taskId)
	{
		TaskBaseViewModel taskViewModel = TaskCache.GetTaskById(taskId);
		if (taskViewModel != null)
		{
			await TaskDetailItemService.UncheckDetailItemsByTaskId(taskId);
			taskViewModel.Status = 0;
			taskViewModel.CompletedTime = null;
			taskViewModel.CompletedUser = null;
			taskViewModel.RemindTime = null;
			taskViewModel.Progress = 0;
		}
		TaskModel task = await TaskDao.GetTaskById(taskId);
		if (task != null)
		{
			task.status = 0;
			task.completedTime = null;
			if (task.remindTime.HasValue)
			{
				task.remindTime = null;
				await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
			}
			task.progress = 0;
			await TaskDao.UpdateTask(task);
		}
	}

	public static async Task UpdateTaskOnMoveUndo(TaskModel task, CheckMatchedType checkMatched = CheckMatchedType.None)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Assignee = task.assignee;
			viewModel.ProjectId = task.projectId;
			viewModel.ParentId = task.parentId;
			viewModel.SortOrder = task.sortOrder;
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, checkMatched);
		}
	}

	public static async Task UpdateTaskColumnId(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.ColumnId = task.columnId;
			taskBaseViewModel.SortOrder = task.sortOrder;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			await SyncStatusDao.AddModifySyncStatus(task.id);
		}
	}

	public static async Task UpdateTaskParent(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.ParentId = task.parentId;
			viewModel.ColumnId = task.columnId;
			viewModel.ChildrenIds = task.childrenString;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.All);
		}
	}

	public static async Task UpdateTaskProject(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.ProjectId = task.projectId;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.All);
		}
	}

	public static async Task UpdateTask(TaskModel task, CheckMatchedType check = CheckMatchedType.All)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.SetProperties(new TaskBaseViewModel(task));
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, check);
		}
	}

	public static async Task<TaskBaseViewModel> UpdateTaskOnCompleteUndo(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Status = task.status;
			viewModel.CompletedTime = task.completedTime;
			viewModel.CompletedUser = task.completedUserId;
			viewModel.StartDate = task.startDate;
			viewModel.DueDate = task.dueDate;
			viewModel.RepeatFlag = task.repeatFlag;
			viewModel.RepeatFrom = task.repeatFrom;
			viewModel.ExDates = task.exDates;
			viewModel.Progress = task.progress.GetValueOrDefault();
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			return viewModel;
		}
		return null;
	}

	private static async Task UpdateTaskOnProjectChanged(TaskModel task, CheckMatchedType check = CheckMatchedType.CheckProject)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.ProjectId = task.projectId;
			viewModel.Assignee = task.assignee;
			viewModel.Deleted = task.deleted;
			viewModel.SortOrder = task.sortOrder;
			viewModel.ColumnId = task.columnId;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, check);
		}
	}

	private static async Task UpdateTaskOnStatusChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Status = task.status;
			viewModel.Progress = task.progress.GetValueOrDefault();
			viewModel.CompletedTime = task.completedTime;
			viewModel.CompletedUser = task.completedUserId;
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.All);
		}
	}

	private static async Task UpdateTaskPinned(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.PinnedTime = task.pinnedTimeStamp;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
		}
	}

	public static async Task ClearAssignByProjectId(string projectId, string userId)
	{
		List<TaskModel> list = await TaskDao.GetAssignTasksInProject(projectId, userId);
		List<TaskBaseViewModel> vms = new List<TaskBaseViewModel>();
		if (list != null && list.Count != 0)
		{
			foreach (TaskModel task in list)
			{
				task.assignee = "-1";
				TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
				taskBaseViewModel.Assignee = "-1";
				vms.Add(taskBaseViewModel);
				await TaskDao.UpdateTask(task);
				await SyncStatusDao.AddSyncStatus(task.id, 0);
			}
		}
		ProjectAndTaskIdsCache.OnTasksChanged(vms, CheckMatchedType.CheckAssign);
	}

	public static async Task UpdateTaskOnDeletedChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Deleted = task.deleted;
			viewModel.AttendId = task.attendId;
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.All);
		}
	}

	public static async Task UpdateTaskOnTimeChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.StartDate = task.startDate;
			viewModel.DueDate = task.dueDate;
			viewModel.IsAllDay = task.isAllDay;
			viewModel.RepeatFlag = task.repeatFlag;
			viewModel.ExDates = task.exDates;
			viewModel.RepeatFrom = task.repeatFrom;
			viewModel.RemindTime = task.remindTime;
			viewModel.IsFloating = task.isFloating == true;
			viewModel.TimeZoneName = task.timeZone;
			viewModel.ModifiedTime = task.modifiedTime;
			viewModel.AttendId = task.attendId;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.CheckSmart);
			ReminderCalculator.AssembleReminders();
		}
	}

	public static async Task UpdateTaskOnDetailChanged(TaskModel task, CheckMatchedType checkType)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, checkType);
		}
	}

	private static async Task UpdateTaskOnKindChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			viewModel.Kind = task.kind;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.All);
		}
	}

	private static async Task UpdateTaskOnProgressChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.Progress = task.progress.GetValueOrDefault();
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
		}
	}

	private static async Task UpdateTaskOnAssigneeChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel viewModel = TaskCache.SafeGetTaskViewModel(task);
			if (viewModel.Assignee != task.assignee)
			{
				viewModel.Assignee = task.assignee;
			}
			viewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			ProjectAndTaskIdsCache.OnTaskChanged(viewModel, CheckMatchedType.CheckAssign);
		}
	}

	public static async Task FoldTask(string taskId, bool isOpen)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			taskModel.isOpen = isOpen;
			TaskCache.SafeGetTaskViewModel(taskModel).IsOpen = taskModel.isOpen;
			await TaskDao.UpdateTask(taskModel);
		}
	}

	public static async Task UpdateTaskOnImgModeChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.ImageMode = task.imgMode;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
		}
	}

	public static async Task UpdateTaskOnTitleChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.Title = task.title;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
		}
	}

	public static async Task UpdateTaskOnContentChanged(TaskModel task)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.Content = task.content;
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
		}
	}

	public static async Task<List<TaskModel>> GetAllSubTasksByIdAsync(string taskId, string projectId)
	{
		return await TaskDao.GetTasksByIds(TaskCache.GetAllSubTasksById(taskId, projectId)?.Select((TaskBaseViewModel t) => t.Id).ToList());
	}

	public static List<TaskModel> GetSubTasks(string taskId, List<TaskModel> tasks, bool withDeleted = false)
	{
		if (tasks == null || string.IsNullOrEmpty(taskId))
		{
			return null;
		}
		Dictionary<string, TaskModel> taskDict = new Dictionary<string, TaskModel>();
		if (!withDeleted)
		{
			tasks = tasks.Where((TaskModel t) => t.deleted == 0).ToList();
		}
		foreach (TaskModel task in tasks)
		{
			if (!string.IsNullOrEmpty(task.id) && !taskDict.ContainsKey(task.id))
			{
				taskDict[task.id] = task;
			}
		}
		return tasks.Where((TaskModel task) => FindParent(task, new HashSet<string>())).ToList();
		bool FindParent(TaskModel task, HashSet<string> checkedIds)
		{
			while (true)
			{
				if (task.parentId == taskId)
				{
					return true;
				}
				if (string.IsNullOrEmpty(task.parentId) || !taskDict.ContainsKey(task.parentId))
				{
					return false;
				}
				if (checkedIds.Contains(task.parentId))
				{
					break;
				}
				checkedIds.Add(task.parentId);
				task = taskDict[task.parentId];
			}
			return false;
		}
	}

	public static async Task UndoTask(TaskModel task, List<TaskDetailItemModel> checkItems = null)
	{
		if (task == null)
		{
			return;
		}
		TaskModel current = await TaskDao.GetTaskById(task.id);
		if (current == null)
		{
			return;
		}
		bool changed = false;
		if (task.projectId != current.projectId)
		{
			changed = true;
			current.projectId = task.projectId;
		}
		if (task.parentId != current.parentId)
		{
			changed = true;
			current.parentId = task.parentId;
		}
		if (task.startDate != current.startDate)
		{
			changed = true;
			current.startDate = task.startDate;
		}
		if (task.dueDate != current.dueDate)
		{
			changed = true;
			current.dueDate = task.dueDate;
		}
		if (task.isAllDay != current.isAllDay)
		{
			changed = true;
			current.isAllDay = task.isAllDay;
		}
		if (task.repeatFlag != current.repeatFlag)
		{
			changed = true;
			current.repeatFlag = task.repeatFlag;
		}
		if (task.repeatFrom != current.repeatFrom)
		{
			changed = true;
			current.repeatFrom = task.repeatFrom;
		}
		if (task.exDates != current.exDates)
		{
			changed = true;
			current.exDates = task.exDates;
		}
		if (task.exDates != current.exDates)
		{
			changed = true;
			current.exDates = task.exDates;
		}
		if (task.tag != current.tag)
		{
			changed = true;
			current.tag = task.tag;
		}
		if (task.priority != current.priority)
		{
			changed = true;
			current.priority = task.priority;
		}
		bool flag = task.kind != current.kind;
		if (task.kind != current.kind)
		{
			changed = true;
			current.kind = task.kind;
		}
		if (task.progress != current.progress)
		{
			changed = true;
			current.progress = task.progress;
		}
		if (task.assignee != current.assignee)
		{
			changed = true;
			current.assignee = task.assignee;
		}
		if (checkItems != null && flag && current.kind == "CHECKLIST")
		{
			await TaskDetailItemDao.DeleteCheckItemsByTaskId(current.id);
			foreach (TaskDetailItemModel item in checkItems)
			{
				item.TaskServerId = current.id;
				await TaskDetailItemDao.InsertChecklistItem(item);
				ItemChangeNotifier.NotifyDeletedUndo(item);
			}
		}
		if (changed)
		{
			await UpdateTask(current);
			TaskChangeNotifier.NotifyTaskBatchChanged(new List<string> { task.id });
			SyncManager.TryDelaySync();
		}
	}

	public static async Task<TaskModel> CreateNewTask()
	{
		TaskModel task = new TaskModel();
		TaskDefaultModel taskDefault = TaskDefaultDao.GetDefaultSafely();
		if (await ProChecker.CheckTaskLimit(taskDefault.projectId))
		{
			return null;
		}
		task.startDate = taskDefault.GetDefaultDateTime();
		task.projectId = taskDefault.ProjectId;
		task.priority = taskDefault.Priority;
		task.isAllDay = true;
		task = await AddTask(task);
		if (!string.IsNullOrEmpty(taskDefault?.AllDayReminders))
		{
			List<TaskReminderModel> list = Utils.BuildReminders(taskDefault.AllDayReminders.Split(',').ToList());
			foreach (TaskReminderModel item in list)
			{
				item.taskserverid = task.id;
				await TaskReminderDao.SaveReminders(item);
			}
		}
		return task;
	}

	public static async Task TryToastMoveControl(ProjectIdentity identity, TaskModel task, string projectId, bool hideTitle = false)
	{
		if (identity != null && !(identity is CompletedProjectIdentity) && task != null && !MoveToastHelper.CheckTaskMatched(identity, task))
		{
			Utils.GetToastWindow()?.ToastMoveProjectControl(projectId, hideTitle ? string.Empty : task.title);
		}
	}

	public static async Task<bool> InEmptyTask(string taskId)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null)
		{
			switch (taskModel.kind)
			{
			case "TEXT":
			case "NOTE":
				if (string.IsNullOrEmpty(taskModel.content))
				{
					return true;
				}
				break;
			case "CHECKLIST":
			{
				if (!string.IsNullOrEmpty(taskModel.desc))
				{
					return false;
				}
				List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskModel.id);
				if (list == null || list.Count == 0)
				{
					return true;
				}
				if (list.Count == 1 && string.IsNullOrEmpty(list[0].title))
				{
					return true;
				}
				break;
			}
			}
		}
		return false;
	}

	public static bool IsTeamTask(string taskId)
	{
		TaskBaseViewModel taskById = TaskCache.GetTaskById(taskId);
		if (taskById != null)
		{
			ProjectModel projectById = CacheManager.GetProjectById(taskById.ProjectId);
			return TeamService.GetTeamId() == projectById?.teamId;
		}
		return false;
	}

	public static void DeleteTasksFromDb(List<TaskModel> needDelete)
	{
		foreach (TaskModel item in needDelete)
		{
			App.Connection.DeleteAsync(item);
			TaskDetailItemDao.DeleteCheckItemsByTaskId(item.id);
			App.Connection.ExecuteAsync("DELETE FROM AttachmentModel WHERE taskId=?", item.id);
			App.Connection.ExecuteAsync("DELETE FROM ReminderModel WHERE TaskId=?", item.id);
		}
	}

	public static async Task PullGuideTask()
	{
		List<TaskModel> list = await Communicator.GetGuideTask();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		long num = 0L;
		foreach (TaskModel item in list)
		{
			item.taskType = 1;
			item.sortOrder = num;
			item.userId = LocalSettings.Settings.LoginUserId;
			item.projectId = Utils.GetInboxId();
			num += 268435456;
		}
		await TaskDao.BatchInsertTasksAndItems(list);
	}

	public static async Task UpdateTaskTag(TaskModel task, bool needSync)
	{
		if (task != null)
		{
			TaskBaseViewModel taskBaseViewModel = TaskCache.SafeGetTaskViewModel(task);
			taskBaseViewModel.SetTags(task.tags?.ToList());
			taskBaseViewModel.ModifiedTime = task.modifiedTime;
			await TaskDao.UpdateTask(task);
			if (needSync)
			{
				await SyncStatusDao.AddModifySyncStatus(task.id);
			}
		}
	}
}
