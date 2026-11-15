using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Sync;

public class MoveColumnProjectBatchHandler : BatchHandler
{
	public MoveColumnProjectBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public static async Task<List<MoveColumnArgs>> GetCommits()
	{
		List<SyncStatusModel> columnStatuses = (await SyncStatusDao.GetSyncStatusByType(32)).DistinctEx((SyncStatusModel t) => t.EntityId);
		List<SyncStatusModel> source = (await SyncStatusDao.GetSyncStatusByType(2)) ?? new List<SyncStatusModel>();
		if (columnStatuses != null && columnStatuses.Count > 0)
		{
			Dictionary<string, List<SyncStatusModel>> taskMoveDict = source.GroupEx((SyncStatusModel t) => t.MoveFromId, (SyncStatusModel t) => t);
			List<MoveColumnArgs> args = new List<MoveColumnArgs>();
			foreach (SyncStatusModel columnStatus in columnStatuses)
			{
				ColumnModel column = await ColumnDao.GetColumnById(columnStatus.EntityId);
				if (column == null || column.deleted == 1)
				{
					continue;
				}
				MoveColumnArgs moveColumnArgs = new MoveColumnArgs
				{
					columnId = column.id,
					fromProjectId = columnStatus.MoveFromId,
					sortOrder = column.sortOrder.GetValueOrDefault(),
					toProjectId = column.projectId
				};
				if (moveColumnArgs.fromProjectId == moveColumnArgs.toProjectId)
				{
					await BaseDao<SyncStatusModel>.DeleteAsync(columnStatus);
					continue;
				}
				List<SyncStatusModel> source2 = (taskMoveDict.TryGetValue(columnStatus.MoveFromId, out var value) ? value : new List<SyncStatusModel>());
				source2 = source2.Where(delegate(SyncStatusModel task)
				{
					TaskBaseViewModel taskById = TaskCache.GetTaskById(task.EntityId);
					return (taskById != null && !(taskById.ColumnId != column.id) && !(taskById.ProjectId != column.projectId)) ? true : false;
				}).ToList();
				moveColumnArgs.taskIds = source2.Select((SyncStatusModel t) => t.EntityId).ToList();
				moveColumnArgs.tasks = source2;
				args.Add(moveColumnArgs);
			}
			return args;
		}
		return null;
	}

	public static async Task<bool> HandleCommitResult(BatchUpdateResult result, List<MoveColumnArgs> moveStatuses)
	{
		bool needReCommit = false;
		if (moveStatuses != null)
		{
			foreach (MoveColumnArgs moveStatus in moveStatuses)
			{
				if (result?.Id2etag != null && result.Id2etag.Count != 0 && result.Id2etag.ContainsKey(moveStatus.columnId))
				{
					await DeleteStatus(moveStatus);
				}
				if (result?.Id2error == null || result.Id2error.Count == 0 || !result.Id2error.ContainsKey(moveStatus.columnId))
				{
					continue;
				}
				UtilLog.Info("big sync: moveColumn id2error \\" + result.Id2error[moveStatus.columnId]);
				ProjectModel projectById = CacheManager.GetProjectById(moveStatus.fromProjectId);
				if (projectById == null || (moveStatus.fromProjectId != Utils.GetInboxId() && projectById.delete_status && projectById.sync_status != Constants.SyncStatus.SYNC_DONE.ToString()))
				{
					switch (result.Id2error[moveStatus.columnId])
					{
					case "FROM_PROJECT_NOT_EXISTED":
						await RestoreColumn(moveStatus, Utils.GetInboxId());
						await DeleteStatus(moveStatus);
						continue;
					case "COLUMN_NOT_EXISTED":
						await SyncStatusDao.DeleteSyncStatus(moveStatus.columnId, 32);
						continue;
					case "TO_PROJECT_NO_PROJECT_PERMISSION":
					case "TO_PROJECT_NOT_EXISTED":
					case "COLUMN_EXCEED_QUOTA":
						await RestoreColumn(moveStatus, Utils.GetInboxId());
						await SyncStatusDao.DeleteSyncStatus(moveStatus.columnId, 32);
						await SyncStatusDao.AddColumnMoveProjectStatus(moveStatus.columnId, Utils.GetInboxId());
						needReCommit = true;
						continue;
					}
				}
				await RestoreColumn(moveStatus, moveStatus.fromProjectId);
				await DeleteStatus(moveStatus);
			}
		}
		return needReCommit;
		static async Task DeleteStatus(MoveColumnArgs status)
		{
			await SyncStatusDao.DeleteSyncStatus(status.columnId, 32);
			foreach (SyncStatusModel task in status.tasks)
			{
				await SyncStatusDao.DeleteSyncStatus(task.EntityId, task.Type);
			}
		}
		static async Task RestoreColumn(MoveColumnArgs status, string projectId)
		{
			await ColumnDao.UpdateColumnProjectId(status.columnId, projectId);
			List<TaskModel> list = await TaskDao.GetTasksByIds(status.taskIds);
			if (list != null && list.Count > 0)
			{
				list.ForEach(delegate(TaskModel t)
				{
					t.projectId = projectId;
					TaskService.UpdateTaskProject(t);
				});
			}
		}
	}
}
