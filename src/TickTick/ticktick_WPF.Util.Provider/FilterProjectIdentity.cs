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
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util.Provider;

public sealed class FilterProjectIdentity : ProjectIdentity
{
	public FilterModel Filter { get; }

	public string FilterId => Filter?.id;

	public override string CatId => Filter?.id;

	public override string Id => Filter?.id;

	public override string SortProjectId => Filter?.id;

	public override string QueryId => Filter?.id;

	public override bool LoadAll => false;

	public override bool IsNote => TaskDefault?.IsNote ?? false;

	public bool OnlyNote => TaskDefault?.OnlyNote ?? false;

	private FilterTaskDefault TaskDefault { get; }

	public override string Title => Filter?.name;

	public override string ViewMode => Filter?.viewMode ?? "list";

	public override string ParallelViewMode => Filter?.ParallelViewMode ?? "list";

	public FilterProjectIdentity(FilterModel filter)
	{
		Filter = filter;
		TaskDefault = FilterViewModel.CalculateTaskDefault(Filter.rule);
		base.SortOption = filter.GetSortOption();
	}

	public override string GetProjectId()
	{
		return TaskDefault.ProjectModel.id;
	}

	public override string GetProjectName()
	{
		return TaskDefault.ProjectModel.name;
	}

	public override TimeData GetTimeData()
	{
		if (!GetIsNote())
		{
			return new TimeData
			{
				StartDate = TaskDefault.DefaultDate,
				Reminders = TimeData.GetDefaultAllDayReminders(),
				IsDefault = true,
				IsAllDay = true
			};
		}
		return new TimeData();
	}

	public int? GetDefaultPriority()
	{
		return TaskDefault.Priority;
	}

	public override int GetPriority()
	{
		return TaskDefault.Priority ?? TaskDefaultDao.GetDefaultSafely().Priority;
	}

	public override string GetDisplayTitle()
	{
		return Filter?.name;
	}

	public override Geometry GetProjectIcon()
	{
		return SpecialListUtils.GetIconBySmartType(SmartListType.Filter);
	}

	public override List<string> GetTags()
	{
		return TaskDefault.DefaultTags;
	}

	public override string GetAssignee()
	{
		return TaskDefault.DefaultAssignee;
	}

	public override bool UseDefaultTags()
	{
		return false;
	}

	public override bool GetForceShowProjectName()
	{
		return TaskDefault.HasProjectSelected;
	}

	public override bool GetIsNote()
	{
		return TaskDefault.IsNote;
	}

	public bool FilterNote()
	{
		return TaskDefault.OnlyNote;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new FilterProjectIdentity(CacheManager.GetFilterById(project.CatId) ?? ((FilterProjectIdentity)project).Filter);
	}

	public override TimelineModel GetTimelineModel()
	{
		if (Filter == null)
		{
			return null;
		}
		TimelineModel timeline = Filter.Timeline;
		if (timeline != null)
		{
			return timeline.Copy();
		}
		Filter.Timeline = new TimelineModel(Constants.SortType.project.ToString());
		CommitTimeline(Filter.Timeline);
		return Filter.Timeline.Copy();
	}

	public override void CommitTimeline(TimelineModel model)
	{
		if (model == null)
		{
			return;
		}
		FilterModel filterById = CacheManager.GetFilterById(Id);
		if (filterById != null && (filterById.Timeline == null || !filterById.Timeline.IsEquals(model)))
		{
			if (model.SortType == "priority")
			{
				model.SortType = filterById.Timeline?.SortType ?? "project";
			}
			bool needSync = filterById.Timeline == null || filterById.Timeline.SyncPropertyChanged(model);
			filterById.Timeline = model;
			Filter.Timeline = model;
			if (filterById.SyncTimeline != null)
			{
				filterById.SyncTimeline.SortType = model.SortType;
				filterById.SyncTimeline.SortOption = model.sortOption;
			}
			else
			{
				filterById.SyncTimeline = new TimelineSyncModel
				{
					SortType = model.SortType,
					SortOption = model.sortOption
				};
			}
			FilterDao.UpdateFilter(filterById, needSync);
		}
	}

	public override List<SortTypeViewModel> GetTimelineSortTypes()
	{
		return SortOptionHelper.GetSmartProjectSortTypeModels(IsNote, inTimeline: true);
	}

	public override async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
		FilterModel filter = CacheManager.GetFilterById(Id);
		if (filter == null)
		{
			return;
		}
		if (isParallel)
		{
			filter.ParallelViewMode = viewMode;
			await FilterDao.UpdateFilter(filter, needSync: false);
		}
		else
		{
			filter.viewMode = viewMode;
			if (!(viewMode == "kanban"))
			{
				if (viewMode == "timeline")
				{
					TimelineModel.CheckTimelineEmpty(filter, Constants.SortType.project);
				}
			}
			else
			{
				SortOption sortOption = filter.SortOption;
				if (sortOption != null && sortOption.groupByType == Constants.SortType.none)
				{
					filter.SortOption.groupBy = "project";
				}
			}
			await FilterDao.UpdateFilter(filter);
			SyncManager.TryDelaySync();
		}
		DataChangedNotifier.NotifyProjectViewModeChanged(new FilterProjectIdentity(filter));
	}

	public override List<string> GetSwitchViewModes()
	{
		return new List<string> { "list", "kanban", "timeline" };
	}

	public override string CheckTitleValid(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return Utils.GetString("AddOrEditProjectNameCantNull");
		}
		if (name.StartsWith("#"))
		{
			return Utils.GetString("ListNameBeginError");
		}
		if (!NameUtils.IsValidNameNoCheckSharp(name, checkSpace: false))
		{
			return Utils.GetString("ListNameCantContain");
		}
		if (CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id != Filter.id && f.name == name) != null)
		{
			return Utils.GetString("DuplicateListName");
		}
		return null;
	}

	public override async void SaveTitle(string text)
	{
		Filter.name = text.Trim();
		await FilterSyncJsonDao.TrySaveFilter(Filter.id);
		await FilterDao.UpdateFilter(Filter);
		SyncManager.Sync();
		ListViewContainer.ReloadProjectData(refreshCount: false);
	}
}
