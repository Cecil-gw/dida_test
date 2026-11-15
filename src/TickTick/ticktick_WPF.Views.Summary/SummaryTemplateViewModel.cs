using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Summary;

public class SummaryTemplateViewModel : BaseViewModel
{
	private bool _isSelected;

	public string Id { get; set; }

	public string Name { get; set; }

	public bool IsDefault => Id == "defaultId";

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			OnPropertyChanged("IsSelected");
		}
	}
}
