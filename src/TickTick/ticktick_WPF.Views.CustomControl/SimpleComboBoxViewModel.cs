namespace ticktick_WPF.Views.CustomControl;

public class SimpleComboBoxViewModel : UpDownSelectViewModel
{
	private string _title;

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public bool IsSplit { get; set; }

	public SimpleComboBoxViewModel(string text)
	{
		_title = text;
		if (text == null)
		{
			IsSplit = true;
		}
	}
}
