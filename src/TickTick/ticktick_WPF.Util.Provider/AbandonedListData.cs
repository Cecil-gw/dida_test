using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class AbandonedListData : SortProjectData
{
	public AbandonedListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskAbandoned");
		base.EmptyContent = Utils.GetString("NoCompletedTaskDesc");
		base.AddTaskHint = string.Empty;
		base.IsCompleted = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("Abandoned");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("AbandonedEmpty");
	}
}
