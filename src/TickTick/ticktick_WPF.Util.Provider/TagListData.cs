using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class TagListData : SortProjectData
{
	private readonly string _tag;

	private readonly string _title;

	public TagListData(string tag)
	{
		_tag = tag;
		TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel t) => t.name == tag);
		_title = tagModel?.GetDisplayName() ?? tag;
		base.AddTaskHint = tagModel?.GetDisplayName() ?? (tag + " ");
		base.EmptyTitle = Utils.GetString("NoTasks");
		base.EmptyContent = Utils.GetString("TaskItEasy");
		base.ShowAssignSort = false;
		base.ShowCustomSort = false;
		base.ShowLoadMore = false;
		base.ShowProjectSort = true;
	}

	public override string GetTitle()
	{
		return _title;
	}

	public override async void SaveSortOption(SortOption sortOption)
	{
		TagModel tagModel = await TagDao.GetTagByName(_tag);
		if (tagModel != null)
		{
			tagModel.SortOption = sortOption;
			tagModel.sortType = ((sortOption.groupBy == "none") ? sortOption.orderBy : sortOption.groupBy);
			tagModel.status = 1;
			await TagDao.UpdateTag(tagModel);
		}
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("TagEmpty");
	}
}
