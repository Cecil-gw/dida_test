namespace ticktick_WPF.Util.Provider;

public sealed class DeletedProjectIdentity : ProjectIdentity
{
	public override string GetDisplayTitle()
	{
		return Utils.GetString("TheListDeleted");
	}
}
