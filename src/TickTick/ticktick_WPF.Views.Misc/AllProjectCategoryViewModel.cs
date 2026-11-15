using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AllProjectCategoryViewModel : SelectableItemViewModel
{
	public AllProjectCategoryViewModel()
	{
		base.Title = Utils.GetString("AllList");
		base.Icon = Utils.GetGeometry("Ic.ProjectList.All");
		base.CanMultiSelect = false;
		base.Id = "ProjectAll2e4c103c57ef480997943206";
	}
}
