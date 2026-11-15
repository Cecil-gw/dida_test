using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Provider;

public class TrashProjectIdentity : SmartProjectIdentity
{
	public override bool CanEdit { get; }

	public override string Id => "_special_id_trash";

	public override string SortProjectId => null;

	public bool IsPerson { get; set; } = true;

	public TrashProjectIdentity()
	{
		base.SortOption = new SortOption();
	}

	public override bool CanAddTask()
	{
		return false;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new TrashProjectIdentity();
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Trash");
	}
}
