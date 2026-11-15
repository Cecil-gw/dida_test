using ticktick_WPF.Models;
using ticktick_WPF.Views.Completed;

namespace ticktick_WPF.Util.Provider;

public class CompletedProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_completed";

	public override string LoadMoreId => "_special_id_completed";

	public static ClosedFilterViewModel Filter { get; set; } = new ClosedFilterViewModel();

	public override string SortProjectId => null;

	public CompletedProjectIdentity()
	{
		base.SortOption = new SortOption();
		base.CanDrag = false;
	}

	public override bool CanAddTask()
	{
		return false;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new CompletedProjectIdentity();
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Completed");
	}
}
