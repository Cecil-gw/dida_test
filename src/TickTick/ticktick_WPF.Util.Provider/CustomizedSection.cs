using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class CustomizedSection : Section
{
	private bool _canSort;

	public bool CanDelete = true;

	public CustomizedSection(bool canSort = true)
	{
		_canSort = canSort;
	}

	public CustomizedSection(ColumnModel column, bool canSort = true)
	{
		base.Name = column.name;
		base.SectionEntityId = column.id;
		base.SectionId = column.id;
		base.Ordinal = column.sortOrder.GetValueOrDefault();
		base.ProjectId = column.projectId;
		base.Customized = true;
		_canSort = canSort;
	}

	public override bool CanSwitch(DisplayType displayType)
	{
		return _canSort;
	}

	public override bool CanSort(Constants.SortType sortBy)
	{
		return _canSort;
	}
}
