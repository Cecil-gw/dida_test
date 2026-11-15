using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class EmptyFilterViewModel : EmptyProjectViewModel
{
	public EmptyFilterViewModel()
	{
		base.Type = PtfType.Filter;
	}
}
