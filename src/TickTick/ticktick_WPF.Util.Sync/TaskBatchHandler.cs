using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class TaskBatchHandler : BatchHandler
{
	public TaskBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(SyncTaskBean syncTaskBean, int type, LogModel logModel, bool ignoreEtag)
	{
		TaskSyncModel taskSyncModel = await TaskSyncCollector.CollectTaskSyncBean(syncTaskBean, type, ignoreEtag);
		if (syncResult == null)
		{
			syncResult = new SyncResult();
		}
		if (syncTaskBean != null && taskSyncModel != null)
		{
			syncResult.AddedTasks = taskSyncModel.TaskSyncBean.Added;
			syncResult.UpdatedTasks = taskSyncModel.TaskSyncBean.Updated;
			syncResult.UpdatedTasks.AddRange(taskSyncModel.TaskSyncBean.Updating);
			syncResult.DeletedTasks = taskSyncModel.TaskSyncBean.DeletedInTrash.Select((TaskModel task) => new TaskProject
			{
				projectId = task.projectId,
				taskId = task.id
			}).ToList();
		}
		await BatchSaveTaskSyncModel(taskSyncModel);
		if (syncTaskBean != null && taskSyncModel != null)
		{
			logModel.Log += $"\r\n\tpull task a:{syncTaskBean.Add.Count} u:{syncTaskBean.Update.Count} d:{syncTaskBean.Delete.Count} result a:{taskSyncModel.TaskSyncBean.Added.Count} ud:{taskSyncModel.TaskSyncBean.Updated.Count} ug:{taskSyncModel.TaskSyncBean.Updating.Count} d:{taskSyncModel.TaskSyncBean.DeletedInTrash.Count} df:{taskSyncModel.TaskSyncBean.DeletedForever.Count}";
			logModel.Log += $"  |  pull attachment a:{taskSyncModel.AttachmentSyncBean.Added.Count} ud:{taskSyncModel.AttachmentSyncBean.Updated.Count} d:{taskSyncModel.AttachmentSyncBean.Deleted.Count}";
		}
	}

	private static async Task BatchSaveTaskSyncModel(TaskSyncModel taskSyncModel)
	{
		await SaveTaskSyncBean(taskSyncModel.TaskSyncBean);
		await SaveTaskOtherEntity(taskSyncModel);
		await TaskSyncedJsonDao.SaveTaskSyncedJsons(taskSyncModel.TaskSyncedJsonBean);
	}

	private static async Task SaveTaskOtherEntity(TaskSyncModel taskSyncModel)
	{
		if (!taskSyncModel.AttachmentSyncBean.Empty)
		{
			await AttachmentDao.SaveServerMergeToDb(taskSyncModel.AttachmentSyncBean);
		}
		List<TaskModel> list = new List<TaskModel>();
		list.AddRange(taskSyncModel.TaskSyncBean.Added);
		list.AddRange(taskSyncModel.TaskSyncBean.Updated);
		list.AddRange(taskSyncModel.TaskSyncBean.Updating);
		await TagService.CheckTaskTags(list);
	}

	private static async Task SaveTaskSyncBean(TaskSyncBean taskSyncBean)
	{
		if (taskSyncBean.DeletedInTrash.Count > 0)
		{
			foreach (TaskModel item in taskSyncBean.DeletedInTrash)
			{
				await TaskDao.DeleteTaskToTrash(item.id);
			}
		}
		if (taskSyncBean.DeletedForever.Count > 0)
		{
			foreach (TaskModel item2 in taskSyncBean.DeletedForever)
			{
				await TaskDao.DeleteTaskForever(item2.id);
			}
		}
		if (taskSyncBean.Added.Count > 0)
		{
			await TaskDao.BatchCreateTaskFromRemote(taskSyncBean.Added, needHandleItem: false);
		}
		if (taskSyncBean.Updated.Count > 0 || taskSyncBean.Updating.Count > 0)
		{
			await TaskDao.BatchUpdateTasksFromRemote(taskSyncBean);
		}
		TaskChangeNotifier.OnServerTaskChanged(taskSyncBean.GetIds());
	}

	public async Task<List<SyncStatusModel>> GetNeedPostCreatedTasks()
	{
		return await TaskDao.GetNeedPostTasks(4);
	}

	public async Task<List<SyncStatusModel>> GetNeedClearTrash()
	{
		return await TaskDao.GetNeedPostTasks(8);
	}

	public static async Task HandleCommitResult(SyncTaskBean syncTaskBean, BatchUpdateResult result, LogModel logModel)
	{
		bool emptyExisted = false;
		logModel.Log = logModel.Log + " etag : " + result?.Id2etag?.Count;
		if (result == null)
		{
			logModel.Log += " result null";
			return;
		}
		if (result.Id2error != null && result.Id2error.Count > 0)
		{
			string log = string.Empty;
			foreach (KeyValuePair<string, string> error in result.Id2error)
			{
				string value = error.Value;
				if (!(value == "EXISTED"))
				{
					if (value == "UNKNOWN")
					{
						await SyncStatusDao.DeleteSyncStatus(error.Key, 6);
						await SyncStatusDao.DeleteSyncStatus(error.Key, 7);
					}
				}
				else
				{
					await SyncStatusDao.DeleteSyncStatus(error.Key, 4, checkRecommit: false);
					await SyncStatusDao.AddModifySyncStatus(error.Key);
				}
				log = log + " " + error.Key + ":" + error.Value + " ";
			}
			logModel.Log = logModel.Log + " error : " + log;
		}
		List<string> list = await SyncStatusDao.GetEntityIdsByType(4);
		if (list != null && list.Count > 0)
		{
			logModel.Log += " create : ";
			foreach (string log in list)
			{
				if (!string.IsNullOrEmpty(log))
				{
					UpdateEtag(result, log);
					if (result.Id2etag != null && result.Id2etag.Count != 0 && result.Id2etag.ContainsKey(log))
					{
						if (await SyncStatusDao.DeleteSyncStatus(log, 4, checkRecommit: false))
						{
							await SyncStatusDao.AddSyncStatus(log, 0);
						}
						logModel.Log = logModel.Log + log + ",";
					}
				}
				else
				{
					emptyExisted = true;
				}
			}
		}
		List<string> list2 = await SyncStatusDao.GetEntityIdsByType(5);
		if (list2 != null && list2.Count > 0)
		{
			foreach (string item in list2)
			{
				if (!string.IsNullOrEmpty(item))
				{
					UpdateEtag(result, item);
					if (result.Id2error == null || result.Id2error.Count == 0 || !result.Id2error.Keys.Contains(item))
					{
						await SyncStatusDao.DeleteSyncStatus(item, 5);
					}
				}
			}
		}
		List<string> list3 = await SyncStatusDao.GetEntityIdsByType(6);
		if (list3 != null && list3.Count > 0)
		{
			foreach (string item2 in list3)
			{
				if (!string.IsNullOrEmpty(item2))
				{
					UpdateEtag(result, item2);
					if (result.Id2etag != null && result.Id2etag.Count != 0 && result.Id2etag.Keys.Contains(item2))
					{
						await SyncStatusDao.DeleteSyncStatus(item2, 6);
					}
				}
			}
		}
		List<string> list4 = await SyncStatusDao.GetEntityIdsByType(0);
		if (list4 != null && list4.Count > 0)
		{
			logModel.Log += " update : ";
			foreach (string item3 in list4)
			{
				if (item3 != null)
				{
					UpdateEtag(result, item3);
					if (result.Id2etag != null && result.Id2etag.Count != 0 && result.Id2etag.ContainsKey(item3))
					{
						logModel.Log = logModel.Log + item3 + ",";
						await SyncStatusDao.DeleteSyncStatus(item3, 0);
					}
				}
				else
				{
					emptyExisted = true;
				}
			}
		}
		if (result.Id2etag != null && syncTaskBean != null && result.Id2etag.Count > 0)
		{
			TaskSyncedJsonBean taskSyncedJsonBean = new TaskSyncedJsonBean();
			if (syncTaskBean.Add != null && syncTaskBean.Add.Count > 0)
			{
				foreach (TaskModel item4 in syncTaskBean.Add)
				{
					taskSyncedJsonBean.Added.Add(item4);
				}
			}
			if (syncTaskBean.Update != null && syncTaskBean.Update.Count > 0)
			{
				foreach (TaskModel item5 in syncTaskBean.Update)
				{
					taskSyncedJsonBean.Updated.Add(item5);
				}
			}
			await TaskSyncedJsonDao.SaveTaskSyncedJsons(taskSyncedJsonBean);
		}
		if (emptyExisted)
		{
			await SyncStatusDao.DeleteEmptySyncStatus();
		}
	}

	private static async void UpdateEtag(BatchUpdateResult result, string taskSid)
	{
		if (!string.IsNullOrWhiteSpace(taskSid) && result?.Id2etag != null && result.Id2etag.ContainsKey(taskSid))
		{
			TaskModel taskModel = await TaskDao.GetTaskById(taskSid);
			if (taskModel != null)
			{
				taskModel.etag = result.Id2etag[taskSid];
				await TaskDao.UpdateTask(taskModel);
			}
		}
	}

	public async Task<List<SyncStatusModel>> GetNeedPostDeletedTasks()
	{
		return await TaskDao.GetNeedPostTasks(5);
	}

	public async Task<List<SyncStatusModel>> GetNeedPostDeleteForeverTasks()
	{
		return await TaskDao.GetNeedPostTasks(6);
	}

	public async Task<List<SyncStatusModel>> GetLocalContentUpdatedTasks()
	{
		return await TaskDao.GetNeedPostTasks(0);
	}

	public async Task MergeTasksOfOpenedProjects(Dictionary<string, ObservableCollection<TaskModel>> serverTasksDict)
	{
		ObservableCollection<TaskModel> obj = await TaskDao.GetTasksInProjectIds(serverTasksDict.Keys.ToList());
		Dictionary<string, TaskModel> id2Tasks = new Dictionary<string, TaskModel>();
		foreach (TaskModel item in obj)
		{
			id2Tasks[item.id] = item;
		}
		List<TaskModel> addOrUpdateServerTasks = new List<TaskModel>();
		foreach (ObservableCollection<TaskModel> value in serverTasksDict.Values)
		{
			if (value == null)
			{
				continue;
			}
			foreach (TaskModel item2 in value)
			{
				if (id2Tasks.ContainsKey(item2.id))
				{
					TaskModel taskModel = id2Tasks[item2.id];
					if (taskModel != null)
					{
						id2Tasks.Remove(item2.id);
						if (item2.TagsNeedSync || !(item2.etag == taskModel.etag) || item2.annoyingAlert != taskModel.annoyingAlert)
						{
							addOrUpdateServerTasks.Add(item2);
						}
					}
				}
				else
				{
					addOrUpdateServerTasks.Add(item2);
				}
			}
		}
		foreach (string taskId in id2Tasks.Keys)
		{
			TaskModel taskModel2 = id2Tasks[taskId];
			if (taskModel2 == null || taskModel2.status == 0)
			{
				Dictionary<int, SyncStatusModel> dictionary = await SyncStatusDao.GetSyncStatusMap(taskId);
				if (dictionary == null || (!dictionary.ContainsKey(4) && !dictionary.ContainsKey(2) && !dictionary.ContainsKey(7)))
				{
					TaskDao.DeleteTaskInDb(taskId);
				}
			}
		}
		TaskSyncModel taskSyncModel = new TaskSyncModel();
		await TaskSyncCollector.MergeUpdatedTasksFromServer(taskSyncModel, addOrUpdateServerTasks, 1, ignoreEtag: true);
		await BatchSaveTaskSyncModel(taskSyncModel);
		if (!taskSyncModel.TaskSyncBean.Empty)
		{
			TaskChangeNotifier.NotifyTaskBatchChanged(taskSyncModel.TaskSyncBean.GetIds());
		}
	}

	public static async Task HandleRepeatCompletedTasks(SyncTaskBean syncBean)
	{
		if (syncBean == null)
		{
			return;
		}
		List<TaskModel> list = new List<TaskModel>();
		if (syncBean.Add != null && syncBean.Add.Any())
		{
			list.AddRange(syncBean.Add);
		}
		if (syncBean.Update != null && syncBean.Update.Any())
		{
			list.AddRange(syncBean.Update);
		}
		if (!list.Any())
		{
			return;
		}
		List<TaskModel> remoteCompletedTasks = list.Where((TaskModel task) => task.status != 0 && !string.IsNullOrEmpty(task.repeatTaskId) && task.id != null && task.id != task.repeatTaskId).ToList();
		if (!remoteCompletedTasks.Any())
		{
			return;
		}
		List<TaskModel> localCompletedTasks = await TaskDao.GetUnSyncCompletedTasks();
		if (localCompletedTasks == null || !localCompletedTasks.Any())
		{
			return;
		}
		List<TaskModel> duplicatedTasks = (from serverTask in remoteCompletedTasks
			select localCompletedTasks.FirstOrDefault((TaskModel localTask) => localTask.repeatTaskId != null && serverTask.repeatTaskId != null && localTask.repeatTaskId == serverTask.repeatTaskId && localTask.startDate.HasValue && serverTask.startDate.HasValue && localTask.startDate.Value == serverTask.startDate.Value) into duplicated
			where duplicated != null
			select duplicated).ToList();
		if (!duplicatedTasks.Any())
		{
			return;
		}
		await SyncStatusDao.BatchRemoveAddSyncStatus(duplicatedTasks.Select((TaskModel task) => task.id).ToList());
		foreach (TaskModel item in duplicatedTasks)
		{
			await TaskDao.DeleteTaskForever(item.id);
		}
	}

	public static async void HandleSetParentResult(Dictionary<string, string> resultId2Etag)
	{
	}
}
