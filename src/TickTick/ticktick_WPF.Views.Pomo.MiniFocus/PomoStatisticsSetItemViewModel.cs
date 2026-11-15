using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo.MiniFocus;

public class PomoStatisticsSetItemViewModel : BaseViewModel
{
	private bool _selected;

	public bool IsEnabled { get; set; } = true;

	public string Name { get; set; }

	public bool Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = value;
			OnPropertyChanged("Selected");
		}
	}
}
