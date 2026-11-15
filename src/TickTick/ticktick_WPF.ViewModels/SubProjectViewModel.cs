using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class SubProjectViewModel : NormalProjectViewModel
{
	public SubProjectViewModel(ProjectModel project)
		: base(project)
	{
		base.IsSubItem = true;
	}
}
