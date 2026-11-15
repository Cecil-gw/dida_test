using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class ProjectColumnViewModel : SelectableItemViewModel
{
	public ProjectColumnViewModel(ColumnModel column)
	{
		base.Title = column.name;
		base.Icon = Utils.GetGeometry("Ic.SortGroup.Group");
		base.Id = column.id;
		base.ParentId = column.projectId;
		base.IsSubItem = true;
		base.Type = "normal";
	}
}
