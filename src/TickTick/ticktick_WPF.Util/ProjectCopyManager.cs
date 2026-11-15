using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Service.SortOrder;

namespace ticktick_WPF.Util;

public static class ProjectCopyManager
{
	public static async Task<TaskModel> CopyTask(TaskModel origin, string projectId, string newId, bool checkParent = true, bool keepChecklistItemStatus = false, int status = 0)
	{
		if (origin == null)
		{
			return null;
		}
		TaskModel copy = origin.Clone();
		copy.status = status;
		copy.id = newId;
		copy.projectId = projectId;
		copy.assignee = origin.assignee;
		copy.commentCount = "0";
		copy.createdTime = DateTime.Now;
		copy.modifiedTime = DateTime.Now;
		if (!keepChecklistItemStatus)
		{
			copy.progress = 0;
		}
		TaskModel taskModel = copy;
		taskModel.content = await CopyAttachments(origin.id, copy.id, copy.content);
		copy.AddPinnedSecond();
		if (copy.pinnedTimeStamp > 0)
		{
			await TaskSortOrderService.InsertOrUpdateAsync("taskPinned", copy.projectId, newId);
		}
		TaskModel taskModel2 = await TaskDao.InsertTask(copy);
		await CopyReminders(origin.id, taskModel2.id);
		await CopyChecklistItems(origin.id, taskModel2.id, keepChecklistItemStatus);
		await CopyPomodoroSummaries(origin.id, taskModel2.id);
		await SyncStatusDao.AddCreateSyncStatus(copy.id);
		if (checkParent && !string.IsNullOrEmpty(copy.parentId))
		{
			await TaskDao.AddOrRemoveTaskChildIds(copy.parentId, new List<string> { copy.id }, addChild: true);
			await SyncStatusDao.AddSetParentSyncStatus(copy.id, "");
		}
		return copy;
	}

	public static async Task<List<string>> TryCopySubTasks(string originId, string taskId, string taskProjectId, int status = 0, bool keepOriginalStatus = false)
	{
		Dictionary<string, string> dict = new Dictionary<string, string> { { originId, taskId } };
		Dictionary<string, List<string>> parentChildrenDic = new Dictionary<string, List<string>>();
		List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(originId, taskProjectId);
		List<string> copiedTaskIds = new List<string>();
		if (list != null && list.Count > 0)
		{
			foreach (TaskModel item in list)
			{
				string guid = Utils.GetGuid();
				dict.Add(item.id, guid);
			}
			foreach (TaskModel item2 in list)
			{
				if (!string.IsNullOrEmpty(item2.parentId) && dict.ContainsKey(item2.parentId))
				{
					item2.parentId = dict[item2.parentId];
				}
				int status2 = status;
				if (keepOriginalStatus)
				{
					status2 = item2.status;
				}
				TaskModel taskModel = await CopyTask(item2, taskProjectId, dict[item2.id], checkParent: false, keepChecklistItemStatus: false, status2);
				if (!string.IsNullOrEmpty(taskModel.parentId))
				{
					if (parentChildrenDic.ContainsKey(taskModel.parentId))
					{
						parentChildrenDic[taskModel.parentId].Add(taskModel.id);
					}
					else
					{
						parentChildrenDic[taskModel.parentId] = new List<string> { taskModel.id };
					}
				}
				copiedTaskIds.Add(taskModel.id);
			}
			foreach (KeyValuePair<string, List<string>> item3 in parentChildrenDic)
			{
				string key = item3.Key;
				List<string> childIds = item3.Value;
				await TaskDao.AddOrRemoveTaskChildIds(key, childIds, addChild: true);
				foreach (string item4 in childIds)
				{
					await SyncStatusDao.AddSetParentSyncStatus(item4, "");
				}
			}
		}
		return copiedTaskIds;
	}

	private static async Task CopyPomodoroSummaries(string originId, string copyTaskSid)
	{
		await PomoSummaryDao.SavePomoSummaries((from item in (await PomoSummaryDao.GetPomosByTaskId(originId)).ToArray()
			select item.Copy(copyTaskSid, clear: true)).ToList());
	}

	private static async Task<string> CopyAttachments(string originalTaskId, string copyTaskSid, string taskContent)
	{
		List<AttachmentModel> list = await AttachmentDao.GetTaskAttachments(originalTaskId);
		if (list != null && list.Count > 0)
		{
			foreach (AttachmentModel attachment in list)
			{
				if (attachment.status != 1)
				{
					AttachmentModel copy = new AttachmentModel
					{
						id = Utils.GetGuid(),
						refId = attachment.refId,
						path = attachment.path,
						localPath = attachment.localPath,
						size = attachment.size,
						fileName = attachment.fileName,
						fileType = attachment.fileType,
						createdTime = attachment.createdTime,
						taskId = copyTaskSid,
						deleted = attachment.deleted,
						sync_status = attachment.sync_status
					};
					AttachmentCache.SetAttachment(copy);
					await AttachmentDao.InsertOrUpdateAttachment(copy);
					taskContent = taskContent?.Replace(attachment.id, copy.id);
				}
			}
		}
		return taskContent;
	}

	public static async Task CopyReminders(string originalTaskId, string copyTaskSid)
	{
		List<TaskReminderModel> list = await TaskReminderDao.GetRemindersByTaskId(originalTaskId);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskReminderModel item in list)
		{
			await TaskReminderDao.SaveReminders(new TaskReminderModel
			{
				id = Utils.GetGuid(),
				taskserverid = copyTaskSid,
				trigger = item.trigger
			});
		}
	}

	public static async Task CopyChecklistItems(string originalTaskId, string copyTaskSid, bool keepStatus = true)
	{
		List<TaskDetailItemModel> list = await TaskDetailItemModels(originalTaskId, copyTaskSid, keepStatus);
		if (list != null && list.Count > 0)
		{
			await TaskDetailItemDao.BatchInsertChecklists(list);
		}
	}

	private static async Task<List<TaskDetailItemModel>> TaskDetailItemModels(string originalTaskId, string copyTaskSid, bool keepStatus)
	{
		List<TaskDetailItemModel> copylist = new List<TaskDetailItemModel>();
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(originalTaskId);
		if (list != null && list.Count > 0)
		{
			copylist.AddRange(list.Select((TaskDetailItemModel item) => new TaskDetailItemModel
			{
				id = Utils.GetGuid(),
				TaskServerId = copyTaskSid,
				title = item.title,
				status = (keepStatus ? item.status : 0),
				sortOrder = item.sortOrder,
				completedTime = item.completedTime,
				startDate = item.startDate,
				isAllDay = item.isAllDay,
				snoozeReminderTime = item.snoozeReminderTime
			}));
		}
		return copylist;
	}
}
