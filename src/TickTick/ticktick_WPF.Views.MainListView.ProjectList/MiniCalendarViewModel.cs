using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.ProjectList;

public class MiniCalendarViewModel : BaseViewModel
{
	private bool _miniMode = true;

	public bool MiniMode
	{
		get
		{
			return _miniMode;
		}
		set
		{
			_miniMode = value;
			OnPropertyChanged("MiniMode");
		}
	}
}
