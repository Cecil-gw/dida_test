namespace ticktick_WPF.Util.Provider;

public class PreviewListData : SortProjectData
{
	public PreviewListData(FilterPreviewIdentity filter)
	{
		base.ShowCustomSort = false;
		base.AddTaskHint = string.Empty;
		base.ShowLoadMore = false;
		base.EmptyTitle = Utils.GetString("SearchA");
		base.EmptyContent = Utils.GetString("SearchB");
		base.ShowShare = false;
	}

	public override string GetTitle()
	{
		return string.Empty;
	}
}
