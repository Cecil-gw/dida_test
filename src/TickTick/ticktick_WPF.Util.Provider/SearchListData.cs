using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class SearchListData : SortProjectData
{
	public SearchListData()
	{
		base.ShowProjectSort = true;
		base.ShowCustomSort = false;
		base.AddTaskHint = string.Empty;
		base.ShowLoadMore = false;
		base.EmptyTitle = Utils.GetString("NothingFound");
		base.EmptyContent = Utils.GetString("ChangeKeywords");
		base.ShowShare = false;
	}

	public override string GetTitle()
	{
		return string.Empty;
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("SearchResultEmpty");
	}
}
