using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class EmojiKeyViewModel : BaseViewModel
{
	public string Key { get; set; }

	public string Text { get; set; }

	private bool _isSelected { get; set; }

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

	public string EmojiPathName { get; set; }
}
