using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class ProThemeViewModel : ThemeBaseModel
{
	private bool _loading;

	public string Url => "https://" + BaseUrl.GetPullDomain() + "/windows/theme/logo/" + base.Key.ToLower() + ".png";

	public bool Loading
	{
		get
		{
			return _loading;
		}
		set
		{
			_loading = value;
			OnPropertyChanged("Loading");
		}
	}
}
