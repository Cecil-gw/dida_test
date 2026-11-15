using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class ListGroupViewModel : SelectableItemViewModel
{
	public ListGroupViewModel()
	{
		base.Title = Utils.GetString("AllList");
		base.Id = "#alllists";
		base.Icon = Utils.GetGeometry("Ic.ProjectList.All");
		base.ShowIndent = true;
		base.Selectable = true;
		base.IsSectionGroup = true;
		base.Open = true;
		base.IsParent = true;
	}
}
