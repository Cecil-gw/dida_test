using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class MoveOrRestoreProjectBatchHandler : BatchHandler
{
	public MoveOrRestoreProjectBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public static async Task<List<MoveOrRestoreProject>> GetCommits(int moveOrRestore)
	{
		List<SyncStatusModel> tasks = await TaskDao.GetNeedPostMovedOrRestoreTasks(moveOrRestore);
		if (tasks != null && tasks.Count > 0)
		{
			List<SyncStatusModel> list = await SyncStatusDao.GetSyncStatusByType(moveOrRestore);
			Dictionary<string, string> moveFromDict = new Dictionary<string, string>();
			if (list != null && list.Count > 0)
			{
				foreach (SyncStatusModel item2 in list)
				{
					if (!string.IsNullOrEmpty(item2.EntityId) && !moveFromDict.ContainsKey(item2.EntityId))
					{
						moveFromDict.Add(item2.EntityId, item2.MoveFromId);
					}
				}
			}
			List<MoveOrRestoreProject> projects = new List<MoveOrRestoreProject>();
			switch (moveOrRestore)
			{
			case 2:
			{
				for (int i = 0; i < tasks.Count; i++)
				{
					TaskModel taskModel2 = await TaskDao.GetTaskById(tasks[i].EntityId);
					if (taskModel2 != null)
					{
						MoveProject moveProject = new MoveProject
						{
							TaskId = taskModel2.id,
							FromProjectId = (moveFromDict.ContainsKey(taskModel2.id) ? moveFromDict[taskModel2.id] : taskModel2.projectId),
							SortOrder = taskModel2.sortOrder,
							ToProjectId = taskModel2.projectId
						};
						if (moveProject.FromProjectId == moveProject.ToProjectId)
						{
							await BaseDao<SyncStatusModel>.DeleteAsync(tasks[i]);
						}
						else
						{
							projects.Add(moveProject);
						}
					}
				}
				return projects;
			}
			case 7:
			{
				for (int i = 0; i < tasks.Count; i++)
				{
					TaskModel taskModel = await TaskDao.GetTaskById(tasks[i].EntityId);
					if (taskModel != null)
					{
						RestoreProject item = new RestoreProject
						{
							TaskId = taskModel.id,
							FromProjectId = (moveFromDict.ContainsKey(taskModel.id) ? moveFromDict[taskModel.id] : taskModel.projectId),
							ToProjectId = taskModel.projectId
						};
						projects.Add(item);
					}
				}
				return projects;
			}
			}
		}
		return null;
	}

	public static async void HandleCommitResult(BatchUpdateResult result, int moveOrRestore)
	{
		List<string> list = await SyncStatusDao.GetEntityIdsByType(moveOrRestore);
		if (list == null)
		{
			return;
		}
		foreach (string taskSid in list)
		{
			if (result?.Id2etag != null && result.Id2etag.Count != 0 && result.Id2etag.ContainsKey(taskSid))
			{
				await SyncStatusDao.DeleteSyncStatus(taskSid, moveOrRestore);
				TaskModel taskModel = await TaskDao.GetTaskById(taskSid);
				if (taskModel != null)
				{
					taskModel.etag = result.Id2etag[taskSid];
					await TaskDao.UpdateTask(taskModel);
				}
			}
			if (result?.Id2error != null && result.Id2error.Count != 0 && result.Id2error.ContainsKey(taskSid))
			{
				UtilLog.Info("big sync: move id2error \\" + result.Id2error[taskSid]);
				await SyncStatusDao.DeleteSyncStatus(taskSid, moveOrRestore);
			}
		}
	}
}
