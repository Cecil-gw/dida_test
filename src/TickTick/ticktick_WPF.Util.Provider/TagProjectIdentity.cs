using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Views.Tag;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util.Provider;

public sealed class TagProjectIdentity : ProjectIdentity
{
	public override string ViewMode => TagModel?.viewMode ?? "list";

	public override string ParallelViewMode => TagModel?.ParallelViewMode ?? "list";

	public string Tag { get; }

	public TagModel TagModel { get; private set; }

	public override string CatId => "#" + Tag;

	public override string SortProjectId => "#" + Tag;

	public override string Id => Tag;

	public override string QueryId => Tag;

	public override bool LoadAll => true;

	public override string Title => Tag;

	public TagProjectIdentity(TagModel tag)
	{
		TagModel = tag;
		Tag = tag.name;
		base.SortOption = tag.GetSortOption();
	}

	public override string GetDisplayTitle()
	{
		return TagDataHelper.GetTagDisplayName(Tag);
	}

	public override List<string> GetTags()
	{
		return new List<string> { Tag };
	}

	public override bool UseDefaultTags()
	{
		return false;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		TagModel tagModel = ((TagProjectIdentity)project).TagModel;
		return new TagProjectIdentity(CacheManager.GetTagByName(tagModel.name) ?? tagModel);
	}

	public override TimelineModel GetTimelineModel()
	{
		if (TagModel == null)
		{
			return null;
		}
		TimelineModel timeline = TagModel.Timeline;
		if (timeline != null)
		{
			return timeline.Copy();
		}
		TagModel.Timeline = new TimelineModel("project");
		CommitTimeline(TagModel.Timeline);
		return TagModel.Timeline.Copy();
	}

	public override void CommitTimeline(TimelineModel model)
	{
		if (model == null)
		{
			return;
		}
		TagModel tagByName = CacheManager.GetTagByName(Tag);
		if (tagByName != null && (tagByName.Timeline == null || !tagByName.Timeline.IsEquals(model)))
		{
			if ((tagByName.Timeline == null || tagByName.Timeline.SyncPropertyChanged(model)) && tagByName.status != 0)
			{
				tagByName.status = 1;
			}
			if (model.SortType == "priority")
			{
				model.SortType = tagByName.Timeline?.SortType ?? "project";
			}
			if (tagByName.SyncTimeline != null)
			{
				tagByName.SyncTimeline.SortType = model.SortType;
				tagByName.SyncTimeline.SortOption = model.sortOption;
			}
			else
			{
				tagByName.SyncTimeline = new TimelineSyncModel
				{
					SortType = model.SortType,
					SortOption = model.sortOption
				};
			}
			tagByName.Timeline = model;
			TagModel = tagByName;
			TagDao.UpdateTag(tagByName);
		}
	}

	public override List<SortTypeViewModel> GetTimelineSortTypes()
	{
		List<SortTypeViewModel> smartProjectSortTypeModels = SortOptionHelper.GetSmartProjectSortTypeModels(isNote: false, inTimeline: true);
		if (!TagModel.IsParent())
		{
			smartProjectSortTypeModels.RemoveAll((SortTypeViewModel s) => s.Id == "tag");
		}
		return smartProjectSortTypeModels;
	}

	public override async Task SwitchViewMode(string viewMode, bool isParallel = false)
	{
		TagModel tag = CacheManager.GetTagByName(Tag);
		if (tag != null)
		{
			await TagDao.SwitchViewModel(tag, viewMode, isParallel);
			DataChangedNotifier.NotifyProjectViewModeChanged(new TagProjectIdentity(tag));
			if (!isParallel)
			{
				SyncManager.TryDelaySync();
			}
		}
	}

	public override List<string> GetSwitchViewModes()
	{
		return new List<string> { "list", "kanban", "timeline" };
	}

	public override Geometry GetProjectIcon()
	{
		return SpecialListUtils.GetIconBySmartType(SmartListType.Tag);
	}
}
