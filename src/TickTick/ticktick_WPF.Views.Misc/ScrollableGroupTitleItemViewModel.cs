using System.Windows.Media;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Misc;

public class ScrollableGroupTitleItemViewModel : BaseViewModel
{
	private bool _selected;

	public string Name { get; set; }

	public string Value { get; set; }

	public SolidColorBrush IconColor { get; set; }

	public Geometry Icon { get; set; }

	public string Tooltip { get; set; }

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

	public ScrollableGroupTitleItemViewModel()
	{
	}

	public ScrollableGroupTitleItemViewModel(string name)
	{
		Name = name;
	}
}
