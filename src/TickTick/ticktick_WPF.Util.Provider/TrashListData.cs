using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Util.Provider;

public class TrashListData : SortProjectData
{
	public TrashListData()
	{
		base.EmptyTitle = Utils.GetString("NoTaskInTrash");
		base.EmptyContent = Utils.GetString("NoTrashDesc");
		base.AddTaskHint = string.Empty;
		base.IsCompleted = false;
		base.IsTrash = true;
	}

	public override string GetTitle()
	{
		return Utils.GetString("Trash");
	}

	public override EmptyIconsSourceModel GetEmptyIconsSource(bool allCompleted)
	{
		return new EmptyIconsSourceModel("TrashEmpty");
	}
}
