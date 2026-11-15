using System.Collections.ObjectModel;
using ticktick_WPF.Framework.Collections;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class FontSizeViewModel : BaseViewModel
{
	private bool _selected;

	public string Title { get; set; }

	public int Size { get; set; }

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

	private FontSizeViewModel(int size, string title)
	{
		Size = size;
		Title = title;
		Selected = LocalSettings.Settings.BaseFontSize == size;
	}

	public static ObservableCollection<FontSizeViewModel> BuildModels()
	{
		return new ExtObservableCollection<FontSizeViewModel>
		{
			new FontSizeViewModel(14, Utils.GetString("Normal"))
			{
				Selected = (LocalSettings.Settings.BaseFontSize == 14 || LocalSettings.Settings.BaseFontSize == 0)
			},
			new FontSizeViewModel(16, Utils.GetString("Large")),
			new FontSizeViewModel(18, Utils.GetString("ExtraLarge"))
		};
	}
}
