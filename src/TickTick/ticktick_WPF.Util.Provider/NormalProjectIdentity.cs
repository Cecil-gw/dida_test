using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util.Provider;

public class NormalProjectIdentity : ProjectIdentity
{
	public override string ViewMode => Project?.viewMode ?? "list";

	public override string ParallelViewMode => Project?.ParallelViewMode ?? "list";

	public override string LoadMoreId => Project?.id;

	public override string QueryId => Project?.id;

	public override string CatId => Project?.id;

	public override string Title => Project?.name;

	public override bool CanEdit => Project?.IsEnable() ?? false;

	public ProjectModel Project { get; private set; }

	public override string SortProjectId => Project?.id;

	public NormalProjectIdentity(ProjectModel project)
	{
		Id = project?.id;
		Project = project;
		if (project != null)
		{
			base.SortOption = ((!project.Isinbox) ? project.GetSortOption() : SmartProjectService.GetSmartProjectSortOption("inbox", inKanban: false));
			if (project.Isinbox)
			{
				project.viewMode = SmartProjectService.GetSmartProjectViewMode(project.id);
				project.ParallelViewMode = SmartProjectService.GetSmartProjectViewMode(project.id, inParallel: true);
			}
			if (project.IsNote && project.viewMode == "timeline")
			{
				project.viewMode = "list";
			}
		}
		else
		{
			base.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.sortOrder, inKanban: false);
		}
		IsNote = project?.IsNote ?? false;
	}

	public override string GetProjectId()
	{
		return Id;
	}

	public override string GetProjectName()
	{
		return Project?.name;
	}

	public override string GetDisplayTitle()
	{
		return Project?.name;
	}

	public override Geometry GetProjectIcon()
	{
		return GetProjectIcon(Project);
	}

	public static Geometry GetProjectIcon(ProjectModel project)
	{
		if (project != null)
		{
			if (project.Isinbox)
			{
				return Utils.GetGeometry("Ic.ProjectList.Inbox");
			}
			if (!project.IsNote)
			{
				return Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.SharedList" : "Ic.ProjectList.List");
			}
			return Utils.GetGeometry(project.IsShareList() ? "Ic.ProjectList.NoteMember" : "Ic.ProjectList.Note");
		}
		return Utils.GetGeometry("Ic.ProjectList.List");
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new NormalProjectIdentity(CacheManager.GetProjectById(project.Id) ?? ((NormalProjectIdentity)project).Project);
	}

	public override bool CanAddTask()
	{
		if (Project != null)
		{
			return Project.IsEnable();
		}
		return false;
	}

	public override TimelineModel GetTimelineModel()
	{
		if (Project == null)
		{
			return null;
		}
		if (Project.Isinbox)
		{
			return SmartProjectService.GetSmartProjectTimeline("inbox");
		}
		TimelineModel timeline = Project.Timeline;
		if (timeline != null)
		{
			return timeline.Copy();
		}
		Project.Timeline = new TimelineModel();
		CommitTimeline(Project.Timeline);
		return Project.Timeline.Copy();
	}

	public override void CommitTimeline(TimelineModel model)
	{
		if (model == null)
		{
			return;
		}
		if (Project.Isinbox)
		{
			SmartProjectService.SaveSmartProjectTimeline("inbox", model);
			DataChangedNotifier.NotifySortOptionChanged(Project.id);
			return;
		}
		ProjectModel projectById = CacheManager.GetProjectById(Id);
		if (projectById != null && (projectById.Timeline == null || !projectById.Timeline.IsEquals(model)))
		{
			if (projectById.Timeline == null || projectById.Timeline.SyncPropertyChanged(model))
			{
				projectById.sync_status = Constants.SyncStatus.SYNC_UPDATE.ToString();
			}
			if (model.SortType == "priority")
			{
				model.SortType = projectById.Timeline?.SortType ?? "sortOrder";
			}
			if (projectById.SyncTimeline != null)
			{
				projectById.SyncTimeline.SortType = model.SortType;
				projectById.SyncTimeline.SortOption = model.sortOption;
			}
			else
			{
				projectById.SyncTimeline = new TimelineSyncModel
				{
					SortType = model.SortType,
					SortOption = model.sortOption
				};
			}
			projectById.Timeline = model;
			Project = projectById;
			ProjectDao.TryUpdateProject(projectById);
		}
	}

	public override List<SortTypeViewModel> GetTimelineSortTypes()
	{
		return SortOptionHelper.GetNormalProjectSortTypeModels(Project.IsShareList(), Project.IsNote, isTimeline: true);
	}

	public override async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
		if (Project.Isinbox)
		{
			SmartProjectService.SaveSmartProjectViewMode(Id, viewMode, isParallel);
			DataChangedNotifier.NotifyProjectViewModeChanged(this);
		}
		ProjectModel project = CacheManager.GetProjectById(Id);
		if (project != null)
		{
			await ProjectDao.SwitchViewModel(project, viewMode, isParallel);
			if (viewMode == "kanban" && project.GetSortOption().groupByType == Constants.SortType.sortOrder)
			{
				await ColumnDao.CheckProjectColumns(Id);
			}
			DataChangedNotifier.NotifyProjectViewModeChanged(new NormalProjectIdentity(project));
			if (!isParallel)
			{
				SyncManager.TryDelaySync();
			}
		}
	}

	public override List<string> GetSwitchViewModes()
	{
		if (!Project.IsValid())
		{
			return null;
		}
		if (Project.IsNote)
		{
			return new List<string> { "list", "kanban" };
		}
		return new List<string> { "list", "kanban", "timeline" };
	}

	public override string CheckTitleValid(string text)
	{
		if (string.IsNullOrEmpty(text.Trim()))
		{
			return Utils.GetString("AddOrEditProjectNameCantNull");
		}
		if (text[0] == '#' || text[0] == '＃')
		{
			return Utils.GetString("ListNameBeginError");
		}
		if (!NameUtils.IsValidNameNoCheckSharp(text, checkSpace: false))
		{
			return Utils.GetString("ListNameCantContain");
		}
		if (Project != null)
		{
			ProjectModel proj = Project;
			if (CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.name.Trim() == text.Trim() && p.id != proj.id && p.teamId == proj.teamId && p.IsBelongGroup(proj.groupId) && !p.Isinbox) != null)
			{
				return Utils.GetString("DuplicateListName");
			}
		}
		return null;
	}

	public override async void SaveTitle(string text)
	{
		await ProjectDao.SaveProjectName(Project.id, text);
		ListViewContainer.ReloadProjectData(refreshCount: false);
	}
}
