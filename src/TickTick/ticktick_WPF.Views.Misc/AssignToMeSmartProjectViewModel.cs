using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AssignToMeSmartProjectViewModel : SmartProjectViewModel
{
	public AssignToMeSmartProjectViewModel()
	{
		base.Title = Utils.GetString("AssignToMe");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.AssignToMe");
		base.Id = "_special_id_assigned";
	}
}
