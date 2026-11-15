using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class ItemColorViewModel : BaseViewModel
{
	private string _color;

	private bool _selected;

	public bool IsAddCustom { get; set; }

	public bool NoColor { get; set; }

	public bool IsContrastingColor { get; set; }

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
			if (_selected != value)
			{
				_selected = value;
				OnPropertyChanged("Selected");
			}
		}
	}
}
