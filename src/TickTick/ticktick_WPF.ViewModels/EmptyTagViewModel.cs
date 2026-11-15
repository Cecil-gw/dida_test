using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class EmptyTagViewModel : EmptyProjectViewModel
{
	public EmptyTagViewModel()
	{
		base.Type = PtfType.Tag;
	}
}
