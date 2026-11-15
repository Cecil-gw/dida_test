using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Provider;

public sealed class SearchProjectIdentity : ProjectIdentity
{
	public override string Id => "_special_id_search";

	public override bool LoadAll => true;

	public override string SortProjectId => null;

	public SearchProjectIdentity()
	{
		base.SortOption = new SortOption
		{
			groupBy = "none",
			orderBy = "none"
		};
		base.CanDrag = false;
	}
}
