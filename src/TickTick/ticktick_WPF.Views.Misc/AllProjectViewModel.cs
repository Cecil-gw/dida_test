using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AllProjectViewModel : SelectableItemViewModel
{
	public AllProjectViewModel()
	{
		base.Title = Utils.GetString("All");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.All");
		base.CanMultiSelect = false;
		base.Id = "all";
	}
}
