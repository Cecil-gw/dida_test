using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public class ProjectGroupDao : BaseDao<ProjectGroupModel>
{
	public static async Task TrySaveProjectGroup(ProjectGroupModel projectGroup, bool isNeedUpdateOpen = false)
	{
		if (projectGroup.id.Contains("_special_id_closed"))
		{
			return;
		}
		int userId = Utils.GetCurrentUserIdInt();
		List<ProjectGroupModel> query = await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId && v.id.Equals(projectGroup.id)
			select v).ToListAsync();
		if (query.Count == 0)
		{
			query = await (from v in App.Connection.Table<ProjectGroupModel>()
				where v._Id.Equals(projectGroup._Id)
				select v).ToListAsync();
		}
		if (query.Count != 0)
		{
			await ProjectGroupSyncedJsonDao.TrySaveProjectGroup(query[0].id);
			projectGroup._Id = query[0]._Id;
			projectGroup.etag = query[0].etag;
			if (!isNeedUpdateOpen)
			{
				projectGroup.open = query[0].open;
			}
			projectGroup.name = projectGroup.name.Trim();
			projectGroup.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			await App.Connection.UpdateAsync(projectGroup);
			CacheManager.UpdateProjectGroup(projectGroup);
		}
		else
		{
			projectGroup.name = projectGroup.name.Trim();
			projectGroup.userId = userId;
			if (!projectGroup.sortOrder.HasValue)
			{
				projectGroup.sortOrder = ProjectDao.GetNewProjectOrder(projectGroup.teamId);
			}
			await App.Connection.InsertAsync(projectGroup);
			CacheManager.UpdateProjectGroup(projectGroup);
		}
	}

	public static async Task SaveProjectGroup(ProjectGroupModel projectGroup, bool needSync = true)
	{
		if (projectGroup != null)
		{
			if (needSync && projectGroup.sync_status != Constants.SyncStatus.SYNC_NEW.ToString())
			{
				projectGroup.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			projectGroup.name = projectGroup.name.Trim();
			await App.Connection.UpdateAsync(projectGroup);
			CacheManager.UpdateProjectGroup(projectGroup);
		}
	}

	public static async Task<bool> CheckUnSyncItem()
	{
		int userId = Utils.GetCurrentUserIdInt();
		string syncDone = Constants.SyncStatus.SYNC_DONE.ToString();
		if ((await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId && v.sync_status != null && v.sync_status != "" && v.sync_status != syncDone
			select v).ToListAsync()).Count != 0)
		{
			return false;
		}
		return true;
	}

	public static async Task<List<ProjectGroupModel>> GetProjectGroups()
	{
		int userId = Utils.GetCurrentUserIdInt();
		return (await (from p in App.Connection.Table<ProjectGroupModel>()
			where p.userId == userId && p.deleted != 1
			select p).ToListAsync()) ?? new List<ProjectGroupModel>();
	}

	public static async Task<ProjectGroupModel> GetProjectGroupById(string id)
	{
		int userId = Utils.GetCurrentUserIdInt();
		List<ProjectGroupModel> list = await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId && v.id == id
			select v).ToListAsync();
		if (list.Count != 0)
		{
			return list[0];
		}
		return null;
	}

	public static async Task<int> DeleteProjectGroupById(string id)
	{
		int userId = Utils.GetCurrentUserIdInt();
		List<ProjectGroupModel> list = await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId && v.id == id
			select v).ToListAsync();
		if (list.Count != 0)
		{
			list[0].deleted = 1;
			CacheManager.DeleteProjectGroup(id);
			return await App.Connection.UpdateAsync(list[0]);
		}
		return -1;
	}

	public static async Task<Dictionary<string, ProjectGroupModel>> GetLocalSyncedProjectGroupMap()
	{
		int userId = Utils.GetCurrentUserIdInt();
		Dictionary<string, ProjectGroupModel> groupDict = new Dictionary<string, ProjectGroupModel>();
		List<ProjectGroupModel> list = await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId
			select v).ToListAsync();
		if (list.Count > 0)
		{
			foreach (ProjectGroupModel item in list)
			{
				if (!groupDict.ContainsKey(item.id))
				{
					groupDict.Add(item.id, item);
				}
			}
		}
		return groupDict;
	}

	private static async Task<List<string>> GetProjectGroupIds()
	{
		string text = Utils.GetCurrentUserIdInt().ToString();
		return await BaseDao<ProjectGroupModel>.GetEntityIds("select id as Id from ProjectGroupModel where userId = '" + text + "'");
	}

	public static async Task SaveServerMergeData(List<ProjectGroupModel> added, List<ProjectGroupModel> updated, List<ProjectGroupModel> deleted)
	{
		if (added.Count > 0)
		{
			List<string> list = await GetProjectGroupIds();
			List<ProjectGroupModel> addlist = new List<ProjectGroupModel>();
			foreach (ProjectGroupModel item in added)
			{
				if (!list.Contains(item.id))
				{
					item.name = item.name.Trim();
					addlist.Add(item);
				}
			}
			if (addlist.Count > 0)
			{
				await App.Connection.InsertAllAsync(addlist);
				foreach (ProjectGroupModel item2 in addlist)
				{
					CacheManager.UpdateProjectGroup(item2);
				}
			}
		}
		if (updated.Count > 0)
		{
			updated.ForEach(delegate(ProjectGroupModel update)
			{
				update.name = update.name.Trim();
			});
			await ProjectGroupSyncedJsonDao.BatchDeleteGroups(updated.Select((ProjectGroupModel group) => group.id).ToList());
			await App.Connection.UpdateAllAsync(updated);
			foreach (ProjectGroupModel item3 in updated)
			{
				CacheManager.UpdateProjectGroup(item3);
			}
		}
		if (deleted.Count <= 0)
		{
			return;
		}
		foreach (ProjectGroupModel item4 in deleted)
		{
			await App.Connection.DeleteAsync(item4);
		}
		foreach (ProjectGroupModel item5 in deleted)
		{
			CacheManager.DeleteProjectGroup(item5);
		}
	}

	public static async Task<List<ProjectGroupModel>> GetNeedPostProjectGroup()
	{
		int userId = Utils.GetCurrentUserIdInt();
		string syncDone = Constants.SyncStatus.SYNC_DONE.ToString();
		return await (from v in App.Connection.Table<ProjectGroupModel>()
			where v.userId == userId && v.sync_status != syncDone
			select v).ToListAsync();
	}

	public static async Task CheckGroupGroupBy(string groupId)
	{
		ProjectGroupModel groupById = CacheManager.GetGroupById(groupId);
		if (groupById == null)
		{
			return;
		}
		SortOption sortOption = groupById.SortOption;
		if (sortOption == null || sortOption.groupByType != Constants.SortType.assignee)
		{
			TimelineModel timeline = groupById.Timeline;
			if (timeline == null || timeline.sortOption?.groupByType != Constants.SortType.assignee)
			{
				return;
			}
		}
		if (!CacheManager.GetProjectsInGroup(groupId).Any((ProjectModel p) => p.IsShareList()))
		{
			SortOption sortOption2 = groupById.SortOption;
			if (sortOption2 != null && sortOption2.groupByType == Constants.SortType.assignee)
			{
				groupById.SortOption.groupBy = "project";
			}
			TimelineModel timeline2 = groupById.Timeline;
			if (timeline2 != null && timeline2.sortOption?.groupByType == Constants.SortType.assignee)
			{
				groupById.Timeline.sortOption.groupBy = "project";
			}
			await TrySaveProjectGroup(groupById);
		}
	}
}
