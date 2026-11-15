using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class SelectThemeColorViewModel : BaseViewModel
{
	private string _color;

	private bool _selected;

	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			_color = value;
			OnPropertyChanged("Color");
		}
	}

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
