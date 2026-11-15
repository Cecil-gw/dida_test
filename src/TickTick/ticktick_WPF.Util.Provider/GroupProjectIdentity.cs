using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.TaskList;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Util.Provider;

public sealed class GroupProjectIdentity : ProjectIdentity
{
	public int DisplayKind;

	public bool ContainsShare;

	public override string ViewMode => Group?.viewMode ?? "list";

	public override string ParallelViewMode => Group?.ParallelViewMode ?? "list";

	public override string Id => Group?.id ?? "_special_id_group_all";

	public override string SortProjectId => Group?.id;

	public override string LoadMoreId => Group?.id;

	public override string QueryId => Group?.id ?? "GroupAll";

	public override string CatId => Group?.id;

	public override string Title => Group?.name;

	public ProjectGroupModel Group { get; private set; }

	public ProjectModel DefaultProject { get; }

	public string GroupId => Group?.id;

	public GroupProjectIdentity(ProjectGroupModel group)
	{
		Group = group;
		base.SortOption = group.GetSortOption();
	}

	public GroupProjectIdentity(ProjectGroupModel group, List<ProjectModel> projects)
	{
		Group = group;
		DefaultProject = (from p in projects?.Where((ProjectModel p) => p.IsProjectPermit())
			orderby p.sortOrder
			select p).FirstOrDefault();
		DisplayKind = GetDisplayKind(projects);
		IsNote = DefaultProject?.IsNote ?? false;
		ContainsShare = projects?.Any((ProjectModel p) => p.IsShareList()) ?? false;
		base.SortOption = group.GetSortOption();
	}

	public override bool GetForceShowProjectName()
	{
		return true;
	}

	public void SetGroup(ProjectGroupModel group)
	{
		if (group != null)
		{
			Group = group;
			base.SortOption = group.GetSortOption();
		}
	}

	private int GetDisplayKind(List<ProjectModel> projects)
	{
		TaskType result = TaskType.Task;
		if (projects == null)
		{
			return (int)result;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (ProjectModel project in projects)
		{
			if (!(flag2 && flag))
			{
				if (project.IsNote)
				{
					flag2 = true;
				}
				else if (!project.IsNote)
				{
					flag = true;
				}
			}
		}
		return flag2 ? ((!flag) ? 1 : 2) : 0;
	}

	public override string GetProjectId()
	{
		return DefaultProject?.id;
	}

	public override string GetProjectName()
	{
		return DefaultProject?.name;
	}

	public override string GetDisplayTitle()
	{
		return Group?.name;
	}

	public override Geometry GetProjectIcon()
	{
		return Utils.GetGeometry("Ic.ProjectList.Folder");
	}

	public override bool CanAddTask()
	{
		return DefaultProject != null;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		ProjectGroupModel groupById = CacheManager.GetGroupById(project.CatId);
		if (groupById != null)
		{
			return new GroupProjectIdentity(groupById, CacheManager.GetProjectsInGroup(groupById.id));
		}
		return (GroupProjectIdentity)MemberwiseClone();
	}

	public override TimelineModel GetTimelineModel()
	{
		if (Group == null)
		{
			return null;
		}
		TimelineModel timeline = Group.Timeline;
		if (timeline != null)
		{
			return timeline.Copy();
		}
		Group.Timeline = new TimelineModel(Constants.SortType.project.ToString());
		CommitTimeline(Group.Timeline);
		return Group.Timeline.Copy();
	}

	public override void CommitTimeline(TimelineModel model)
	{
		if (model == null)
		{
			return;
		}
		ProjectGroupModel groupById = CacheManager.GetGroupById(Group?.id);
		if (groupById != null && (groupById.Timeline == null || !groupById.Timeline.IsEquals(model)))
		{
			bool needSync = groupById.Timeline == null || groupById.Timeline.SyncPropertyChanged(model);
			if (model.SortType == "assignee" || model.SortType == "priority")
			{
				model.SortType = groupById.Timeline?.SortType ?? "project";
			}
			groupById.Timeline = model;
			if (groupById.SyncTimeline != null)
			{
				groupById.SyncTimeline.SortType = model.SortType;
				groupById.SyncTimeline.SortOption = model.sortOption;
			}
			else
			{
				groupById.SyncTimeline = new TimelineSyncModel
				{
					SortType = model.SortType,
					SortOption = model.sortOption
				};
			}
			ProjectGroupDao.SaveProjectGroup(groupById, needSync);
		}
		if (Group != null)
		{
			Group.Timeline = model;
		}
	}

	public override List<SortTypeViewModel> GetTimelineSortTypes()
	{
		return SortOptionHelper.GetGroupTimelineSortTypeModels(ContainsShare);
	}

	public override async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
		ProjectGroupModel group = CacheManager.GetGroupById(Group?.id);
		if (group == null)
		{
			return;
		}
		if (isParallel)
		{
			group.ParallelViewMode = viewMode;
			await ProjectGroupDao.SaveProjectGroup(group, needSync: false);
		}
		else
		{
			group.viewMode = viewMode;
			if (!(viewMode == "kanban"))
			{
				if (viewMode == "timeline")
				{
					TimelineModel.CheckTimelineEmpty(group, Constants.SortType.project);
				}
			}
			else
			{
				SortOption sortOption = group.SortOption;
				if (sortOption != null && sortOption.groupByType == Constants.SortType.none)
				{
					group.SortOption.groupBy = "project";
					group.sortType = "project";
				}
			}
			await ProjectGroupDao.SaveProjectGroup(group);
			SyncManager.TryDelaySync();
		}
		List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(group.id);
		DataChangedNotifier.NotifyProjectViewModeChanged(new GroupProjectIdentity(group, projectsInGroup));
	}

	public override List<string> GetSwitchViewModes()
	{
		return new List<string> { "list", "kanban", "timeline" };
	}

	public void CheckSortOption()
	{
		SortOption sortOption = Group?.GetSortOption();
		if (sortOption == null)
		{
			return;
		}
		bool flag = false;
		if (DisplayKind == 1)
		{
			if (sortOption.groupByType == Constants.SortType.priority || sortOption.groupByType == Constants.SortType.dueDate)
			{
				sortOption.groupBy = "project";
				flag = true;
			}
			if (sortOption.orderByType == Constants.SortType.priority || sortOption.orderByType == Constants.SortType.dueDate)
			{
				sortOption.orderBy = "createdTime";
				flag = true;
			}
		}
		if (flag)
		{
			base.SortOption = sortOption;
			ProjectGroupModel groupById = CacheManager.GetGroupById(Group?.id);
			if (groupById != null)
			{
				groupById.SortOption = sortOption;
				Group = groupById;
				ProjectGroupDao.TrySaveProjectGroup(groupById);
				SyncManager.TryDelaySync();
			}
		}
	}

	public override string CheckTitleValid(string text)
	{
		if (string.IsNullOrEmpty(text.Trim()))
		{
			return Utils.GetString("AddOrEditFolderNameCantNull");
		}
		return null;
	}

	public override async void SaveTitle(string text)
	{
		Group.name = text.Trim();
		await ProjectGroupDao.TrySaveProjectGroup(Group);
		SyncManager.Sync();
		ProjectWidgetsHelper.OnProjectChanged(new GroupProjectIdentity(Group, null));
		ListViewContainer.ReloadProjectData(refreshCount: false);
	}
}
