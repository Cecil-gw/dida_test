using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Provider;

public class AllProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_all";

	public override string CatId => "all";

	public AllProjectIdentity()
	{
		base.SortOption = SmartProjectService.GetSmartProjectSortOption("all", inKanban: false);
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("All");
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return ProjectIdentity.CreateSmartIdentity(project.Id);
	}
}
