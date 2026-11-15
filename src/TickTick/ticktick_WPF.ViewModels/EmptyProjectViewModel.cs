using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class EmptyProjectViewModel : ProjectItemViewModel
{
	public PtfType Type { get; set; } = PtfType.Project;

	public EmptyProjectViewModel()
	{
		base.IsEmptyProject = true;
	}
}
