using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.NewUser;

namespace ticktick_WPF.Dal;

public class ProjectDao : BaseDao<ProjectModel>
{
	private static async Task<List<string>> GetProjectIds()
	{
		string text = Utils.GetCurrentUserIdInt().ToString();
		return await BaseDao<ProjectModel>.GetEntityIds("select id as Id from ProjectModel where userId = '" + text + "'");
	}

	public static async Task<ObservableCollection<ProjectModel>> GetAllProjects(bool withInbox = true, bool withClosed = true)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return new ObservableCollection<ProjectModel>(await (from p in App.Connection.Table<ProjectModel>()
			where p.userid == userId && p.delete_status == false && (withInbox || p.Isinbox == false) && (withClosed || p.closed == (bool?)null || p.closed == (bool?)false)
			orderby p.sortOrder
			select p).ToListAsync());
	}

	public static async Task<ObservableCollection<ProjectModel>> GetProjectsWithoutClosed(bool withInbox = true)
	{
		return await GetAllProjects(withInbox, withClosed: false);
	}

	public static async Task<ObservableCollection<ProjectModel>> GetProjectsInGroup(string groupId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return new ObservableCollection<ProjectModel>(await (from v in App.Connection.Table<ProjectModel>()
			where v.userid == userId && v.groupId.Equals(groupId) && v.delete_status == false
			orderby v.sortOrder
			select v).ToListAsync());
	}

	public static async Task<ProjectModel> GetMinOrderProjectInGroup(string groupId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return ((await (from v in App.Connection.Table<ProjectModel>()
			where v.userid == userId && v.groupId.Equals(groupId) && v.delete_status == false
			select v).ToListAsync())?.Where((ProjectModel v) => v.IsEnable()).ToList())?.OrderBy((ProjectModel v) => v.sortOrder).FirstOrDefault();
	}

	public static async Task<bool> CheckUnSyncProjects()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		string syncDone = Constants.SyncStatus.SYNC_DONE.ToString();
		return (await (from v in App.Connection.Table<ProjectModel>()
			where v.userid == userId && v.sync_status != null && v.sync_status != "" && v.sync_status != syncDone
			select v).ToListAsync()).Count == 0;
	}

	public static async Task CreateProject(ProjectModel project, bool notify = true)
	{
		if (string.IsNullOrEmpty(project.userid))
		{
			project.userid = Utils.GetCurrentUserIdInt().ToString();
		}
		await App.Connection.InsertAsync(project);
		CacheManager.UpdateProject(project, notify);
	}

	public static async Task<bool> DeleteProjectById(string projectId, bool showTaskDeleteConfirm = true)
	{
		ProjectModel project = await GetProjectById(projectId);
		if (project != null)
		{
			List<TaskModel> tasks = await TaskDao.GetTasksInProjectAsync(projectId);
			if (!project.IsShareList() && showTaskDeleteConfirm && !TaskService.ConfirmBatchDeleteTasks(tasks.Count))
			{
				return false;
			}
			if (project.IsNew())
			{
				await App.Connection.DeleteAsync(project);
			}
			else
			{
				project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
				project.delete_status = true;
				await App.Connection.UpdateAsync(project);
			}
			if (!project.IsShareList())
			{
				await TaskService.BatchDeleteTasks(tasks, showUndo: false, 1, null, null, showConfirm: false);
			}
			else
			{
				foreach (TaskModel task in tasks)
				{
					await App.Connection.DeleteAsync(task);
					TaskCache.DeleteTask(task.id);
				}
			}
			CacheManager.DeleteProject(project);
			List<SyncStatusModel> moveColumn = await SyncStatusDao.GetSyncStatusByType(32);
			((await ColumnDao.GetColumnsByProjectId(project.id))?.Where((ColumnModel c) => moveColumn.Any((SyncStatusModel m) => m.EntityId == c.id)).ToList())?.ForEach(delegate(ColumnModel c)
			{
				BaseDao<ColumnModel>.DeleteAsync(c);
			});
			return true;
		}
		return false;
	}

	public static async Task<int> TryUpdateProject(ProjectModel project)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<ProjectModel> list = await (from v in App.Connection.Table<ProjectModel>()
			where v.id.Equals(project.id) && v.userid == userId
			select v).ToListAsync();
		if (list.Count == 0)
		{
			list = await (from v in App.Connection.Table<ProjectModel>()
				where v._Id.Equals(project._Id) && v.userid == userId
				select v).ToListAsync();
		}
		if (list.Count != 0)
		{
			ProjectModel projectModel = list[0];
			if ((projectModel.sync_status == Constants.SyncStatus.SYNC_NEW.ToString() || projectModel.sync_status == Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString()) && project.sync_status == Constants.SyncStatus.SYNC_UPDATE.ToString())
			{
				project.sync_status = projectModel.sync_status;
			}
			project._Id = list[0]._Id;
			project.name = project.name.Trim();
			project.modifiedTime = DateTime.Now;
			CacheManager.UpdateProject(project);
			await ProjectSyncJsonDao.TryAddProjectJson(project.id);
			await App.Connection.UpdateAsync(project);
			return project._Id;
		}
		await App.Connection.InsertAsync(project);
		CacheManager.UpdateProject(project);
		return project._Id;
	}

	public static async Task<Dictionary<string, ProjectModel>> GetLocalSyncedProjectMap(string userId)
	{
		Dictionary<string, ProjectModel> projectDict = new Dictionary<string, ProjectModel>();
		List<ProjectModel> list = await (from v in App.Connection.Table<ProjectModel>()
			where v.userid.Equals(userId)
			select v).ToListAsync();
		if (list.Count > 0)
		{
			foreach (ProjectModel item in list)
			{
				if (!projectDict.ContainsKey(item.id) && !item.id.StartsWith("inbox"))
				{
					projectDict.Add(item.id, item);
				}
			}
		}
		return projectDict;
	}

	public static async Task SaveServerMergeData(List<ProjectModel> added, List<ProjectModel> updated, List<ProjectModel> deleted)
	{
		if (added.Count > 0 && added.Count > 0)
		{
			List<string> list = (await GetProjectIds()) ?? new List<string>();
			List<ProjectModel> list2 = new List<ProjectModel>();
			foreach (ProjectModel item in added)
			{
				item.name = item.name?.Trim();
				if (!list.Contains(item.id))
				{
					list2.Add(item);
				}
			}
			if (list2.Count > 0)
			{
				await App.Connection.InsertAllAsync(list2);
				foreach (ProjectModel item2 in added)
				{
					CacheManager.UpdateProject(item2, notify: false);
					if (item2.IsValid() && item2.IsShareList())
					{
						AvatarHelper.GetProjectUsersAsync(item2.id);
					}
				}
			}
		}
		if (updated.Count > 0)
		{
			updated.ForEach(delegate(ProjectModel projectModel)
			{
				projectModel.name = projectModel.name?.Trim();
			});
			await App.Connection.UpdateAllAsync(updated);
			foreach (ProjectModel item3 in updated)
			{
				CacheManager.UpdateProject(item3, notify: false);
			}
			await ProjectSyncJsonDao.BatchDeleteProjects(updated);
		}
		if (deleted.Count > 0)
		{
			List<SyncStatusModel> moveColumn = await SyncStatusDao.GetSyncStatusByType(32);
			foreach (SyncStatusModel m in moveColumn)
			{
				ColumnModel column = await ColumnDao.GetColumnById(m.EntityId);
				if (column != null && deleted.Any((ProjectModel d) => d.id == column.projectId))
				{
					column.projectId = m.MoveFromId;
					if (!deleted.Any((ProjectModel d) => d.id == column.projectId))
					{
						await ColumnDao.UpdateColumn(column);
					}
					else
					{
						await BaseDao<ColumnModel>.DeleteAsync(column);
					}
					await BaseDao<SyncStatusModel>.DeleteAsync(m);
				}
			}
			foreach (SyncStatusModel m in await SyncStatusDao.GetSyncStatusByType(2))
			{
				TaskModel task = await TaskDao.GetTaskById(m.EntityId);
				if (task != null && deleted.Any((ProjectModel d) => d.id == task.projectId))
				{
					task.projectId = m.MoveFromId;
					if (!deleted.Any((ProjectModel d) => d.id == task.projectId))
					{
						await TaskService.UpdateTaskProject(task);
					}
					else
					{
						await TaskDao.DeleteTaskInDb(task.id);
						UtilLog.Info("SaveServerMergeData.TaskDeleted " + task.id);
					}
					await BaseDao<SyncStatusModel>.DeleteAsync(m);
				}
			}
			foreach (ProjectModel project in deleted)
			{
				if (project.id.StartsWith("inbox"))
				{
					continue;
				}
				TaskStickyWindow.TryCloseInProject(project.id);
				await App.Connection.DeleteAsync(project);
				CacheManager.DeleteProject(project);
				((await ColumnDao.GetColumnsByProjectId(project.id))?.Where((ColumnModel c) => moveColumn.Any((SyncStatusModel syncStatusModel) => syncStatusModel.EntityId != c.id)).ToList())?.ForEach(delegate(ColumnModel c)
				{
					BaseDao<ColumnModel>.DeleteAsync(c);
				});
				foreach (TaskModel item4 in await TaskDao.GetTasksInProjectAsync(project.id))
				{
					if (project.IsShareList() && !project.isOwner)
					{
						App.Connection.DeleteAsync(item4);
					}
					else
					{
						TaskDao.DeleteTaskToTrash(item4.id);
					}
				}
			}
		}
		DataChangedNotifier.NotifyProjectChanged();
	}

	public static async Task<Dictionary<string, string>> GetNeedPostProject()
	{
		List<ProjectSyncStatusModel> list = await App.Connection.QueryAsync<ProjectSyncStatusModel>("select id as ProjectId, sync_status as SyncStatus from ProjectModel where sync_status is not null and sync_status <> 'SYNC_DONE' and userId = '" + Utils.GetCurrentUserIdInt() + "'", Array.Empty<object>());
		Dictionary<string, string> dict = new Dictionary<string, string>();
		if (list != null && list.Count > 0)
		{
			foreach (ProjectSyncStatusModel item in list.Where((ProjectSyncStatusModel statusModel) => !dict.ContainsKey(statusModel.ProjectId)))
			{
				dict.Add(item.ProjectId, item.SyncStatus);
			}
		}
		return dict;
	}

	public static async Task<HashSet<string>> GetProjectIdSet()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		return await BaseDao<ProjectModel>.GetEntityIdSet("select id as Id from ProjectModel where userId = '" + loginUserId + "'");
	}

	public static async Task<ProjectModel> GetProjectById(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from p in App.Connection.Table<ProjectModel>()
			where p.userid == userId && p.id == id
			select p).FirstOrDefaultAsync();
	}

	public static List<ProjectModel> GetProjectsInIdsOrGroupIds(List<string> ids, List<string> groupIds)
	{
		return (from p in CacheManager.GetProjects()
			where (ids != null && ids.Contains(p.id)) || (groupIds != null && groupIds.Contains(p.groupId))
			orderby p.sortOrder
			select p).ToList();
	}

	public static long GetNewProjectOrder(string teamId)
	{
		List<long> list = (from p in CacheManager.GetProjects()
			where !p.Isinbox && p.IsValid() && p.teamId == teamId
			select p.sortOrder).ToList();
		List<long> collection = (from g in CacheManager.GetProjectGroups()
			where g.teamId == teamId
			select g.sortOrder.GetValueOrDefault()).ToList();
		list.AddRange(collection);
		if (list.Count > 0)
		{
			list.Sort();
			return list[0] - 268435456;
		}
		return 0L;
	}

	public static async Task SaveInboxColor(string color)
	{
		ProjectModel inbox = await GetProjectById(Utils.GetInboxId());
		if (inbox != null && inbox.color != color)
		{
			inbox.color = color;
			await App.Connection.UpdateAsync(inbox);
			CacheManager.UpdateProject(inbox);
		}
	}

	public static void SetProjectDefaultColumn(string projectId, string columnId)
	{
		TaskCache.SetLastDefaultColumnId(projectId, columnId);
	}

	public static async Task<int> GetTaskOverLimitProjectNum()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		string sql = "select count(id) from (select t.id,t.projectId from TaskModel t, ProjectModel p where t.userId = '" + loginUserId + "' and p.userId = '" + loginUserId + "' and t.projectId = p.id and (p.closed is null or p.closed = false)) group by projectId";
		List<int> list = await App.Connection.QueryAsync<int>(sql, Array.Empty<object>());
		if (list != null && list.Count > 0)
		{
			return list.Where((int num) => num > 99).ToList().Count;
		}
		return 0;
	}

	public static bool IsValidProject(string projectId)
	{
		if (CalendarUtils.IsAllListId(projectId))
		{
			return true;
		}
		return CacheManager.GetProjectById(projectId)?.IsValid() ?? false;
	}

	public static async Task<List<string>> GetProjectIdByGroupId(string groupId)
	{
		return await BaseDao<ProjectModel>.GetEntityIds("select id from projectModel where groupId = " + groupId);
	}

	public static async Task UpdateInboxSortType(SortOption sortOption)
	{
		ProjectModel projectById = CacheManager.GetProjectById(Utils.GetInboxId());
		if (projectById != null)
		{
			projectById.SortOption = sortOption;
			TryUpdateProject(projectById);
		}
	}

	internal static async Task SwitchViewModel(ProjectModel project, string viewMode, bool isParallel = false)
	{
		if (isParallel)
		{
			project.ParallelViewMode = viewMode;
			await TryUpdateProject(project);
			return;
		}
		if (project.sync_status != Constants.SyncStatus.SYNC_NEW.ToString())
		{
			project.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
		}
		project.viewMode = viewMode;
		if (!(viewMode == "kanban"))
		{
			if (viewMode == "timeline")
			{
				TimelineModel.CheckTimelineEmpty(project, Constants.SortType.sortOrder);
			}
		}
		else
		{
			SortOption sortOption = project.SortOption;
			if (sortOption != null && sortOption.groupByType == Constants.SortType.none)
			{
				project.SortOption.groupBy = "sortOrder";
				project.sortType = "sortOrder";
			}
		}
		await TryUpdateProject(project);
		SyncManager.Sync();
	}

	public static ProjectModel GetAssignProjectInGroup(string groupId, string assignee)
	{
		List<ProjectModel> list = (from p in CacheManager.GetProjectsInGroup(groupId)
			where p.IsEnable()
			select p).ToList();
		list.Sort((ProjectModel a, ProjectModel b) => a.sortOrder.CompareTo(b.sortOrder));
		foreach (ProjectModel item in list)
		{
			if (AvatarHelper.GetUserByIdAndProjectId(assignee, item.id) != null)
			{
				return item;
			}
		}
		return null;
	}

	public static async Task SaveProjectName(string projectId, string name)
	{
		ProjectModel projectById = CacheManager.GetProjectById(projectId);
		if (projectById != null)
		{
			projectById.name = name;
			projectById.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			await TryUpdateProject(projectById);
			SyncManager.Sync();
		}
	}

	internal static async Task InitProjects(List<string> projects)
	{
		if (projects == null || projects.Count == 0)
		{
			return;
		}
		long order = CacheManager.GetProjects().Max((ProjectModel p) => p.sortOrder);
		foreach (string project in projects)
		{
			order += 268435456;
			await CreateProject(new ProjectModel
			{
				id = Utils.GetGuid(),
				name = (NewUserGuide.DefaultProjects.TryGetValue(project, out var value) ? value : "") + project,
				sortType = Constants.SortType.sortOrder.ToString(),
				SortOption = new SortOption
				{
					groupBy = Constants.SortType.sortOrder.ToString(),
					orderBy = Constants.SortType.sortOrder.ToString()
				},
				viewMode = "list",
				userid = LocalSettings.Settings.LoginUserId,
				sync_status = Constants.SyncStatus.SYNC_NEW.ToString(),
				kind = Constants.ProjectKind.TASK.ToString(),
				sortOrder = order,
				inAll = true
			}, notify: false);
			DataChangedNotifier.NotifyProjectChanged();
			ListViewContainer.ReloadProjectData();
			SyncManager.TryDelaySync();
		}
	}

	public static async Task<string> PullGuideProject(string abTest)
	{
		GuideProjectModel gProject = await Communicator.GetGuideProject("newuser3_202403" + abTest);
		if (gProject != null)
		{
			ProjectModel project = gProject.ToProject();
			List<ColumnModel> models = gProject.ToColumns();
			List<TaskModel> tasks = gProject.ToTasks();
			await BaseDao<ColumnModel>.InsertAllAsync(models);
			await CreateProject(project);
			await TaskDao.BatchInsertTasksAndItems(tasks);
			return gProject.id;
		}
		return null;
	}

	public static async Task SetProjectNeedPullRemote(string projectId, bool needPullRemote)
	{
		string query = $"Update ProjectModel Set needPullRemote = {needPullRemote} where id = '{projectId}' and userId = '{LocalSettings.Settings.LoginUserId}'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task<bool> CheckIfNewShowReminder()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from p in App.Connection.Table<ProjectModel>()
			where p.userid == userId && (p.showType != (int?)null || p.reminderType != (int?)null)
			select p).CountAsync() > 0;
	}
}
