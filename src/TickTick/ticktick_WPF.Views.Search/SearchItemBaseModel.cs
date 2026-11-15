using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class SearchItemBaseModel : BaseViewModel
{
	private bool _selected;

	public virtual string Id { get; set; }

	public virtual bool CanSelect { get; set; }

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
