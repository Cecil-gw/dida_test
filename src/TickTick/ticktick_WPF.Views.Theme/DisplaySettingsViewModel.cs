using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Theme;

public class DisplaySettingsViewModel : BaseViewModel
{
	private bool _selected;

	private string _imageUri;

	public string Title { get; set; }

	public int Type { get; }

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

	public string ImageUri
	{
		get
		{
			return _imageUri;
		}
		set
		{
			if (_imageUri != value)
			{
				_imageUri = value;
				OnPropertyChanged("ImageUri");
			}
		}
	}

	public DisplaySettingsViewModel(int type, string id, string title)
	{
		Type = type;
		Title = title;
		ImageUri = "../../Assets/Theme/" + (ThemeUtil.IsDark() ? "dark" : "light") + "/" + id + "_" + (Utils.IsCn() ? "cn" : "en") + ".png";
	}

	public void ChangeImage()
	{
		if (ThemeUtil.IsDark())
		{
			ImageUri = ImageUri?.Replace("light", "dark");
		}
		else
		{
			ImageUri = ImageUri?.Replace("dark", "light");
		}
	}
}
