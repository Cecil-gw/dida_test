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
using ticktick_WPF.Util.Twitter;

namespace ticktick_WPF.Util.Sync;

public class ProjectBatchHandler : BatchHandler
{
	public ProjectBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(Collection<ProjectModel> serverProjects, LogModel logModel, long lastCheckPoint)
	{
		List<ProjectModel> added = new List<ProjectModel>();
		List<ProjectModel> updated = new List<ProjectModel>();
		logModel.Log += $"  |  MergeProjects num :{serverProjects?.Count}";
		if (serverProjects == null)
		{
			return;
		}
		Dictionary<string, ProjectModel> localMap = await ProjectDao.GetLocalSyncedProjectMap(userId);
		List<ProjectSyncedJsonModel> list = await ProjectSyncJsonDao.GetProjectSyncedModels();
		foreach (ProjectModel delta in serverProjects)
		{
			if (!LocalSettings.Settings.IsNoteEnabled && delta.kind == Constants.ProjectKind.NOTE.ToString())
			{
				LocalSettings.Settings.IsNoteEnabled = true;
				LocalSettings.Settings.Save();
			}
			if (delta.userid == null)
			{
				delta.userid = userId;
			}
			if (localMap.ContainsKey(delta.id))
			{
				ProjectModel projectModel = localMap[delta.id];
				localMap.Remove(delta.id);
				if ((projectModel.etag != null && delta.etag != null && projectModel.etag.Equals(delta.etag)) || projectModel.delete_status)
				{
					continue;
				}
				if (projectModel.sync_status == null)
				{
					projectModel.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
				}
				if (projectModel.sync_status.Equals(Constants.SyncStatus.SYNC_UPDATE.ToString()) || projectModel.sync_status.Equals(Constants.SyncStatus.SYNC_INIT.ToString()))
				{
					ProjectModel original = projectModel;
					ProjectSyncedJsonModel projectSyncedJsonModel = list?.FirstOrDefault((ProjectSyncedJsonModel p) => p.UserId == userId && p.ProjectId == delta.id);
					if (projectSyncedJsonModel != null)
					{
						original = JsonConvert.DeserializeObject<ProjectModel>(projectSyncedJsonModel.JsonString);
					}
					MergeUtils.Merge(original, delta, projectModel);
					projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					updated.Add(projectModel);
				}
				else if (projectModel.sync_status.Equals(Constants.SyncStatus.SYNC_DONE.ToString()))
				{
					delta._Id = projectModel._Id;
					delta.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
					delta.ShowAddColumn = projectModel.ShowAddColumn;
					delta.Timeline = projectModel.Timeline ?? new TimelineModel();
					delta.Timeline.SortType = delta.SyncTimeline?.SortType ?? delta.Timeline.SortType;
					delta.Timeline.sortOption = delta.SyncTimeline?.SortOption ?? delta.Timeline.sortOption;
					delta.ParallelViewMode = projectModel.ParallelViewMode;
					updated.Add(delta);
				}
			}
			else
			{
				delta.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
				delta.userid = userId;
				delta.Timeline = ((delta.SyncTimeline == null) ? null : new TimelineModel(delta.SyncTimeline.SortType, delta.SyncTimeline.SortOption));
				if (lastCheckPoint > 0 && (delta.transferred == true || delta.userCount > 1))
				{
					delta.needPullRemote = true;
				}
				added.Add(delta);
			}
		}
		List<ProjectModel> deleted = localMap.Values.Where((ProjectModel project) => project.sync_status == Constants.SyncStatus.SYNC_DONE.ToString() && !project.delete_status).ToList();
		logModel.Log = logModel.Log + " a:" + added.Select((ProjectModel a) => a.id).Join(",") + " u:" + updated.Select((ProjectModel a) => a.id).Join(",") + " d:" + deleted.Select((ProjectModel a) => a.id).Join(",");
		if (added.Count > 0 || updated.Count > 0 || deleted.Count > 0)
		{
			await ProjectDao.SaveServerMergeData(added, updated, deleted);
			if (syncResult == null)
			{
				syncResult = new SyncResult();
			}
			syncResult.AddedProjects = added;
			syncResult.UpdatedProjects = updated;
			syncResult.DeletedProjects = deleted;
		}
	}

