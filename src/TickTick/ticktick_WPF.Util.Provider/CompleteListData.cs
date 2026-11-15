using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class CompleteListData : SortProjectData
{
	public CompleteListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskCompleted");
		base.EmptyContent = Utils.GetString("NoCompletedTaskDesc");
		base.AddTaskHint = string.Empty;
		base.IsCompleted = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("Completed");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("CompletedEmpty");
	}
}
