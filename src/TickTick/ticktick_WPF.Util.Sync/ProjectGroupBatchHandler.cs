using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class ProjectGroupBatchHandler : BatchHandler
{
	public ProjectGroupBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(Collection<ProjectGroupModel> serverGroups, LogModel syncBeanLog)
	{
		List<ProjectGroupModel> added = new List<ProjectGroupModel>();
		List<ProjectGroupModel> updated = new List<ProjectGroupModel>();
		syncBeanLog.Log += $"\r\n\tMergeGroup num :{serverGroups?.Count}";
		if (serverGroups == null)
		{
			return;
		}
		Dictionary<string, ProjectGroupModel> localGroups = await ProjectGroupDao.GetLocalSyncedProjectGroupMap();
		foreach (ProjectGroupModel server in serverGroups)
		{
			if (localGroups.ContainsKey(server.id))
			{
				ProjectGroupModel local = localGroups[server.id];
				localGroups.Remove(server.id);
				if ((string.IsNullOrEmpty(local.etag) || !local.etag.Equals(server.etag)) && !local.sync_status.Equals(Constants.SyncStatus.SYNC_NEW.ToString()))
				{
					ProjectGroupSyncedJsonModel projectGroupSyncedJsonModel = await ProjectGroupSyncedJsonDao.GetSavedJson(server.id);
					if (projectGroupSyncedJsonModel != null)
					{
						MergeUtils.Merge(JsonConvert.DeserializeObject<ProjectGroupModel>(projectGroupSyncedJsonModel.JsonString), server, local);
						local.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
						updated.Add(local);
						continue;
					}
					server._Id = local._Id;
					server.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
					server.open = local.open;
					server.Timeline = local.Timeline ?? new TimelineModel(Constants.SortType.project.ToString());
					server.Timeline.SortType = server.SyncTimeline?.SortType ?? server.Timeline.SortType;
					server.Timeline.sortOption = server.SyncTimeline?.SortOption ?? server.Timeline.sortOption;
					updated.Add(server);
				}
			}
			else
			{
				server.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
				server.userId = int.Parse(userId);
				server.Timeline = ((server.SyncTimeline == null) ? null : new TimelineModel(server.SyncTimeline.SortType, server.SyncTimeline.SortOption));
				added.Add(server);
			}
		}
		List<ProjectGroupModel> deleted = localGroups.Values.Where((ProjectGroupModel v) => v.sync_status == Constants.SyncStatus.SYNC_DONE.ToString()).ToList();
		syncBeanLog.Log += $" a:{added.Count} u:{updated.Count} d:{deleted.Count}";
		if (added.Count > 0 || updated.Count > 0 || deleted.Count > 0)
		{
			await ProjectGroupDao.SaveServerMergeData(added, updated, deleted);
			if (syncResult == null)
			{
				syncResult = new SyncResult();
			}
			syncResult.AddedProjectGroups = added;
			syncResult.UpdatedProjectGroups = updated;
			syncResult.DeletedProjectGroups = deleted;
		}
	}

	public static async Task CommitToServer(LogModel logModel)
	{
		List<ProjectGroupModel> list = await ProjectGroupDao.GetNeedPostProjectGroup();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		SyncProjectGroupBean syncProjectGroupBean = ProjectGroupTransfer.DescribeSyncProjectGroupBean(list);
		if (syncProjectGroupBean != null && !syncProjectGroupBean.Empty)
		{
			logModel.Log = logModel.Log + "\r\n" + $"big sync:  update group count : a {syncProjectGroupBean.Add.Count} u {syncProjectGroupBean.Update.Count} d {syncProjectGroupBean.Deleted.Count}";
			BatchUpdateResult batchUpdateResult = await Communicator.BatchUpdateProjectGroup(syncProjectGroupBean);
			if (batchUpdateResult != null)
			{
				await HandleCommitResult(batchUpdateResult.Id2etag, batchUpdateResult.Id2error, logModel);
			}
		}
	}

	private static async Task HandleCommitResult(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, LogModel logModel)
	{
		if (id2Etag != null)
		{
			logModel.Log += "Id2etag : ";
			foreach (string serverId2 in id2Etag.Keys)
			{
				ProjectGroupModel projectGroup = await ProjectGroupDao.GetProjectGroupById(serverId2);
				if (projectGroup != null)
				{
					projectGroup.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
					projectGroup.etag = id2Etag[serverId2];
					await App.Connection.UpdateAsync(projectGroup);
					CacheManager.UpdateProjectGroup(projectGroup);
					logModel.Log = logModel.Log + serverId2 + "  ";
				}
			}
			await ProjectGroupSyncedJsonDao.BatchDeleteGroups(id2Etag.Keys.ToList());
		}
		if (id2Error == null)
		{
			return;
		}
		string serverId = string.Empty;
		foreach (string serverId2 in id2Error.Keys)
		{
			ProjectGroupModel projectGroup = await ProjectGroupDao.GetProjectGroupById(serverId2);
			if (projectGroup != null)
			{
				string text = id2Error[serverId2];
				if (!(text == "EXISTED"))
				{
					if (text == "NOT_EXISTED")
					{
						projectGroup.sync_status = ((projectGroup.deleted == 0) ? Constants.SyncStatus.SYNC_NEW.ToString() : Constants.SyncStatus.SYNC_DONE.ToString());
						await App.Connection.UpdateAsync(projectGroup);
					}
				}
				else
				{
					projectGroup.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					await App.Connection.UpdateAsync(projectGroup);
				}
			}
			CacheManager.UpdateProjectGroup(projectGroup);
			serverId = serverId + serverId2 + " : " + id2Error[serverId2];
		}
		logModel.Log = logModel.Log + "   error " + serverId;
	}
}
