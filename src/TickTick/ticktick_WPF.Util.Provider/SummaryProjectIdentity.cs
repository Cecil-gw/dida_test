namespace ticktick_WPF.Util.Provider;

public class SummaryProjectIdentity : SmartProjectIdentity
{
	public override bool CanEdit { get; }

	public override string Id => "_special_id_summary";

	public override string SortProjectId => null;

	public override bool CanAddTask()
	{
		return false;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return new SummaryProjectIdentity();
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("Summary");
	}
}
