using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Summary;

public class SummaryDisplayStyleItemModel : BaseViewModel
{
	private bool _selected;

	public string Key { get; set; }

	public string Name { get; set; }

	public SummaryDisplayItemViewModel Parent { get; set; }

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
