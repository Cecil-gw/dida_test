using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class ThemeBaseModel : BaseViewModel
{
	private bool _selected;

	public string Key { get; set; }

	public List<ThemeBaseModel> Parent { get; set; }

	public string Name { get; set; }

	public Brush Color { get; set; }

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

	public void SetSelected()
	{
		Parent.Where((ThemeBaseModel item) => item.Selected && item.Key != Key).ToList().ForEach(delegate(ThemeBaseModel item)
		{
			item.Selected = false;
		});
		Selected = true;
	}
}
