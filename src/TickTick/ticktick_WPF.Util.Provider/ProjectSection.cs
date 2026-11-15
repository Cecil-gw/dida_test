using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class ProjectSection : Section
{
	private bool _canSort;

	public new string ProjectId { private get; set; }

	public ProjectSection(bool canSort)
	{
		_canSort = canSort;
	}

	public override string GetProjectId()
	{
		return ProjectId;
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
