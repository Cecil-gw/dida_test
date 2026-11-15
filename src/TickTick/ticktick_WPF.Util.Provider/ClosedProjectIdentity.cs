namespace ticktick_WPF.Util.Provider;

public sealed class ClosedProjectIdentity : ProjectIdentity
{
	private string ProjectId { get; }

	public override string QueryId => ProjectId;

	public ClosedProjectIdentity(string projectId)
	{
		ProjectId = projectId;
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("TheListClosed");
	}
}
