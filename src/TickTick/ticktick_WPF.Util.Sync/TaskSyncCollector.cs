using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util.Sync;

public static class TaskSyncCollector
{
	public static async Task<TaskSyncModel> CollectTaskSyncBean(SyncTaskBean syncTaskBean, int type, bool ignoreEtag)
	{
		TaskSyncModel syncModel = new TaskSyncModel();
		await CollectDeletedTaskFromRemoteModel(syncTaskBean, syncModel);
		if (syncTaskBean != null && syncTaskBean.TagUpdate?.Count > 0)
		{
			await MergeUpdatedTasksTagFromServer(syncTaskBean.TagUpdate);
		}
		if (syncTaskBean != null && syncTaskBean.Update?.Count > 0)
		{
			await MergeUpdatedTasksFromServer(syncModel, syncTaskBean?.Update, type, ignoreEtag);
		}
		return syncModel;
	}

	private static async Task CollectDeletedTaskFromRemoteModel(SyncTaskBean syncTaskBean, TaskSyncModel syncModel)
	{
		await CollectDeleteInTrash(syncTaskBean, syncModel);
		await CollectDeleteForever(syncTaskBean, syncModel);
	}

	private static async Task CollectDeleteInTrash(SyncTaskBean syncTaskBean, TaskSyncModel syncModel)
	{
		List<TaskProject> trashTasks = syncTaskBean?.DeletedInTrash;
		if (trashTasks == null || trashTasks.Count <= 0)
		{
			return;
		}
		List<string> status = await SyncStatusDao.GetEntityIdsByType(7);
		List<string> list = status;
		list.AddRange(await SyncStatusDao.GetEntityIdsByType(6));
		foreach (TaskProject task in trashTasks)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(task.taskId);
			if (taskModel != null && taskModel.deleted != 2 && !status.Contains(task.taskId))
			{
				syncModel.TaskSyncBean.DeletedInTrash.Add(taskModel);
			}
		}
	}

	private static async Task CollectDeleteForever(SyncTaskBean syncTaskBean, TaskSyncModel syncModel)
	{
		List<TaskProject> list = syncTaskBean?.DeletedForever;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskProject item in list)
		{
			TaskModel taskModel = await TaskDao.GetThinTaskById(item.taskId);
			if (taskModel != null)
			{
				syncModel.TaskSyncBean.DeletedForever.Add(taskModel);
			}
		}
	}

	public static async Task MergeUpdatedTasksTagFromServer(List<TaskModel> update)
	{
		HashSet<string> contentChangeTaskIds = await SyncStatusDao.GetEntityIdSetByType(0);
		Dictionary<string, TaskEtag> localTasks = await TaskDao.GetTask2SidMap(update.Select((TaskModel t) => t.id).ToList());
		List<Task> list = new List<Task>();
		int num = 0;
		list.Clear();
		for (; num < update.Count; num++)
		{
			TaskModel serverTask = update[num];
			list.Add(MergeServerTask(serverTask));
		}
		await Task.WhenAll(list);
		async Task MergeServerTask(TaskModel taskModel)
		{
			if (localTasks.ContainsKey(taskModel.id) && (!(localTasks[taskModel.id].Etag == taskModel.etag) || taskModel.TagsNeedSync))
			{
				if (contentChangeTaskIds.Contains(taskModel.id))
				{
					TaskModel localTask = await TaskDao.GetThinTaskById(taskModel.id);
					TaskModel originalTask = null;
					TaskSyncedJsonModel taskSyncedJsonModel = await TaskSyncedJsonDao.GetJsonByTaskId(localTask.id);
					if (taskSyncedJsonModel != null)
					{
						originalTask = JsonConvert.DeserializeObject<TaskModel>(taskSyncedJsonModel.jsonString);
					}
					if (originalTask != null)
					{
						try
						{
							MergeUtils.MergeTaskTags(originalTask, taskModel, localTask);
							localTask.TagsNeedSync |= taskModel.TagsNeedSync;
							localTask.etag = taskModel.etag;
							await TaskService.UpdateTaskTag(localTask, needSync: false);
						}
						catch (Exception ex)
						{
							UtilLog.Error("MergeTaskError: " + ExceptionUtils.BuildExceptionMessage(ex));
						}
					}
				}
				else
				{
					TaskModel obj = await TaskDao.GetThinTaskById(taskModel.id);
					obj.tags = taskModel.tags;
					obj.TagsNeedSync |= taskModel.TagsNeedSync;
					obj.etag = taskModel.etag;
					obj.tag = TagSerializer.ToJsonContent(taskModel.tags?.ToList());
					await TaskService.UpdateTaskTag(obj, needSync: false);
				}
			}
		}
	}

	public static async Task MergeUpdatedTasksFromServer(TaskSyncModel syncModel, List<TaskModel> update, int type, bool ignoreEtag)
	{
		HashSet<string> projectIds = await ProjectDao.GetProjectIdSet();
		HashSet<string> contentChangeTaskIds = await SyncStatusDao.GetEntityIdSetByType(0);
		HashSet<string> deleted = await SyncStatusDao.GetEntityIdSetByType(5);
		HashSet<string> changeParent = await SyncStatusDao.GetEntityIdSetByType(16);
		List<SyncStatusModel> moveStatus = await SyncStatusDao.GetSyncStatusByType(2);
		int sameEtag = 0;
		int ignoreCount = update.Count;
		update = update.Where((TaskModel t) => t != null && projectIds.Contains(t.projectId)).ToList();
		if (deleted.Any() || UndoHelper.DeletingIds.Any())
		{
			update = update.Where((TaskModel t) => !deleted.Contains(t.id) && !UndoHelper.DeletingIds.Contains(t.id)).ToList();
		}
		ignoreCount -= update.Count;
		Dictionary<string, TaskEtag> localTasks = await TaskDao.GetTask2SidMap(update.Select((TaskModel t) => t.id).ToList());
		int updateCount = 0;
		foreach (TaskModel item in update)
		{
			await MergeServerTask(item);
			updateCount++;
		}
		UtilLog.Info(string.Format("\r\n\tpull task merge updateCount : {0}, {1} {2}", updateCount, (ignoreCount > 0) ? $" ignoreCount:{ignoreCount}" : "", (sameEtag > 0) ? $" sameEtag:{sameEtag}" : ""));
		async Task MergeServerTask(TaskModel serverTask)
		{
			if (localTasks.ContainsKey(serverTask.id))
			{
				TaskEtag taskEtag = localTasks[serverTask.id];
				if (taskEtag.Etag == serverTask.etag && !serverTask.TagsNeedSync && type != 1 && !ignoreEtag)
				{
					sameEtag++;
				}
				else
				{
					await AttachmentSyncCollector.CollectRemoteAttachments(serverTask, syncModel.AttachmentSyncBean);
					if (contentChangeTaskIds.Contains(serverTask.id))
					{
						TaskModel localTask = await TaskDao.GetFullTaskById(serverTask.id);
						TaskModel originalTask = null;
						TaskSyncedJsonModel taskSyncedJsonModel = await TaskSyncedJsonDao.GetJsonByTaskId(localTask.id);
						if (taskSyncedJsonModel != null)
						{
							originalTask = JsonConvert.DeserializeObject<TaskModel>(taskSyncedJsonModel.jsonString);
						}
						if (originalTask != null)
						{
							try
							{
								MergeUtils.Merge(originalTask, serverTask, localTask);
								localTask.TagsNeedSync |= serverTask.TagsNeedSync;
							}
							catch (Exception ex)
							{
								UtilLog.Error("MergeTaskError: " + ExceptionUtils.BuildExceptionMessage(ex));
							}
						}
						localTask.notionBlock = serverTask.notionBlock;
						if (!changeParent.Contains(localTask.id))
						{
							localTask.parentId = serverTask.parentId;
							localTask.childIds = serverTask.childIds;
						}
						if (moveStatus != null)
						{
							SyncStatusModel syncStatusModel = moveStatus.FirstOrDefault((SyncStatusModel m) => m.EntityId == localTask.id);
							if (syncStatusModel != null)
							{
								if (syncStatusModel.MoveFromId != serverTask.projectId)
								{
									syncStatusModel.MoveFromId = serverTask.projectId;
									await BaseDao<SyncStatusModel>.UpdateAsync(syncStatusModel);
								}
							}
							else
							{
								localTask.projectId = serverTask.projectId;
							}
						}
						syncModel.TaskSyncBean.Updating.Add(localTask);
					}
					else
					{
						serverTask._Id = taskEtag._Id;
						TaskBaseViewModel taskById = TaskCache.GetTaskById(serverTask.id);
						serverTask.Actions = taskById?.Actions;
						serverTask.Resources = taskById?.Resources;
						serverTask.label = taskById?.Label;
						syncModel.TaskSyncBean.Updated.Add(serverTask);
						syncModel.TaskSyncedJsonBean.Updated.Add(serverTask);
					}
				}
			}
			else
			{
				AttachmentModel[] attachments = serverTask.Attachments;
				if (attachments != null && attachments.Length != 0)
				{
					AttachmentModel[] attachments2 = serverTask.Attachments;
					foreach (AttachmentModel attachmentModel in attachments2)
					{
						if (attachmentModel != null)
						{
							attachmentModel.fileType = AttachmentProvider.GetFileType(attachmentModel.fileName).ToString();
						}
					}
				}
				syncModel.TaskSyncBean.Added.Add(serverTask);
				syncModel.TaskSyncedJsonBean.Added.Add(serverTask);
			}
		}
	}
}
