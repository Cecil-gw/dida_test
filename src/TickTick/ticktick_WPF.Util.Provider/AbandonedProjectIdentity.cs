using ticktick_WPF.Models;
using ticktick_WPF.Views.Completed;

namespace ticktick_WPF.Util.Provider;

public class AbandonedProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_abandoned";

	public override string LoadMoreId => "_special_id_abandoned";

	public static ClosedFilterViewModel Filter { get; set; } = new ClosedFilterViewModel
	{
		IsCompleted = false
	};

	public override string SortProjectId => null;

	public AbandonedProjectIdentity()
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
		return new AbandonedProjectIdentity();
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Abandoned");
	}
}
