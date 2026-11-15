using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AllSmartProjectViewModel : SmartProjectViewModel
{
	public AllSmartProjectViewModel()
	{
		base.Title = Utils.GetString("All");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.All");
		base.Id = "_special_id_all";
	}
}
