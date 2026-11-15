using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class ColumnBatchHandler : BatchHandler
{
	public ColumnBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public static async Task CommitToServer(LogModel logModel)
	{
		Dictionary<string, string> dictionary = await ColumnDao.GetNeedPostColumns();
		if (dictionary == null || !dictionary.Any())
		{
			return;
		}
		SyncColumnBean syncColumnBean = await ColumnTransfer.DescribeSyncColumnBean(dictionary);
		if (syncColumnBean.Add.Any() || syncColumnBean.Update.Any() || syncColumnBean.Deleted.Any())
		{
			logModel.Log = logModel.Log + "\r\n" + $"big sync:  update column count : a {syncColumnBean.Add.Count} u {syncColumnBean.Update.Count} d {syncColumnBean.Deleted.Count}";
			BatchUpdateResult batchUpdateResult = await Communicator.BatchUpdateColumns(syncColumnBean);
			if (batchUpdateResult != null)
			{
				await ColumnTransfer.HandleCommitResult(batchUpdateResult.Id2etag, batchUpdateResult.Id2error, logModel);
			}
		}
	}

	public static async Task<bool> MergeWithServer(SyncColumnBean bean, LogModel syncBeanLog)
	{
		List<ColumnModel> added = new List<ColumnModel>();
		List<ColumnModel> updated = new List<ColumnModel>();
		List<ColumnModel> serverColumns = new List<ColumnModel>();
		serverColumns.AddRange(bean.Add ?? new List<ColumnModel>());
		serverColumns.AddRange(bean.Update ?? new List<ColumnModel>());
		if (serverColumns.Any())
		{
			Dictionary<string, ColumnModel> dictionary = await ColumnDao.GetLocalSyncedColumnMap();
			foreach (ColumnModel item in serverColumns)
			{
				if (string.IsNullOrEmpty(item.userId))
				{
					item.userId = Utils.GetCurrentUserIdInt().ToString();
				}
				if (dictionary.ContainsKey(item.id))
				{
					ColumnModel columnModel = dictionary[item.id];
					dictionary.Remove(item.id);
					if ((string.IsNullOrEmpty(columnModel.etag) || string.IsNullOrEmpty(item.etag) || !(columnModel.etag == item.etag)) && columnModel.syncStatus.Equals("done"))
					{
						item._Id = columnModel._Id;
						item.syncStatus = "done";
						updated.Add(item);
					}
				}
				else
				{
					item.syncStatus = "done";
					added.Add(item);
				}
			}
		}
		if (bean.Deleted != null && bean.Deleted.Any())
		{
			await ColumnDao.DeleteColumns(bean.Deleted);
		}
		if (added.Count > 0 || updated.Count > 0)
		{
			await ColumnDao.SaveServerMergeData(added, updated, new List<ColumnModel>());
		}
		string empty = string.Empty;
		string empty2 = string.Empty;
		syncBeanLog.Log = syncBeanLog.Log + "\r\n\tmergeColumns u" + updated.Aggregate(empty, (string text, ColumnModel column) => text + column?.id + ",") + "  a" + added.Aggregate(empty2, (string text, ColumnModel column) => text + column?.id + ",");
		return added.Count > 0 || updated.Count > 0 || (bean.Deleted != null && bean.Deleted.Any());
	}

	public static async Task<bool> MergeWithServer(string projectId)
	{
		ProjectModel projectModel = await ProjectDao.GetProjectById(projectId);
		if (projectModel == null || projectModel.IsNew())
		{
			return false;
		}
		List<ColumnModel> added = new List<ColumnModel>();
		List<ColumnModel> updated = new List<ColumnModel>();
		List<ColumnModel> serverColumns = await Communicator.GetRemoteColumnsByProjectId(projectId);
		List<SyncStatusModel> moveColumnStatus = await SyncStatusDao.GetSyncStatusByType(32);
		if (serverColumns != null && serverColumns.Any())
		{
			Dictionary<string, ColumnModel> dictionary = await ColumnDao.GetLocalSyncedColumnMap(projectId);
			foreach (ColumnModel serverColumn in serverColumns)
			{
				if (string.IsNullOrEmpty(serverColumn.userId))
				{
					serverColumn.userId = Utils.GetCurrentUserIdInt().ToString();
				}
				if (dictionary.ContainsKey(serverColumn.id))
				{
					ColumnModel columnModel = dictionary[serverColumn.id];
					dictionary.Remove(serverColumn.id);
					if ((string.IsNullOrEmpty(columnModel.etag) || string.IsNullOrEmpty(serverColumn.etag) || !(columnModel.etag == serverColumn.etag) || columnModel.deleted == 1) && columnModel.syncStatus.Equals("done"))
					{
						serverColumn._Id = columnModel._Id;
						serverColumn.syncStatus = "done";
						updated.Add(serverColumn);
					}
				}
				else if (!moveColumnStatus.Any((SyncStatusModel c) => c.EntityId == serverColumn.id && c.MoveFromId == serverColumn.projectId))
				{
					serverColumn.syncStatus = "done";
					added.Add(serverColumn);
				}
			}
			List<ColumnModel> list = dictionary.Values.Where((ColumnModel column) => ((moveColumnStatus.Count == 0 || moveColumnStatus.All((SyncStatusModel c) => c.EntityId != column.id)) && column.syncStatus == "done") || column.syncStatus == "update").ToList();
			UtilLog.Info("Merge ProjectColumns " + projectId + " : server " + serverColumns?.ToString() + $"a {added.Count},u {updated.Count},d {list.Count}");
			if (added.Count > 0 || updated.Count > 0 || list.Count > 0)
			{
				await ColumnDao.SaveServerMergeData(added, updated, list);
				DataChangedNotifier.NotifyColumnChanged(projectId);
				return true;
			}
		}
		return false;
	}
}