	public static async Task CommitToServer(bool delete, LogModel logModel)
	{
		Dictionary<string, string> dictionary = await ProjectDao.GetNeedPostProject();
		if (dictionary == null || dictionary.Count <= 0)
		{
			return;
		}
		SyncProjectBean syncBean = await ProjectTransfer.DescribeSyncProjectBean(dictionary);
		if (!syncBean.Empty)
		{
			if (delete)
			{
				syncBean.Update.RemoveAll((ProjectModel p) => p.closed != true);
				syncBean.Add.Clear();
				logModel.Log = logModel.Log + "\r\n" + $"big sync: delete project count : {syncBean.Deleted.Count}";
			}
			else
			{
				syncBean.Deleted.Clear();
				syncBean.Update.RemoveAll((ProjectModel p) => p.closed == true);
				logModel.Log = logModel.Log + "\r\n" + $"big sync: update project count : a {syncBean.Add.Count} u {syncBean.Update.Count}";
			}
		}
		if (!syncBean.Empty)
		{
			BatchUpdateResult batchUpdateResult = await Communicator.BatchUpdateProject(syncBean);
			if (batchUpdateResult != null)
			{
				await HandleCommitResult(batchUpdateResult.Id2etag, batchUpdateResult.Id2error, syncBean, logModel);
			}
		}
	}

	private static async Task HandleCommitResult(Dictionary<string, string> id2Etag, Dictionary<string, string> id2Error, SyncProjectBean bean, LogModel logModel)
	{
		if (id2Etag != null && id2Etag.Any())
		{
			List<ProjectModel> projects = bean.Update;
			projects.AddRange(bean.Add);
			logModel.Log += "Id2etag : ";
			foreach (string serverId in id2Etag.Keys)
			{
				ProjectModel projectModel = await ProjectDao.GetProjectById(serverId);
				if (projectModel != null)
				{
					ProjectModel projectModel2 = projects.FirstOrDefault((ProjectModel p) => p.id == serverId);
					if (projectModel2 == null || projectModel2.modifiedTime >= projectModel.modifiedTime)
					{
						projectModel.sync_status = Constants.SyncStatus.SYNC_DONE.ToString();
					}
					else
					{
						projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					}
					projectModel.etag = id2Etag[serverId];
				}
				CacheManager.UpdateProject(projectModel, notify: false);
				await App.Connection.UpdateAsync(projectModel);
				logModel.Log = logModel.Log + serverId + "  ";
			}
			await ProjectSyncJsonDao.BatchDeleteProjects(id2Etag.Keys.ToList());
		}
		if (id2Error != null && id2Error.Count > 0)
		{
			string log = string.Empty;
			foreach (string serverId2 in id2Error.Keys)
			{
				ProjectModel project = await ProjectDao.GetProjectById(serverId2);
				if (project != null)
				{
					switch (id2Error[serverId2])
					{
					case "EXISTED":
						project.SetSyncStatus(Constants.SyncStatus.SYNC_UPDATE.ToString());
						await App.Connection.UpdateAsync(project);
						break;
					case "NO_TEAM_PERMISSION":
						project.SetSyncStatus(Constants.SyncStatus.SYNC_DONE.ToString());
						await App.Connection.UpdateAsync(project);
						break;
					case "EXCEED_QUOTA":
						project.SetSyncStatus(Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString());
						await App.Connection.UpdateAsync(project);
						break;
					case "NO_PROJECT_PERMISSION":
						project.SetSyncStatus(Constants.SyncStatus.SYNC_DONE.ToString());
						await App.Connection.UpdateAsync(project);
						break;
					case "UNKNOWN":
						project.SetSyncStatus(Constants.SyncStatus.SYNC_DONE.ToString());
						await App.Connection.UpdateAsync(project);
						break;
					}
					CacheManager.UpdateProject(project, notify: false);
				}
				log = log + " " + serverId2 + " : " + id2Error[serverId2] + " ";
			}
			logModel.Log = logModel.Log + "error " + log;
		}
		if (bean.Deleted == null || bean.Deleted.Count <= 0)
		{
			return;
		}
		foreach (string item in bean.Deleted)
		{
			ProjectModel projectModel3 = await ProjectDao.GetProjectById(item);
			if (projectModel3 != null)
			{
				App.Connection.DeleteAsync(projectModel3);
			}
		}
		await ProjectSyncJsonDao.BatchDeleteProjects(bean.Deleted.ToList());
	}
}
