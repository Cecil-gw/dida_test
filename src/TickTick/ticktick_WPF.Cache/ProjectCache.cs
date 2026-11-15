using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Cache;

public class ProjectCache : CacheBase<ProjectModel>
{
	private bool _loaded;

	public override async Task Load()
	{
		if (_loaded)
		{
			return;
		}
		_loaded = true;
		List<ProjectModel> projects = (await ProjectDao.GetAllProjects()).ToList();
		ProjectModel projectModel = projects.FirstOrDefault((ProjectModel p) => p.Isinbox);
		if (projectModel != null)
		{
			projectModel.name = Utils.GetString("Inbox");
			projectModel.sortOrder = long.MinValue;
		}
		else
		{
			await Utils.CreatInbox();
			projectModel = await ProjectDao.GetProjectById(LocalSettings.Settings.InServerBoxId);
			if (projectModel != null)
			{
				projects.Add(projectModel);
			}
		}
		await CheckProjectSortOrders(projects);
		await CheckProjectTimelineModel(projects);
		AssembleData(projects, (ProjectModel project) => project.id);
		LoadProjectAvatars(projects);
	}

	private async Task CheckProjectTimelineModel(List<ProjectModel> projects)
	{
		if (projects == null || projects.Count <= 0)
		{
			return;
		}
		List<ProjectModel> list = new List<ProjectModel>();
		foreach (ProjectModel project in projects)
		{
			if (project.Timeline != null && (project.SyncTimeline == null || project.SyncTimeline.SortType != project.Timeline.SortType))
			{
				if (project.SyncTimeline == null)
				{
					project.SyncTimeline = new TimelineSyncModel();
				}
				if (project.Timeline.SortType == "none" || string.IsNullOrEmpty(project.Timeline.SortType))
				{
					project.Timeline.SortType = "sortOrder";
				}
				project.SyncTimeline.SortType = project.Timeline.SortType;
				list.Add(project);
			}
		}
		if (list.Any())
		{
			await App.Connection.UpdateAllAsync(list);
		}
	}

	private async Task CheckProjectSortOrders(List<ProjectModel> projects)
	{
		List<ProjectModel> list = projects;
		if (list == null || list.Count <= 0)
		{
			return;
		}
		projects = (from projectModel3 in projects
			orderby projectModel3.teamId, projectModel3.groupId, projectModel3.closed, projectModel3.sortOrder
			select projectModel3).ToList();
		long? num = null;
		long? num2 = null;
		List<ProjectModel> needResort = new List<ProjectModel>();
		foreach (ProjectModel project in projects)
		{
			ProjectModel p = needResort.LastOrDefault();
			if (p != null)
			{
				if (project.sortOrder == p.sortOrder && project.teamId == p.teamId && project.groupId == p.groupId)
				{
					needResort.Add(project);
					continue;
				}
				if (needResort.Count > 1)
				{
					if (!num.HasValue || project.sortOrder > num.Value)
					{
						num2 = project.sortOrder;
					}
					num = num ?? (num2.Value - 268435456);
					num2 = num2 ?? (num.Value + 268435456);
					long num3 = (num2.Value - num.Value) / (needResort.Count + 1);
					for (int num4 = 0; num4 < needResort.Count; num4++)
					{
						ProjectModel projectModel = needResort[num4];
						projectModel.sortOrder = num.Value + num3 * (num4 + 1);
						projectModel.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
					}
					await App.Connection.UpdateAllAsync(needResort);
				}
				needResort.Clear();
				needResort.Add(project);
				num = p.sortOrder;
				num2 = null;
			}
			else
			{
				needResort.Add(project);
			}
		}
		if (needResort.Count > 1)
		{
			if (!num.HasValue)
			{
				num2 = needResort[0].sortOrder - 268435456;
				num = needResort[0].sortOrder - 268435456;
			}
			long num5 = (new long?(num2 ?? (num.Value + 268435456)).Value - num.Value) / (needResort.Count + 1);
			for (int num6 = 0; num6 < needResort.Count; num6++)
			{
				ProjectModel projectModel2 = needResort[num6];
				projectModel2.sortOrder = num.Value + num5 * (num6 + 1);
				projectModel2.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			await App.Connection.UpdateAllAsync(needResort);
		}
	}

	private void LoadProjectAvatars(List<ProjectModel> projects)
	{
		Utils.RunOnBackgroundThread(Application.Current?.Dispatcher, async delegate
		{
			foreach (ProjectModel project in projects)
			{
				if (project.IsValid() && project.IsShareList())
				{
					AvatarHelper.GetProjectUsersAsync(project.id);
				}
			}
		});
	}
}
