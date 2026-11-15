using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Setting;

internal class FeatureViewModel : BaseViewModel
{
	private string _settingText;

	public ExpandFeature Value { get; set; }

	public string Title { get; set; }

	public string Desc { get; set; }

	public string SettingText
	{
		get
		{
			if (!IsOpen)
			{
				return null;
			}
			return _settingText;
		}
		set
		{
			_settingText = value;
			OnPropertyChanged("SettingText");
		}
	}

	public bool IsOpen { get; set; }

	public bool ShowOpen { get; set; } = true;

	public string Image { get; set; } = "../../Assets/ImageSource/Feature.png";

	public FeatureViewModel()
	{
	}

	public FeatureViewModel(ExpandFeature feature, bool isOpen, bool showSettings = false)
	{
		Value = feature;
		Title = GetTitle(feature);
		Desc = Utils.GetString(feature.ToString() + "Desc");
		_settingText = ((!showSettings) ? null : Utils.GetString(feature.ToString() + "Setting"));
		IsOpen = isOpen;
		Image = "../../Assets/ImageSource/" + feature.ToString() + "Feature.png";
	}

	private string GetTitle(ExpandFeature feature)
	{
		return feature switch
		{
			ExpandFeature.Habit => Utils.GetString("HabitView"), 
			ExpandFeature.Focus => Utils.GetString("PomoFocus"), 
			_ => Utils.GetString(feature.ToString()), 
		};
	}

	public void NotifySettingTextChanged()
	{
		OnPropertyChanged("SettingText");
	}
}
