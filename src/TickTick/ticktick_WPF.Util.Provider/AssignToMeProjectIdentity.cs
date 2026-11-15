using System.Collections.Generic;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Provider;

public class AssignToMeProjectIdentity : SmartProjectIdentity
{
	public override string Id => "_special_id_assigned";

	public override string CatId => "assigned";

	public override string ViewMode => "list";

	public override string SortProjectId => "assignee";

	public AssignToMeProjectIdentity()
	{
		base.SortOption = SmartProjectService.GetSmartProjectSortOption("assignToMe", inKanban: false);
	}

	public override bool CanAddTask()
	{
		return false;
	}

	public override string GetDisplayTitle()
	{
		return Utils.GetString("AssignToMe");
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		return ProjectIdentity.CreateSmartIdentity(project.Id);
	}

	public override List<string> GetSwitchViewModes()
	{
		return null;
	}
}
